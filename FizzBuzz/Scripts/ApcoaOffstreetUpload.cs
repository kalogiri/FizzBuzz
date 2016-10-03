using LibertyPdfRegeneration;
using LibertyUtils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSLib;
using WinSCP;

namespace FizzBuzz.Scripts
{
    class ApcoaOffstreetUpload : BaseDownloadScript
    {
        string _awaitingUploadsDir;
        string _localArchiveDir;
        string _ppPdfRegenerationDir;
        string _externalConfirmationDir;

        private void ChangeDelimiter(string oSymb, string rSymb, string file)
        {
            string text = File.ReadAllText(file);
            Log.Write("Changing Delimiters");
            text = text.Replace(oSymb, rSymb);
            File.WriteAllText(file, text);
        }

        public ApcoaOffstreetUpload()
        {
            DebugLogDir = HostPath.ppwatch_2 + @"Data\Apcoa\Process\Upload\DebugLogs\";
            WorkingDir = HostPath.ppwatch_2 + @"Data\Apcoa\Uploads\WorkingFolder\";

            _awaitingUploadsDir = HostPath.ppwatch_2 + @"Data\Apcoa\Uploads\AwaitingUpload\";
            _localArchiveDir = HostPath.ppwatch_2 + @"Data\Apcoa\Uploads\Archive\";
            _ppPdfRegenerationDir = HostPath.ppwatch_2 + @"Data\Apcoa\Uploads\";

            CredentialsPath = HostPath.ppwatch_1 + @"LibertyConfig\ExternalCredentials.xml";
            LibertyConfigExternalCredentials.Credential ftpCredentials = Credentials.Get("Parking - APCOA Offstreet");

            _externalConfirmationDir = "/Incoming/";

            MutexUtils.OpenMutexUnique("APOCOA Offstreet");
            DirUtils.RecreateLogged(WorkingDir);
            try
            {
                List<WorkingFile> workingFiles = GatherLocalData<WorkingFile>(
                    from: _awaitingUploadsDir,
                    to: WorkingDir,
                    searchPattern: "*.txt",
                    searchOption: SearchOption.TopDirectoryOnly,
                    clearDestination: true
                );

                if (workingFiles.Count == 0)
                {
                    Log.Write("No Data!");
                    return;
                }

                foreach (WorkingFile wf in workingFiles)
                {
                    // First convert all of the "|"s to ","s
                    ChangeDelimiter("|", ",", wf.WorkingPath);

                    Log.Write("--------------------------------------------------------");
                    Log.Write("Processing file: " + wf.SourcePath);

                    Log.Write("Querying database: " + Path.GetFileName(wf.SourcePath));

                    List<Job> jobs = Job.GetJobsWithFilename(clientName: "Apcoa", fileName: Path.GetFileName(wf.SourcePath));

                    Log.Write("Found " + jobs.Count + " jobs for file: " + Path.GetFileName(wf.SourcePath));
                    if (Job.JobsNotInTask(jobs, TaskType.Confirmation).Count > 0)
                    {
                        Log.Default.Write("Unfinished jobs found for this file, skipping");
                        continue;
                    }

                    List<int> finishedRecordNumbers = Job.ActiveRecordNumbersForJobs(jobs);
                    if (finishedRecordNumbers.Count() == 0)
                    {
                        EmailUtils.ErrorReport("No jobs matching file. Archive manually", "No jobs matching filename: " + wf.WorkingPath);
                        continue;
                    }

                    try
                    {
                        // Lock jobs in task while we do processing
                        using (JobTaskProcess taskProcess = new JobTaskProcess(TaskType.Confirmation, jobs, disableWhenDebugging: false))
                        {
                            ProcessUpload(wf, finishedRecordNumbers, ftpCredentials);
                            taskProcess.Finished();
                        }

                        FileUtils.DeleteLogged(wf.SourcePath, disableWhenDebugging: false);
                        //DebugUtils.ConsolePause();
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex);
                    }
                }
            }
            catch (Exception ex)
            {

                Log.Write(ex);
            }
        }

        private void ProcessUpload(WorkingFile wf, List<int> finishedRecordNumbers, LibertyConfigExternalCredentials.Credential cred)
        {
            // Output dir
            string workingOutputDir = GetWorkingDir(@"Output\", empty: true);

            CSVDocument csvDoc = new CSVDocument(wf.WorkingPath) { Delimiter = ",", QuotedValues = true };
            csvDoc.LoadFile();
            List<Dictionary<string, string>> csvDataRows = csvDoc.ReadAllKeyed(ignoreDuplicateHeaderKeys: true);
            csvDoc.UnloadFile();

            // Get all the rows in the csv doc of which the records are finished.
            csvDataRows = csvDataRows.Where((r, i) => finishedRecordNumbers.Contains(i + 1)).ToList();

            // Posted date for the TimeStamp
            string date = csvDataRows[0].LookupLogged("LibertyTaggedTimestamp").Substring(0, 19);
            string pattern = "dd/MM/yyyy H:mm:ss";

            DateTime libertyTaggedTimestamp = DateTime.ParseExact(date, pattern, CultureInfo.CurrentCulture);

            // Zip Name
            string zipName = libertyTaggedTimestamp.ToString("yyyyMMddHmm") + "_" + csvDataRows[0].LookupLogged("LetterCode") + "_LibertyPDFImport.zip";

            List<string> expectedPdfPaths = new List<string>();
            List<string> missingPdfPaths = new List<string>();

            foreach (Dictionary<string, string> csvDataRow in csvDataRows)
            {
                string pdfFileName = csvDataRow.LookupLogged("ClientReference2") + "_" + csvDataRow.LookupLogged("LetterCode") + "_" + libertyTaggedTimestamp.ToString("yyyyMMdd") + ".pdf";

                // Add all the files that needs to be processed
                expectedPdfPaths.Add(_awaitingUploadsDir + pdfFileName);

                if (!File.Exists(_awaitingUploadsDir + pdfFileName)) { missingPdfPaths.Add(_awaitingUploadsDir + pdfFileName); }
            }

            Log.Default.Write("Missing " + missingPdfPaths.Count() + " of " + csvDataRows.Count() + " pdfs");

            // Restore PDFs and Images
            if (missingPdfPaths.Count() > 0)
            {
                // Restore Image
                PdfRegeneration.RestoreImages(
                    clientName: "Apcoa",
                    fileName: Path.GetFileName(wf.SourcePath)
                );

                // Restoring PDFs
                string regenDataPath = WorkingDir + Path.GetFileName(wf.WorkingPath) + "-PDF-REGENERATION.txt";
                Log.Write("Creating regen file in: " + regenDataPath);

                CSVUtils.FilterToFileKeyed(wf.WorkingPath, regenDataPath, quotedValues: true,
                    delimeter: "|",
                    includeRecords: (dataRow, i) =>
                        missingPdfPaths.Contains(_awaitingUploadsDir + dataRow.LookupLogged("ClientReference2") + "_" + dataRow.LookupLogged("LetterCode") + "_" + libertyTaggedTimestamp.ToString("yyyyMMdd") + ".pdf"));

                try
                {
                    // Restore PDFs
                    PdfRegeneration.RestorePdfs(
                        dataPath: regenDataPath,
                        dataProcessingDir: _ppPdfRegenerationDir,
                        expectedPdfPaths: missingPdfPaths,
                        minutesTimeout: 10 + (missingPdfPaths.Count / 5)
                    );
                }
                catch (Exception ex)
                {
                    Log.Write("PDF regeneration error" + Environment.NewLine + ex);
                }

                foreach (string pdfPath in expectedPdfPaths)
                {
                    FileUtils.CopyLogged(pdfPath, FileUtils.UniqueFilename(workingOutputDir + Path.GetFileName(pdfPath)));
                }

                ZipUtils.CompressLogged(workingOutputDir + "*", WorkingDir + zipName);

                if (_localArchiveDir != null)
                {
                    FileUtils.CopyLogged(WorkingDir + zipName, _localArchiveDir + zipName, overwrite: true);
                }

                List<string> filesToCollect = Directory.GetFiles(_localArchiveDir, "*.zip").ToList();

                MoveToServer(cred, _externalConfirmationDir, filesToCollect);
            }
        }

        private SessionOptions Sessionoptions(LibertyConfigExternalCredentials.Credential credentials)
        {
            int port = int.Parse(credentials.Server.Substring(credentials.Server.LastIndexOf(':') + 1));
            string server = credentials.Server;
            int serverIndex = credentials.Server.IndexOf(':');
            if (serverIndex > 0) { server = server.Substring(0, serverIndex); }

            SessionOptions sessionOptions = new SessionOptions
            {
                Protocol = Protocol.Sftp,
                HostName = server,
                PortNumber = port,
                UserName = credentials.Username,
                Password = credentials.Password,
                GiveUpSecurityAndAcceptAnySshHostKey = true
            };
            return sessionOptions;
        }

        private void MoveToServer(LibertyConfigExternalCredentials.Credential credentials, string archiveFolder, List<string> localZipFiles)
        {
            SessionOptions sessionOption = Sessionoptions(credentials);
            try
            {
                using (Session session = new Session())
                {
                    session.Open(sessionOption);
                    if (session.Opened)
                    {
                        Log.Write("Archiving Files...");
                        foreach (string file in localZipFiles)
                        {
                            session.PutFiles(file, archiveFolder);
                            Log.Write("Finished Moving " + file + "to the server");
                        }
                        Log.Write("Archive process finished.");
                        Log.Write("Closing connection to server.");
                    }
                }
                Log.Write("Connectio to server closed.");
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
