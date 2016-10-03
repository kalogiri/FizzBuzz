using LibertyPdfRegeneration;
using LibertyUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TSLib;

namespace FizzBuzz.Scripts.Dartford
{
    class DartfordUploads : BaseDownloadScript
    {
        string _dataPickupDir;
        string _localArchiveDir;
        string _sourceArchiveDir;
        string _ppPdfRegenerationDir;
        string _pdfFinalLocation;

        private void ChangeDelimiter(string originalDelimiter, string newDelimiter, string dataFile)
        {
            string text = File.ReadAllText(dataFile);
            Log.Write("Changing Delimiters");
            text = text.Replace(originalDelimiter, newDelimiter);
            File.WriteAllText(dataFile, text);
        }

        public DartfordUploads()
        {
            DebugLogDir = HostPath.ppwatch_3 + @"Data\Dartford\Process\Uploads\DebugLogs\";
            WorkingDir = HostPath.ppwatch_3 + @"Data\Dartford\Process\Uploads\WorkingDir\";
            LocalSftpDir = HostPath.sftp + @"Dartford DFFCP\Taranto\DataOut\";
            //LocalSftpDir = HostPath.tenp_serv + @"data\Dartford\Uploads\TEST\SFTP\";

            _dataPickupDir = HostPath.ppwatch_3 + @"Data\Dartford\Uploads\Output\";

            _localArchiveDir = HostPath.mass_store + @"MassStorage\ParkingArchive\Dartford\ConfirmationsComplete\Uploaded\";

            _sourceArchiveDir = HostPath.mass_store + @"MassStorage\ParkingArchive\Dartford\SourceArchive\";

            _ppPdfRegenerationDir = HostPath.ppwatch_3 + @"Data\Dartford\Uploads\";

            // Check for space on the server before processing
            MinimumDiskSpace(HostPath.ppwatch_1 + @"Data\Dartford\", 2/*GB*/);

            // Create a mutex
            MutexUtils.OpenMutexCached("Dartford");

            DirUtils.RecreateLogged(WorkingDir);

            // Start of the script...
            Log.Write("=========================================================");

            // Collect the data files
            List<WorkingFile> workingFiles = GatherLocalData<WorkingFile>(
                from: _dataPickupDir,
                to: WorkingDir,
                searchPattern: "*.txt",
                searchOption: SearchOption.TopDirectoryOnly,
                clearDestination: true
            );

            workingFiles = workingFiles.Where(wf => Regex.IsMatch(wf.WorkingPath, "_MTCNODR_")).ToList();

            if (workingFiles.Count == 0)
            {
                Log.Write("No data!");
                return;
            }

            foreach (WorkingFile wf in workingFiles)
            {
                Log.Write("--------------------------------------------------------");
                Log.Write("Processing file: " + wf.SourcePath);

                Log.Write("Changing delimiters from Pipe to Comma for file: " + wf.WorkingPath);
                ChangeDelimiter("\t", ",", wf.WorkingPath);

                // Get Details from the file name
                FilenameDetails dartfordFilenameDetails = new FilenameDetails(wf.SourcePath);

                // Lookup database for completed record numbers
                Log.Write("Querrying database: " + Path.GetFileName(wf.SourcePath));
                ConfigThunderSnow.Local.UseServer = "LIVE";
                List<Job> jobs = Job.GetJobsWithFilename(clientName: "Dartford", fileName: Path.GetFileName(wf.SourcePath));

                Log.Write("Found " + jobs.Count + " jobs for file: " + Path.GetFileName(wf.SourcePath));
                if (Job.JobsNotInTask(jobs, TaskType.Confirmation).Count > 0)
                {
                    // if unfinished jobs, skip
                    Log.Default.Write("Unifnished jobs found for this file, skipping.");
                    return;
                }

                // Collate all the number of jobs that are finished for the data file
                List<int> finishedRecordNumber = Job.ActiveRecordNumbersForJobs(jobs);

                if (finishedRecordNumber.Count == 0)
                {
                    // If no active records, send email and skip
                    EmailUtils.ErrorReport("No jobs matching file. Archive manually", "No jobs matching filename: " + wf.WorkingPath);
                    continue;
                }

                try
                {
                    using (JobTaskProcess taskProcess = new JobTaskProcess(TaskType.Confirmation, jobs, disableWhenDebugging: false))
                    {
                        ProcessUpload(wf, dartfordFilenameDetails, finishedRecordNumber);
                        taskProcess.Finished();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error processing file: " + wf.SourcePath, ex);
                }
            }
        }

        private void ProcessUpload(WorkingFile wf, FilenameDetails fd, List<int> frn)
        {
            // Create empty output dir
            string workingOutputDir = GetWorkingDir(@"Output\", empty: true);

            // Load data from csv file
            CSVDocument csvDoc = new CSVDocument(wf.WorkingPath) { Delimiter = ",", QuotedValues = true };
            csvDoc.LoadFile();
            List<Dictionary<string, string>> csvDataRows = csvDoc.ReadAllKeyed(ignoreDuplicateHeaderKeys: true);
            csvDoc.UnloadFile();

            // Filter data rows to include only those that are finished
            csvDataRows = csvDataRows.Where((r, i) => frn.Contains(i + 1)).ToList();

            // Get the posted data from the first record's parent
            string postedDate = csvDataRows[0].LookupLogged("LibertyTaggedTimestamp").Substring(0, 10);

            // Check for missing pdfs
            List<string> expectedPdfPaths = new List<string>();
            List<string> missingPdfPaths = new List<string>();

            _pdfFinalLocation = _dataPickupDir + Path.GetFileNameWithoutExtension(wf.SourcePath) + @"\";
            Directory.CreateDirectory(_pdfFinalLocation);
            // Loop through the data rows
            foreach (Dictionary<string, string> csvDataRow in csvDataRows)
            {
                string pdfFileName = csvDataRow.LookupLogged("PCN TICKET NUMBER") + ".pdf";
                expectedPdfPaths.Add(_pdfFinalLocation + pdfFileName);
                if (!File.Exists(_pdfFinalLocation + pdfFileName))
                {
                    missingPdfPaths.Add(_pdfFinalLocation + pdfFileName);
                }
            }
            Log.Default.Write("Missing " + missingPdfPaths.Count + " of " + csvDataRows.Count() + " pdfs");

            // Regenerate the missing pdfs
            if (missingPdfPaths.Count > 0)
            {
                // Restore images
                PdfRegeneration.RestoreImages(clientName: "Dartford", fileName: Path.GetFileName(wf.SourcePath));

                // Restore pdfs
                //--------------

                // Write a data file with the exact same format as the original but only with the missing pdfs
                string regenerationDataPath = WorkingDir + Path.GetFileName(wf.WorkingPath) + "-REGENERATON";
                CSVUtils.FilterToFileKeyed(
                    originalDataPath: wf.WorkingPath,
                    newDataPath: regenerationDataPath,
                    quotedValues: true,
                    delimeter: "\t",
                    includeRecords: (datarow, i) =>
                    {
                        return missingPdfPaths.Contains(_pdfFinalLocation + datarow.LookupLogged("PCN TICKET NUMBER") + ".pdf");
                    }
                );

                try
                {
                    // Restore the missng pdfs
                    PdfRegeneration.RestorePdfs(
                        dataPath: regenerationDataPath,
                        dataProcessingDir: _ppPdfRegenerationDir,
                        expectedPdfPaths: missingPdfPaths,
                        minutesTimeout: 10 + (missingPdfPaths.Count / 5)
                    );
                }
                catch (TimeoutException)
                {
                    // Restore the missng pdfs one more time
                    PdfRegeneration.RestorePdfs(
                        dataPath: regenerationDataPath,
                        dataProcessingDir: _ppPdfRegenerationDir,
                        expectedPdfPaths: missingPdfPaths,
                        minutesTimeout: 10 + (missingPdfPaths.Count / 5)
                    );
                }
            }
            // Gather the pdf files and put them in a unique f
            foreach (string pdfPath in expectedPdfPaths)
            {
                FileUtils.CopyLogged(pdfPath, FileUtils.UniqueFilename(workingOutputDir + Path.GetFileName(pdfPath)));
            }

            string[] pdfPaths = Directory.GetFiles(workingOutputDir, "*.pdf", SearchOption.TopDirectoryOnly);

            // Create the XML file with the processed batch's details
            Log.Write("Saving XML file with batch details.");
            XElement currentBatchItems;
            XElement xmlRoot = new XElement("Batches");
            xmlRoot.Add(
                new XElement("Batch",
                    new XElement("BatchNo", fd.BatchNo),
                    new XElement("DocType", fd.BatchType),
                    new XElement("PostedDate", postedDate),
                    currentBatchItems = new XElement("Items")
                )
            );

            foreach (string pdfPath in pdfPaths)
            {
                currentBatchItems.Add(
                    new XElement("Item",
                        new XElement("Reference", Path.GetFileNameWithoutExtension(pdfPath)),
                        new XElement("Filename", Path.GetFileName(pdfPath)),
                        new XElement("PostedString", "Document printed by Libertys and posted by Royal Mail on " + postedDate),
                        new XElement("rdnumber"),
                        new XElement("ChequeNo"),
                        new XElement("SortCode")
                    )
                );
            }

            // Save the xml file
            string xmlFileName = fd.BatchType + "-" + fd.BatchNo + ".xml";
            Log.Write("Saving xml batch file: " + workingOutputDir + xmlFileName);
            xmlRoot.Save(workingOutputDir + xmlFileName);

            // Create a zip file that includes the contents of the WorkingOutputDir
            string zipFileName = fd.BatchType + "_" + fd.BatchNo + ".zip";
            ZipUtils.CompressLogged(workingOutputDir + "*", WorkingDir + zipFileName);
            string tempDir = HostPath.mass_store + @"ParkingArchive\Dartford\SourceArchive\";

            #region Move the mass storage and the sftp
            try
            {
                // Move the zip file to mass storage
                string massStorageDest = HostPath.mass_store + @"MassStorage\ParkingArchive\Dartford\ConfirmationsComplete\Uploaded\";
                FileUtils.CopyLogged(WorkingDir + zipFileName, massStorageDest + zipFileName, true);

                // Move the zip file to sftp
                FileUtils.CopyLogged(WorkingDir + zipFileName, LocalSftpDir + zipFileName);
            }
            catch (Exception ex)
            {
                Log.Write("Error archiving the files, Please check the " + WorkingDir + " for the zip file" + Environment.NewLine + ex);
            }
            #endregion

            #region Copy the data file to archive
            try
            {
                Log.Write("Moving files from " + _dataPickupDir + " to " + tempDir);
                FileUtils.CopyLogged(WorkingDir + Path.GetFileName(wf.WorkingPath), tempDir + Path.GetFileName(wf.WorkingPath), true);
            }
            catch (Exception ex)
            {
                Log.Write("Error moving the data files to " + tempDir + Environment.NewLine + ex + Environment.NewLine);
            }

            #endregion

            #region Delete original data file and folder
            try
            {
                Log.Write("Deleting original files and folder in " + _dataPickupDir);
                DeleteFromSource(Path.GetFileName(wf.SourcePath), _dataPickupDir);
            }
            catch (Exception ex)
            {
                Log.Write("Error Deleting the files at the end." + Environment.NewLine + ex + Environment.NewLine);
            }
            #endregion

        }

        private void DeleteFromSource(string fileName, string rootDir)
        {
            FileUtils.DeleteLogged(rootDir + fileName, disableWhenDebugging: false);
            string folderName = rootDir + Path.GetFileNameWithoutExtension(fileName);

            if (Directory.Exists(folderName))
            {
                DirUtils.DeleteLogged(folderName + @"\");
            }
        }
    }
}

#region Dartford Shit V.1 - Basically TENP with bits of DatrfordUploads2
//class DartfordUploads : BaseDownloadScript
//{

//    string AwaitingUploadsDir;
//    string PDfRegenerationDir;
//    string ZipUploadDir;
//    string LocalArchiveDir;
//    string SourceArchiveDir;

//    class DartfordWorkingFile : WorkingFile
//    {
//        public string BatchName;
//        public string SourcePDfDir;
//        public List<Job> jobs;
//    }

//    private void changeDelimiter(string oSymb, string rSymb, string file)
//    {
//        string text = File.ReadAllText(file);
//        Log.Write("Changing Delimiters");
//        text = text.Replace(oSymb, rSymb);
//        File.WriteAllText(file, text);
//    }

//    public DartfordUploads()
//    {
//        DebugLogDir = HostPath.ppwatch_1 + @"Data\Dartford\Process\Uploads\DebugLogs\";
//        WorkingDir = HostPath.ppwatch_1 + @"Data\Dartford\Process\Uploads\WorkingFolder\";

//        AwaitingUploadsDir = HostPath.ppwatch_1 + @"Data\Dartford\Uploads\Output\";
//        PDfRegenerationDir = HostPath.ppwatch_1 + @"Data\Dartford\Uploads\";

//        LocalSftpDir = HostPath.sftp + @"Dartford DFFCP\Taranto\";

//        LocalArchiveDir = HostPath.mass_store + @"MassStorage\ParkingArchive\Dartford\ConfirmationsComplete\Uploaded\";
//        SourceArchiveDir = HostPath.mass_store + @"MassStorage\ParkingArchive\Dartford\SourceArchive\";

//        // Gather all the files that are sitting in \\PPWATCH-1\Data\Dartford\Uploads\Output\
//        var workingFiles = GatherLocalData<DartfordWorkingFile>(
//            from: AwaitingUploadsDir,
//            to: WorkingDir,
//            searchPattern: "*MTCNODR*.txt",
//            searchOption: SearchOption.TopDirectoryOnly,
//            clearDestination: true
//        );

//        if (workingFiles.Count == 0)
//        {
//            Log.Write("No data!");
//            return;
//        }

//        foreach (var wf in workingFiles)
//        {
//            Log.Write(Environment.NewLine + "--------------------------------------------------------");
//            Log.Write("Processing file: " + wf.SourcePath);


//            Log.Write("Changing delimiters for the file.");

//            changeDelimiter("\t", ",", wf.WorkingPath);

//            wf.BatchName = wf.SourcePath;

//            FilenameDetails DartfordFileNameDetails = new FilenameDetails(wf.BatchName); // Batchname

//            // Lookup database for completed record numbers
//            Log.Write("Querying database: " + Path.GetFileName(wf.SourcePath));
//            List<Job> jobs = Job.GetJobsWithFilename(
//                clientName: "Dartford",
//                fileName: Path.GetFileName(wf.SourcePath)
//            );

//            Log.Write("Found " + jobs.Count + " jobs for file: " + Path.GetFileName(wf.SourcePath));

//            if (Job.JobsNotInTask(jobs, TaskType.Confirmation).Count > 0)
//            {
//                // If unfinished jobs, skip
//                Log.Default.Write("Unfinished jobs found for this file, skipping");
//                continue;
//            }

//            List<int> finishedRecordNumbers = Job.ActiveRecordNumbersForJobs(jobs);
//            if (finishedRecordNumbers.Count == 0)
//            {
//                //if no active records, send email and skip
//                EmailUtils.ErrorReport("No jobs matching file. Archive manually", "No jobs matching filename: " + wf.WorkingPath);
//                continue;
//            }

//            try
//            {
//                using (var taskProcess = new JobTaskProcess(TaskType.Confirmation, jobs, disableWhenDebugging: true))
//                {
//                    ProcessTextFiles(wf, DartfordFileNameDetails, finishedRecordNumbers);
//                    taskProcess.Finished();
//                }
//                Log.Write("Dartford Uploads Finished for: " + Path.GetFileName(wf.WorkingPath));
//            }
//            catch (Exception ex)
//            {
//                throw new Exception("Error processing file: " + wf.SourcePath, ex);
//            }

//            try
//            {
//                Log.Write("Deleting the data folder and file");
//                DirUtils.DeleteLogged(AwaitingUploadsDir + Path.GetFileNameWithoutExtension(wf.SourcePath));
//            }
//            catch(Exception ex)
//            {
//                Log.Write("Error deleting the folder " + AwaitingUploadsDir + Path.GetFileName(wf.SourcePath) + Environment.NewLine + ex);
//            }
//        }
//        // Emptying WorkingFolder to save space
//        DirUtils.Empty(WorkingDir);
//        Log.Write("======================================================");
//        Log.Write(this.GetType().Name + " complete!");
//    }

//    private void ProcessTextFiles(WorkingFile wf, FilenameDetails fd, List<int> fr)
//    {
//        string XmlFileName = fd.BatchType + "_" + fd.BatchNo + ".xml";
//        string ZipFileName = fd.BatchType + "_" + fd.BatchNo + ".zip";

//        // Empty temp folder to collect files for zip
//        string WorkingOutputFolder = GetWorkingDir(@"Output\", empty: true);
//        string PDFFolder = GetWorkingDir(@"Output\PDF\", empty: true);
//        string XMLFolder = GetWorkingDir(@"Output\XML\", empty: true);

//        // Load data from data file
//        CSVDocument csvDoc = new CSVDocument(wf.WorkingPath) { Delimiter = ",", QuotedValues = true };
//        csvDoc.LoadFile();
//        List<Dictionary<string, string>> csvDataRows = csvDoc.ReadAllKeyed(ignoreDuplicateHeaderKeys: true);
//        csvDoc.UnloadFile();

//        // Filter data rows to only include those that are finished
//        csvDataRows = csvDataRows.Where((r, i) => fr.Contains(i + 1)).ToList();

//        // Get PostedDate from first record's parent
//        string postedDate = csvDataRows[0].LookupLogged("LibertyTaggedTimestamp").Substring(0, 10);

//        // Check for missing pdfs
//        List<string> expectedPdfs = new List<string>();
//        List<string> missingPdfs = new List<string>();

//        foreach (var row in csvDataRows)
//        {
//            string pdfFileName = row.LookupLogged("PCN TICKET NUMBER") + ".pdf";
//            expectedPdfs.Add(AwaitingUploadsDir + pdfFileName);

//            if (!File.Exists(AwaitingUploadsDir + pdfFileName))
//            {
//                missingPdfs.Add(AwaitingUploadsDir + pdfFileName);
//            }
//        }

//        Log.Write("Missing " + missingPdfs.Count + " of " + csvDataRows.Count + " pdfs.");

//        // Regenerate missing pdfs
//        if (missingPdfs.Count > 0)
//        {
//            // Restore images
//            PdfRegeneration.RestoreImages(
//                clientName: "Dartford",
//                fileName: Path.GetFileName(wf.SourcePath)
//            );

//            // Rebuild a csv file with only the missing rows
//            string regenerationDataPath = WorkingDir + Path.GetFileName(wf.WorkingPath);
//            CSVUtils.FilterToFileKeyed(
//                originalDataPath: wf.WorkingPath,
//                newDataPath: regenerationDataPath,
//                quotedValues: true,
//                delimeter: ",",
//                includeRecords: (dataRow, i) =>
//                {
//                    return missingPdfs.Contains(AwaitingUploadsDir + dataRow.LookupLogged("PCN TICKET NUMBER") + ".pdf");
//                }
//            );

//            Log.Write("Restoring PDfs");

//            try
//            {
//                PdfRegeneration.RestorePdfs(
//                    dataPath: regenerationDataPath,
//                    dataProcessingDir: PDfRegenerationDir,
//                    expectedPdfPaths: missingPdfs,
//                    minutesTimeout: 10 + (missingPdfs.Count / 10)
//                );
//            }
//            catch (TimeoutException)
//            {
//                PdfRegeneration.RestorePdfs(
//                     dataPath: regenerationDataPath,
//                     dataProcessingDir: PDfRegenerationDir,
//                     expectedPdfPaths: missingPdfs,
//                     minutesTimeout: 10 + (missingPdfs.Count / 10)
//                );
//            }
//        }

//        // Create the XML file
//        XElement currentBatchItems;
//        XElement xmlRoot = new XElement("Batches");
//        xmlRoot.Add(
//            new XElement("Batch",
//                new XElement("BatchNo", fd.BatchNo),
//                new XElement("DocType", fd.BatchType),
//                currentBatchItems = new XElement("Items")
//            )
//        );

//        // Copy PDFs to temp dir
//        foreach (string pdfPath in expectedPdfs)
//        {
//            FileUtils.CopyLogged(pdfPath, FileUtils.UniqueFilename(WorkingOutputFolder + Path.GetFileName(pdfPath)));

//            currentBatchItems.Add(
//                new XElement("Item",
//                    new XElement("Reference", Path.GetFileNameWithoutExtension(pdfPath)),
//                    new XElement("Filename", Path.GetFileName(pdfPath)),
//                    new XElement("PostedString", "Document printed by Libertys and poster by Royal Main on " + postedDate),
//                    new XElement("rdnumber"),
//                    new XElement("ChequeNo"),
//                    new XElement("SortCode")
//                )
//            );
//        }

//        Log.Write("Saving xml batch file: " + XMLFolder + XmlFileName);
//        xmlRoot.Save(XMLFolder + XmlFileName);

//        ZipUtils.CompressLogged(WorkingOutputFolder + "*", WorkingDir + ZipFileName);

//        // Copy file to local archive
//        if (LocalArchiveDir != null)
//        {
//            FileUtils.CopyLogged(WorkingDir + ZipFileName, LocalArchiveDir + ZipFileName, overwrite: true);
//        }

//        // Archive original pdfs
//        foreach (string pdfPath in expectedPdfs)
//        {
//            FileUtils.DeleteLogged(pdfPath, disableWhenDebugging: true);
//        }
//    }

//    private void ProcessXmlFiles()
//    {

//    }
//}
#endregion

#region Dartford Shit V.2 - I think I figured it out...
//class DartfordUpload : BaseDownloadScript
//{
//    string DataPickupDir;
//    string LocalArchiveDir;
//    string SourceArchiveDir;
//    string PPPdfRegenerationDir;

//    private void ChangeDelimiter(string oSymb, string rSymb, string file)
//    {
//        string text = File.ReadAllText(file);
//        Log.Write("Changing Delimiters");
//        text = text.Replace(oSymb, rSymb);
//        File.WriteAllText(file, text);
//    }

//    public DartfordUpload()
//    {
//        #region Global Variables
//        DebugLogDir = HostPath.ppwatch_1 + @"Data\Dartford\Process\Uploads\DebugLogs\";
//        WorkingDir = HostPath.ppwatch_1 + @"Data\Dartford\Process\Uploads\WorkingDir\";
//        LocalSftpDir = HostPath.sftp + @"Dartford DFFCP\Taranto\DataOut\";

//        DataPickupDir = HostPath.ppwatch_1 + @"Data\Dartford\Uploads\Output\";

//        LocalArchiveDir = HostPath.mass_store + @"MassStorage\ParkingArchive\Dartford\ConfirmationsComplete\Uploaded\";

//        SourceArchiveDir = HostPath.mass_store + @"MassStorage\ParkingArchive\Dartford\SourceArchive\";

//        PPPdfRegenerationDir = HostPath.ppwatch_1 + @"Data\Dartford\Uploads\";
//        #endregion

//        // Check for space on the server before processing
//        MinimumDiskSpace(HostPath.ppwatch_1 + @"Data\Dartford\", 2/*GB*/);

//        // Create a mutex
//        MutexUtils.OpenMutexCached("Dartford");

//        DirUtils.RecreateLogged(WorkingDir);

//        // Start of the script...
//        Log.Write("=========================================================");

//        // Collect the data files
//        var WorkingFiles = GatherLocalData<WorkingFile>(
//            from: DataPickupDir,
//            to: WorkingDir,
//            searchPattern: "*.txt",
//            searchOption: SearchOption.TopDirectoryOnly,
//            clearDestination: true
//        );

//        WorkingFiles = WorkingFiles.Where(wf => Regex.IsMatch(wf.WorkingPath, "_MTCNODR_")).ToList();

//        if (WorkingFiles.Count == 0)
//        {
//            Log.Write("No data!");
//            return;
//        }

//        foreach (var wf in WorkingFiles)
//        {
//            // Change the delimeters of the data files
//            ChangeDelimiter("\t", ",", wf.WorkingPath);
//            // Provide sanity check for the data file initially
//            FilenameDetails fd = new FilenameDetails(wf.WorkingPath);
//            Process_The_Shit(wf, fd);
//        }
//    }

//    private void Process_The_Shit(WorkingFile wf, FilenameDetails fd)
//    {
//        string batchName = wf.SourcePath;

//        // Check the DB for the data file and see if the file exists...
//        Log.Write("Querying database: " + batchName);
//        ConfigThunderSnow.Local.UseServer = "LIVE";
//        List<Job> jobs = Job.GetJobsWithFilename(clientName: "Dartford", fileName: Path.GetFileName(batchName));
//        Log.Write("Found " + jobs.Count + " jobs for file " + batchName);

//        #region Sanity Checks
//        bool skipbatch = false;

//        // Now check how many of these jobs are in the proper state.
//        if (Job.JobsNotInTask(jobs, TaskType.Confirmation).Count > 0)
//        {
//            Log.Write("Unfinished jobs found for this file, skipping.");
//            skipbatch = true;
//        }

//        if (skipbatch == true)
//        {
//            // Exit and move onto another datafile if any are left.
//            Log.Write("Skipping batch: " + batchName);
//            return;
//        }

//        // Get the number of active from the list of jobs specified above
//        var finishedRecordNumbers = Job.ActiveRecordNumbersForJobs(jobs);

//        // If no active records, send email and skip
//        if (finishedRecordNumbers.Count == 0)
//        {
//            EmailUtils.ErrorReport("No jobs matching file. Archive manually", "No jobs matching filename: " + wf.SourcePath);
//            return;
//        }
//        #endregion Sanity Checks

//        Log.Write("Processing Batch " + batchName);

//        try
//        {
//            using (var taskProcess = new JobTaskProcess(TaskType.Confirmation, jobs, disableWhenDebugging: false))
//            {

//                // Load data from the csv for processing
//                var csvDoc = new CSVDocument(wf.WorkingPath) { Delimiter = ",", QuotedValues = true };
//                csvDoc.LoadFile();

//                // Read all the rows from the datafile
//                List<Dictionary<string, string>> CsvDataRows = csvDoc.ReadAllKeyed(ignoreDuplicateHeaderKeys: true);
//                csvDoc.UnloadFile();
//                string PDFSource = DataPickupDir + Path.GetFileNameWithoutExtension(wf.SourcePath) + @"\";
//                string tempPDFSource = DataPickupDir + Path.GetFileNameWithoutExtension(wf.SourcePath) + "-PDF-REGENERATON.txt" + @"\";
//                if (!Directory.Exists(PDFSource))
//                {
//                    Directory.CreateDirectory(PDFSource);
//                }
//                string[] listofpdfs = Directory.GetFiles(PDFSource, "*.pdf");
//                List<string> expectedPdfs = new List<string>();
//                List<string> missingPdfs = new List<string>();
//                foreach (var datarow in CsvDataRows)
//                {
//                    string pdfname = datarow.LookupLogged("PCN TICKET NUMBER") + ".pdf";
//                    expectedPdfs.Add(PDFSource + pdfname);
//                    if (!listofpdfs.Contains(PDFSource + pdfname))
//                    {
//                        missingPdfs.Add(PDFSource + pdfname);
//                    }
//                }

//                Log.Write("Missing " + missingPdfs.Count + " out of " + expectedPdfs.Count);

//                // Start the regeneration process for the missing pdfs

//                if (missingPdfs.Count > 0)
//                {
//                    // Restore images first
//                    PdfRegeneration.RestoreImages(clientName: "Dartford", fileName: Path.GetFileName(wf.SourcePath));

//                    // Process for restoring the pdfs
//                    // Create a data file with only the missing pdfs.
//                    // Note: For PlanetPress to regenerate the files, the data file created needs to be with the SAME DELIMITER as the original datafile.
//                    // otherwise the resulting restoration will be wrong.
//                    string regenerationpath = WorkingDir + Path.GetFileName(wf.WorkingPath) + "-PDF-REGENERATION.txt";
//                    CSVUtils.FilterToFileKeyed(
//                        originalDataPath: wf.WorkingPath,
//                        newDataPath: regenerationpath,
//                        quotedValues: true,
//                        delimeter: "\t",
//                        includeRecords: (dataRow, i) =>
//                        {
//                            return missingPdfs.Contains(PDFSource + dataRow.LookupLogged("PCN TICKET NUMBER") + ".pdf");
//                        }
//                    );

//                    Log.Write("Attempting pdf regneration process.");
//                    try
//                    {
//                        // Actual restoration of the datafiles
//                        PdfRegeneration.RestorePdfs(
//                            dataPath: regenerationpath,
//                            dataProcessingDir: PPPdfRegenerationDir,
//                            expectedPdfPaths: missingPdfs,
//                            minutesTimeout: 10 + (missingPdfs.Count / 5)
//                        );
//                    }
//                    catch (Exception ex)
//                    {
//                        Log.Write("An error occured when regenerating PDFs." + Environment.NewLine + ex);
//                    }
//                }

//                // After the regeneration, move all the pdfs that were already there as well as the ones that are regenerated into a temporary output folder.
//                string OutputDir = GetWorkingDir(@"Output\", empty: true);
//                try
//                {
//                    foreach (var pdfs in expectedPdfs)
//                    {
//                        FileUtils.CopyLogged(pdfs, FileUtils.UniqueFilename(OutputDir + Path.GetFileName(pdfs)));
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Log.Write("Error when moving files to the " + OutputDir + Environment.NewLine + ex);
//                }

//                string xmlFileName = fd.BatchType + "-" + fd.BatchNo + ".xml";
//                CreateXML(FileanemDetails: fd, DataRows: CsvDataRows, SavePath: OutputDir + xmlFileName);

//                string zipFileName = fd.BatchType + "_" + fd.BatchNo + ".zip";

//                try
//                {
//                    // Zip the contents of the output folder.
//                    ZipUtils.CompressLogged(OutputDir + "*", WorkingDir + zipFileName);

//                    // Copy the file to the local archive.
//                    FileUtils.CopyLogged(WorkingDir + zipFileName, LocalArchiveDir + zipFileName, overwrite: true);

//                    // Move the zip file to the server.
//                    FileUtils.CopyLogged(WorkingDir + zipFileName, LocalSftpDir + zipFileName, overwrite: true);

//                    if (File.Exists(DataPickupDir + Path.GetFileName(wf.SourcePath)))
//                    {
//                        FileUtils.DeleteLogged(DataPickupDir + Path.GetFileName(wf.SourcePath));
//                    }
//                    // Delete the output dir.
//                    DirUtils.DeleteLogged(OutputDir);

//                }
//                catch (ZipException ez)
//                {
//                    Log.Write("Error when zipping the files: " + Environment.NewLine + ez);
//                }
//                catch (Exception ex)
//                {
//                    Log.Write("Generic error: " + Environment.NewLine + ex);
//                }

//                taskProcess.Finished();
//            }

//            Log.Write("Finished Processing " + batchName);
//        }
//        catch (Exception ex)
//        {
//            throw new Exception("Error processing file: " + wf.WorkingPath, ex);
//        }
//    }

//    /// <summary>
//    /// Create the xml file recording the details for each data file with its corresponding pdfs.
//    /// </summary>
//    /// <param name="FileanemDetails">FilenameDetails</param>
//    /// <param name="DataRows">Dictionary values of the data rows in the datafile</param>
//    /// <param name="SavePath">Save location for the xml file</param>
//    private void CreateXML(FilenameDetails FileanemDetails, List<Dictionary<string, string>> DataRows, string SavePath)
//    {
//        string PostedDate = DataRows[0].LookupLogged("LibertyTaggedTimestamp").Substring(0, 10);
//        // Only repeats once per xml file
//        XElement currentBatchItems;
//        XElement xmlRoot = new XElement("Batches");
//        xmlRoot.Add(
//            new XElement("Batch",
//                new XElement("Batch No", FileanemDetails.BatchNo),
//                new XElement("DocType", FileanemDetails.BatchType),
//                new XElement("PostedDate", PostedDate),
//                currentBatchItems = new XElement("Items") // Start the repeating header block for the xml file
//            )
//        );

//        foreach (var DataRow in DataRows)
//        {
//            currentBatchItems.Add(
//                new XElement("Item",
//                    new XElement("Reference", DataRow.LookupLogged("PCN TICKET NO")),
//                    new XElement("Filename", DataRow.LookupLogged("PCN TICKET NO") + ".pdf"),
//                    new XElement("PostedString", "Document printed by Libertys and posted by Royal Mail on " + PostedDate),
//                    new XElement("rdnumber"),
//                    new XElement("ChequeNo"),
//                    new XElement("SortCode")
//                )
//            );
//        }

//        Log.Write("Saving xml batch file at: " + SavePath);
//        xmlRoot.Save(SavePath);
//    }
//}
#endregion

#region Dartford Shit V.2.5 - Ok! new problem, the file is being used by the csv creation... (╯°□°）╯︵ ┻━┻
//class DartfordUpload2 : BaseDownloadScript
//{
//    string DataPickupDir;
//    string LocalArchiveDir;
//    string SourceArchiveDir;
//    string PPPdfRegenerationDir;
//    string PdfFinalLocation;

//    private void ChangeDelimiter(string originalDelimiter, string newDelimiter, string DataFile)
//    {
//        string text = File.ReadAllText(DataFile);
//        Log.Write("Changing Delimiters");
//        text = text.Replace(originalDelimiter, newDelimiter);
//        File.WriteAllText(DataFile, text);
//    }

//    public DartfordUpload2()
//    {
//        DebugLogDir = HostPath.ppwatch_3 + @"data\Dartford\Process\Uploads\DebugLogs\";
//        WorkingDir = HostPath.ppwatch_3 + @"data\Dartford\Process\Uploads\WorkingDir\";
//        //LocalSftpDir = HostPath.sftp + @"Dartford DFFCP\Taranto\DataOut\";
//        LocalSftpDir = HostPath.ppwatch_3 + @"data\Dartford\Uploads\TEST\SFTP\";

//        DataPickupDir = HostPath.ppwatch_3 + @"data\Dartford\Uploads\Output\";

//        LocalArchiveDir = HostPath.mass_store + @"MassStorage\ParkingArchive\Dartford\ConfirmationsComplete\Uploaded\";

//        SourceArchiveDir = HostPath.mass_store + @"MassStorage\ParkingArchive\Dartford\SourceArchive\";

//        PPPdfRegenerationDir = HostPath.ppwatch_3 + @"data\Dartford\Uploads\";

//        // Check for space on the server before processing
//        MinimumDiskSpace(HostPath.ppwatch_1 + @"Data\Dartford\", 2/*GB*/);

//        // Create a mutex
//        MutexUtils.OpenMutexCached("Dartford");

//        DirUtils.RecreateLogged(WorkingDir);

//        // Start of the script...
//        Log.Write("=========================================================");

//        // Collect the data files
//        var WorkingFiles = GatherLocalData<WorkingFile>(
//            from: DataPickupDir,
//            to: WorkingDir,
//            searchPattern: "*.txt",
//            searchOption: SearchOption.TopDirectoryOnly,
//            clearDestination: true
//        );

//        WorkingFiles = WorkingFiles.Where(wf => Regex.IsMatch(wf.WorkingPath, "_MTCNODR_")).ToList();

//        if (WorkingFiles.Count == 0)
//        {
//            Log.Write("No data!");
//            return;
//        }

//        foreach(var wf in WorkingFiles)
//        {
//            Log.Write("--------------------------------------------------------");
//            Log.Write("Processing file: " + wf.SourcePath);

//            Log.Write("Changing delimiters from Pipe to Comma for file: " + wf.WorkingPath);
//            ChangeDelimiter("\t", ",", wf.WorkingPath);

//            // Get Details from the file name
//            var DartfordFilenameDetails = new FilenameDetails(wf.SourcePath);

//            // Lookup database for completed record numbers
//            Log.Write("Querrying database: " + Path.GetFileName(wf.SourcePath));
//            ConfigThunderSnow.Local.UseServer = "LIVE";
//            List<Job> jobs = Job.GetJobsWithFilename(clientName: "Dartford", fileName: Path.GetFileName(wf.SourcePath));

//            Log.Write("Found " + jobs.Count + " jobs for file: " + Path.GetFileName(wf.SourcePath));
//            if(Job.JobsNotInTask(jobs, TaskType.Confirmation).Count > 0)
//            {
//                // if unfinished jobs, skip
//                Log.Default.Write("Unifnished jobs found for this file, skipping.");
//                return;
//            }

//            // Collate all the number of jobs that are finished for the data file
//            List<int> FinishedRecordNumber = Job.ActiveRecordNumbersForJobs(jobs);

//            if(FinishedRecordNumber.Count == 0)
//            {
//                // If no active records, send email and skip
//                EmailUtils.ErrorReport("No jobs matching file. Archive manually", "No jobs matching filename: " + wf.WorkingPath);
//                continue;
//            }

//            try
//            {
//                using (var taskProcess = new JobTaskProcess(TaskType.Confirmation, jobs, disableWhenDebugging: false))
//                {
//                    ProcessUpload(wf, DartfordFilenameDetails, FinishedRecordNumber);
//                    taskProcess.Finished();
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new Exception("Error processing file: " + wf.SourcePath, ex);
//            }
//        }
//    }

//    private void ProcessUpload(WorkingFile wf, FilenameDetails fd, List<int> frn)
//    {
//        // Create empty output dir
//        string WorkingOutputDir = GetWorkingDir(@"Output\", empty: true);

//        // Load data from csv file
//        var csvDoc = new CSVDocument(wf.WorkingPath) { Delimiter = ",", QuotedValues = true};
//        csvDoc.LoadFile();
//        List<Dictionary<string, string>> csvDataRows = csvDoc.ReadAllKeyed(ignoreDuplicateHeaderKeys: true);
//        csvDoc.UnloadFile();

//        // Filter data rows to include only those that are finished
//        csvDataRows = csvDataRows.Where((r, i) => frn.Contains(i + 1)).ToList();

//        // Get the posted data from the first record's parent
//        string postedDate = csvDataRows[0].LookupLogged("LibertyTaggedTimestamp").Substring(0, 10);

//        // Check for missing pdfs
//        List<string> expectedPdfPaths = new List<string>();
//        List<string> missingPdfPaths = new List<string>();

//        PdfFinalLocation = DataPickupDir + Path.GetFileNameWithoutExtension(wf.SourcePath) + @"\";
//        Directory.CreateDirectory(PdfFinalLocation);
//        // Loop through the data rows
//        foreach(var csvDataRow in csvDataRows)
//        {
//            string pdfFileName = csvDataRow.LookupLogged("PCN TICKET NUMBER") + ".pdf";
//            expectedPdfPaths.Add(PdfFinalLocation + pdfFileName);
//            if(!File.Exists(PdfFinalLocation + pdfFileName))
//            {
//                missingPdfPaths.Add(PdfFinalLocation + pdfFileName);
//            }
//        }
//        Log.Default.Write("Missing " + missingPdfPaths.Count + " of " + csvDataRows.Count() + " pdfs");

//        // Regenerate the missing pdfs
//        if(missingPdfPaths.Count > 0)
//        {
//            // Restore images
//            PdfRegeneration.RestoreImages(clientName: "Dartford", fileName: Path.GetFileName(wf.SourcePath));

//            // Restore pdfs
//            //--------------

//            // Write a data file with the exact same format as the original but only with the missing pdfs
//            string regenerationDataPath = WorkingDir + Path.GetFileName(wf.WorkingPath) + "-REGENERATON";
//            CSVUtils.FilterToFileKeyed(
//                originalDataPath: wf.WorkingPath,
//                newDataPath: regenerationDataPath,
//                quotedValues: true,
//                delimeter: "\t",
//                includeRecords: (datarow, i) =>
//                {
//                    return missingPdfPaths.Contains(PdfFinalLocation + datarow.LookupLogged("PCN TICKET NUMBER") + ".pdf");
//                }
//            );

//            try
//            {
//                // Restore the missng pdfs
//                PdfRegeneration.RestorePdfs(
//                    dataPath: regenerationDataPath,
//                    dataProcessingDir: PPPdfRegenerationDir,
//                    expectedPdfPaths: missingPdfPaths,
//                    minutesTimeout: 10 + (missingPdfPaths.Count / 5)
//                );
//            }
//            catch (TimeoutException)
//            {
//                // Restore the missng pdfs one more time
//                PdfRegeneration.RestorePdfs(
//                    dataPath: regenerationDataPath,
//                    dataProcessingDir: PPPdfRegenerationDir,
//                    expectedPdfPaths: missingPdfPaths,
//                    minutesTimeout: 10 + (missingPdfPaths.Count / 5)
//                );
//            }

//            // Gather the pdf files and put them in a unique f
//            foreach(string pdfPath in expectedPdfPaths)
//            {
//                FileUtils.CopyLogged(pdfPath, FileUtils.UniqueFilename(WorkingOutputDir + Path.GetFileName(pdfPath)));
//            }

//            var pdfPaths = Directory.GetFiles(WorkingOutputDir, "*.pdf", SearchOption.TopDirectoryOnly);

//            // Create the XML file with the processed batch's details
//            Log.Write("Saving XML file with batch details.");                
//            XElement currentBatchItems;
//            XElement xmlRoot = new XElement("Batches");
//            xmlRoot.Add(
//                new XElement("Batch",
//                    new XElement("BatchNo", fd.BatchNo),
//                    new XElement("DocType", fd.BatchType),
//                    new XElement("PostedDate", postedDate),
//                    currentBatchItems = new XElement("Items")
//                )
//            );

//            foreach(string pdfPath in pdfPaths)
//            {
//                currentBatchItems.Add(
//                    new XElement("Item", 
//                        new XElement("Reference", Path.GetFileNameWithoutExtension(pdfPath)),
//                        new XElement("Filename", Path.GetFileName(pdfPath)),
//                        new XElement("PostedString", "Document printed by Libertys and posted by Royal Mail on " + postedDate),
//                        new XElement("rdnumber"),
//                        new XElement("ChequeNo"),
//                        new XElement("SortCode") 
//                    )
//                );
//            }

//            // Save the xml file
//            string xmlFileName = fd.BatchType + "-" + fd.BatchNo + ".xml";
//            Log.Write("Saving xml batch file: " + WorkingOutputDir + xmlFileName);
//            xmlRoot.Save(WorkingOutputDir + xmlFileName);

//            // Create a zip file that includes the contents of the WorkingOutputDir
//            string zipFileName = fd.BatchType + "_" + fd.BatchNo + ".zip";
//            ZipUtils.CompressLogged(WorkingOutputDir + "*", WorkingDir + zipFileName);

//            try
//            {
//                // Move the zip file to mass storage
//                string massStorageDest = HostPath.mass_store + @"MassStorage\ParkingArchive\Dartford\ConfirmationsComplete\Uploaded\";
//                FileUtils.CopyLogged(WorkingDir + zipFileName, massStorageDest + zipFileName, true);

//                // Move the zip file to sftp
//                FileUtils.CopyLogged(WorkingDir + zipFileName, LocalSftpDir + zipFileName);
//            }
//            catch (Exception ex)
//            {
//                Log.Write("Error archiving the files, Please check the " + WorkingDir + " for the zip file" + Environment.NewLine + ex);
//            }

//            try
//            {
//                string tempDir = HostPath.ppwatch_3 + @"data\Dartford\Uploads\TEST\SourceStorage\";

//                Log.Write("Moving files from " + DataPickupDir + " to " + tempDir);
//                MoveToArchive(Path.GetFileName(wf.SourcePath), DataPickupDir, tempDir);

//                Log.Write("Deleting original files and folder in " + DataPickupDir);
//                DeleteFromSource(Path.GetFileName(wf.SourcePath), DataPickupDir);
//                // Deleting the data folder
//                DirUtils.DeleteLogged(DataPickupDir + Path.GetFileNameWithoutExtension(wf.WorkingPath));

//                // Deleting the data file
//                FileUtils.DeleteLogged(DataPickupDir + Path.GetFileName(wf.WorkingPath));
//            }
//            catch(Exception ex)
//            {
//                Log.Write("Error Deleting the files at the end. Please check that the folder still exists." + Environment.NewLine + ex);
//            }
//        }
//    }

//    private void MoveToArchive(string fileName, string rootDir, string archiveDir)
//    {
//        try
//        {
//            FileUtils.CopyLogged(rootDir + fileName, archiveDir + fileName, true);
//            if (!Directory.Exists(archiveDir + Path.GetFileNameWithoutExtension(fileName)))
//            {
//                DirUtils.CreateLogged(archiveDir + Path.GetFileNameWithoutExtension(fileName));
//            }

//            DirUtils.CopyRecursiveLogged(rootDir + Path.GetFileNameWithoutExtension(fileName) + @"\", archiveDir + Path.GetFileNameWithoutExtension(fileName) + @"\");
//        }
//        catch (Exception ex)
//        {
//            Log.Write("Error whilst moving files and folders" + Environment.NewLine + ex);
//        }
//    }

//    private void DeleteFromSource(string fileName, string rootDir)
//    {
//        try
//        {
//            FileUtils.DeleteLogged(rootDir + fileName, disableWhenDebugging: false);
//            string folderName = rootDir + Path.GetFileNameWithoutExtension(fileName);

//            if(Directory.Exists(folderName))
//            {
//                DirUtils.DeleteLogged(folderName + @"\");
//            }
//        }
//        catch (Exception ex)
//        {
//            Log.Write("Error occured when deleting source files and folder." + Environment.NewLine + ex);
//        }
//    }
//}
#endregion