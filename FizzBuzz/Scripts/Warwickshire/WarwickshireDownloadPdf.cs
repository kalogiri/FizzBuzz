using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibertyUtils;
using System.IO;
using System.Text.RegularExpressions;

namespace FizzBuzz.Scripts.Warwickshire
{
    class WarwickshireLib : BaseDownloadScript
    {
        public LibertyConfigExternalCredentials.Credential FtpCredentials;
        public string SequenceNumber;

        public WarwickshireLib()
        {
            DebugLogDir = HostPath.ppwatch_2 + @"Data\Warwickshire\Process\Download\DebugLogs\";
            WorkingDir = HostPath.ppwatch_2 + @"Data\Warwickshire\Process\Download\WorkingFolder\";
            LiveDataDir = HostPath.ppwatch_2 + @"Data\Warwickshire\ThunderSnow\Incoming\";
            PPImagesDir = HostPath.ppwatch_2 + @"Data\Warwickshire\Images\";
            CredentialsPath = HostPath.ppwatch_2 + @"LibertyConfig\ExternalCredentials.xml";
            FtpCredentials = Credentials.Get("NSLCloud");
            SequenceNumber = NextSequenceNumber(HostPath.ppwatch_2 + @"Data\Warwickshire\Process\Download\Scripts\NextBatchId.txt");
        }
    }
    class WarwickshireDownloadPdf : WarwickshireLib
    {
        string _remoteDir, _pdfPrefix;
        public WarwickshireDownloadPdf()
        {
            _remoteDir = @"Out/Warwickshire/PCN/";

            _pdfPrefix = SequenceNumber + "_";

            MutexUtils.OpenMutexUnique("Warwickshire_ONEOFF");
            //DirUtils.RecreateLogged(WorkingDir);

            Console.WriteLine(DebugLogDir + Environment.NewLine + WorkingDir + Environment.NewLine + LiveDataDir + Environment.NewLine + PPImagesDir + Environment.NewLine + FtpCredentials.Name + Environment.NewLine);
        }

        private void ProcessDownload()
        {
            List<WorkingFile> WorkingFiles = GatherFTPData<WorkingFile>
            (
                   credential: FtpCredentials,
                   from: _remoteDir,
                   to: WorkingDir,
                   searchPattern: "*.pdf",
                   searchOption: SearchOption.TopDirectoryOnly,
                   clearDestination: true
            );

            List<string> archiveCommands = new List<string>();
            try
            {
                IEnumerable<WorkingFile> workingFiles = WorkingFiles.Where(wf => Regex.IsMatch(Path.GetFileName(wf.SourcePath), @"^NOR.*?.pdf$", RegexOptions.IgnoreCase));

                // Check for files
                if(workingFiles.Count() == 0)
                {
                    Log.Write("No Data");
                }
                else
                {
                    string csvFilePathNor = WorkingDir + "Warwickshire_NoR_" + SequenceNumber + ".txt";
                    
                    // Generate CSV
                    Log.Write("Generating CSV from list of pdfs: " + csvFilePathNor);
                    CSVDocument csvDoc = new CSVDocument();
                    foreach(WorkingFile wf in workingFiles)
                    {
                        csvDoc.AddRow(_pdfPrefix + Path.GetFileName(wf.SourcePath));
                        archiveCommands.Add("rm " + "\"" + wf.SourcePath + "\"");
                        FileUtils.MoveLogged(wf.WorkingPath, PPImagesDir + _pdfPrefix + Path.GetFileName(wf.WorkingPath), disableWhenDebugging: false);
                    }
                    csvDoc.SaveAs(csvFilePathNor);
                    csvDoc.UnloadFile();

                    // Move files to live
                    FileUtils.MoveLogged(csvFilePathNor, LiveDataDir + Path.GetFileName(csvFilePathNor), disableWhenDebugging: false);
                }
                Log.Write("----------------------------------------------------");
                Log.Write("Processing Corres");

                workingFiles = WorkingFiles.Where(wf => Regex.IsMatch(Path.GetFileName(wf.SourcePath), @"^(ZQ|NTOZQ).*?.pdf$", RegexOptions.IgnoreCase));

                // Check for files
                if(workingFiles.Count() == 0)
                {
                    Log.Write("No data");
                }
                else
                {
                    string csvFilePathCorres = WorkingDir + "Warwickshire_Correspondence_" + SequenceNumber + ".txt";

                    // Generate csv
                    Log.Write("Generating csv from list of pdfs: " + csvFilePathCorres);
                    CSVDocument csv = new CSVDocument();
                    foreach(WorkingFile wf in workingFiles)
                    {
                        csv.AddRow(_pdfPrefix + Path.GetFileName(wf.SourcePath));
                        archiveCommands.Add("rm " + "\"" + wf.SourcePath + "\"");
                        FileUtils.MoveLogged(wf.WorkingPath, PPImagesDir + _pdfPrefix + Path.GetFileName(wf.WorkingPath), disableWhenDebugging: false);
                    }

                    csv.SaveAs(csvFilePathCorres);
                    csv.UnloadFile();

                    FileUtils.MoveLogged(csvFilePathCorres, LiveDataDir + Path.GetFileName(csvFilePathCorres), disableWhenDebugging: false);
                }

                if(archiveCommands.Count() > 0)
                {
                    Log.Write("----------------------------------------------------");
                    Log.Write("Archiving completed files");
                    ExecuteFTPCommand(
                        credential: FtpCredentials,
                        command: string.Join("\n", archiveCommands),
                        disableWhenDebugging: false    
                    );
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
