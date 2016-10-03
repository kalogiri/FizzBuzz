using System;
using System.Collections.Generic;
using LibertyUtils;

namespace FizzBuzz.Scripts
{
    internal class SouthwarkCorresDownload : BaseDownloadScript
    {
        private readonly LibertyConfigExternalCredentials.Credential _ftpCredential;
        private readonly string _sequenceNumber;

        public SouthwarkCorresDownload()
        {
            DebugLogDir = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\SouthwarkCorresDownload\DebugLogDir\";
            WorkingDir = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\SouthwarkCorresDownload\WorkingDir\";
            LiveDataDir = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\SouthwarkCorresDownload\LiveDataDir\";
            PPImagesDir = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\SouthwarkCorresDownload\ImageDir\";

            _ftpCredential = Credentials.Get("Parking - Southwark");

            DirUtils.RecreateLogged(WorkingDir);

            string[] ftpFolders = { "/TEST/1st_Class/", "/TEST/2nd_Class/", "/TEST/Formal_Rej/" };

            MutexUtils.OpenMutexCached("Southwark");

            string sequenceFilePath = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\SouthwarkCorresDownload\SeqNumber.txt";
            _sequenceNumber = NextSequenceNumber(sequenceFilePath);

            Log.Write("Starting Download Process");

            ProcessDownload(ftpFolders);
        }

        private void ProcessDownload(IEnumerable<string> folders)
        {
            //string sequenceFilePath = HostPath.ppwatch_2 + @"Data\Southwark\SeqNumber.txt"; 

            
            string csvFirstClass = @"Southwark_Correspondence_1st" + "_" + DateTime.Now.ToString("yyyyMMdd") + "_" + _sequenceNumber + ".csv";
            string csvSecondClass = @"Southwark_Correspondence_2nd" + "_" + DateTime.Now.ToString("yyyyMMdd") + "_" + _sequenceNumber + ".csv";
            string csvFormalRej = @"Southwark_Correspondence_FR" + "_" + DateTime.Now.ToString("yyyyMMdd") + "_" + _sequenceNumber + ".csv";

            foreach (string folder in folders)
            {
                string csvFileName = folder.Contains("/1st_Class/") ? csvFirstClass /* if */
                                   : folder.Contains("/2nd_Class/") ? csvSecondClass /* else if */
                                   : csvFormalRej; /* else */

                Log.Write("=========================================================");

                Log.Write("Downloading files from " + folder.Trim('/'));

                List<WorkingFile> workingFiles = GatherFTPData<WorkingFile>(
                    credential: _ftpCredential,
                    from: folder,
                    to: WorkingDir,
                    searchPattern: "*.pdf", 
                    searchOption: System.IO.SearchOption.TopDirectoryOnly
                );

                if(workingFiles.Count == 0)
                {
                    Log.Write(folder.Trim('/') + " had 0 files. Skipping.");
                    continue;
                }

                CSVDocument csvDoc = new CSVDocument {Delimiter = "\n"};
                csvDoc.AddRow("PDF Name");
                List<string> deleteCommands = new List<string>();
                foreach(WorkingFile pdf in workingFiles)
                {
                    string pdfName = pdf.WorkingFileName;
                    try
                    {
                        csvDoc.AddRow(pdfName);
                        FileUtils.MoveLogged(pdf.WorkingPath, PPImagesDir + pdf.WorkingFileName, overwrite: true);
                        Log.Write("Deleting " + pdf.WorkingFileName + " from the server.");
                        deleteCommands.Add(" rm " + folder + pdfName);
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex);
                    }
                }

                csvDoc.SaveAs(WorkingDir + csvFileName);
                csvDoc.UnloadFile();

                try
                {
                    // Move the csv files from WorkingDir to LiveDir
                    Log.Write("Moving " + csvFileName + " to live data directory.");
                    FileUtils.MoveLogged(WorkingDir + csvFileName, LiveDataDir + csvFileName);
                    ExecuteFTPCommand(_ftpCredential, string.Join(Environment.NewLine, deleteCommands));
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                }

                Log.Write("=========================================================");
                Log.Write("Finished Processing files for " + folder.Trim('/'));
            }

            Log.Write("Southwark corres download finished!");
        }
    }
}
