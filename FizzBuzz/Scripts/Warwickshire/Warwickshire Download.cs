using LibertyUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WinSCP;

namespace FizzBuzz.Scripts.Warwickshire
{
    /*
        Run down of what the script does.
        The script has 2 parts to it. The PDF part and the Data file part.
        
        For the first part:
        - Download all the pdf's from the PCN folder.
        - Some are NoR files and some are Correspondence files
        - Once the files are downloaded, create a csv file with all the PDFs and their names.
        - Warrwickshire_NoR_sequencenumber.txt for the NoR files and Warwickshire_Correspondence_sequencenumber.txt for the Corres files.
        - Once this has been done, send the newly created .txt files to the Live folder and the PDFs to the Image folder.
        - Then delete these files from the server.

        For the second part:
        - Download all the .csv files and the .WRK files from the Permits and the PCN folders
        - The .WRK files exist inside the PCN folder, and the .csv files exist inside the permits folder
        - Once the download is completed, create an inlist that details the number of rows inside the data file.
        - Move the data files to the live data directory for thundersnow to pickup.
        - Remove the files downloaded from the server.
    */


    class WarwickshireDownload : BaseDownloadScript
    {
        LibertyConfigExternalCredentials.Credential _ftpCredentials;
        string _sequencePath;
        string _sequenceNumber;
        string _pdfPrefix;
        string _inListFilename;
        string _inListPath;
        public WarwickshireDownload()
        {
            DebugLogDir = @"C:\PPProject\c# Projects\Test\ppwatch\Warwickshire\DebugLogDir\";
            WorkingDir = @"C:\PPProject\c# Projects\Test\ppwatch\Warwickshire\WorkingDir\";
            LiveDataDir = @"C:\PPProject\c# Projects\Test\ppwatch\Warwickshire\LiveDataDir\";
            PPImagesDir = @"C:\PPProject\c# Projects\Test\ppwatch\Warwickshire\PPImageDir\";

            _sequencePath = @"C:\PPProject\c# Projects\Test\ppwatch\Warwickshire\NextBatchId.txt";
            _sequenceNumber = NextSequenceNumber(_sequencePath);
            _pdfPrefix = _sequenceNumber + "_";

            _inListFilename = @"Warwickshire-" + DateTime.Now.ToString("dd-MM-yyyy") + ".csv";
            _inListPath = @"C:\PPProject\c# Projects\Test\ppwatch\Warwickshire\";

            CredentialsPath = HostPath.ppwatch_2 + @"LibertyConfig\ExternalCredentials.xml";
            _ftpCredentials = Credentials.Get("NSLCloud");

            RunPdfProcess();

            RunDataFileProcess();

            Log.Write(GetType() + " complete.");
        }

        private void RunPdfProcess()
        {
            try
            {
                // download pdfs
                List<WorkingFile> allWorkingFiles = GatherFTPData<WorkingFile>(
                   credential: Credentials.Get("NSLCloud"),
                   from: @"Out/Warwickshire/PCN/",
                   to: WorkingDir,
                   searchPattern: "*.pdf",
                   searchOption: SearchOption.TopDirectoryOnly,
                   clearDestination: true
                );

                Log.Write("----------------------------------------------------");
                Log.Write("Processing PCNs");

                List<string> archiveCommands = new List<string>();
                try
                {
                    IEnumerable<WorkingFile> workingFiles = allWorkingFiles.Where(wf => Regex.IsMatch(Path.GetFileName(wf.SourcePath), @"^NOR.*?.pdf$", RegexOptions.IgnoreCase));

                    //check for files
                    if (workingFiles.Count() == 0)
                    {
                        Log.Write("No data");
                    }
                    else
                    {
                        string csvFilePath = WorkingDir + "Warwickshire_NoR_" + _sequenceNumber + ".txt";

                        // generate csv
                        Log.Write("Generating csv from list of pdfs: " + csvFilePath);
                        CSVDocument csv = new CSVDocument();
                        //csv.AddRow("File");
                        foreach (WorkingFile wf in workingFiles)
                        {
                            csv.AddRow(_pdfPrefix + Path.GetFileName(wf.SourcePath));
                        }
                        csv.SaveAs(csvFilePath);
                        csv.UnloadFile();

                        // move files to live
                        foreach (WorkingFile wf in workingFiles)
                        {
                            FileUtils.MoveLogged(wf.WorkingPath, PPImagesDir + _pdfPrefix + Path.GetFileName(wf.WorkingPath),
                                disableWhenDebugging: true);
                        }
                        FileUtils.MoveLogged(csvFilePath, LiveDataDir + Path.GetFileName(csvFilePath),
                                disableWhenDebugging: true);

                        // add archive commands
                        foreach (WorkingFile wf in workingFiles)
                        {
                            archiveCommands.Add("rm " + "\"" + wf.SourcePath + "\"");
                        }
                    }


                    Log.Write("----------------------------------------------------");
                    Log.Write("Processing Corres");

                    workingFiles = allWorkingFiles.Where(wf => Regex.IsMatch(Path.GetFileName(wf.SourcePath), @"^(ZQ|NTOZQ).*?.pdf$", RegexOptions.IgnoreCase));

                    //check for files
                    if (workingFiles.Count() == 0)
                    {
                        Log.Write("No data");
                    }
                    else
                    {
                        string csvFilePath = WorkingDir + "Warwickshire_Correspondence_" + _sequenceNumber + ".txt";

                        // generate csv
                        Log.Write("Generating csv from list of pdfs: " + csvFilePath);
                        CSVDocument csv = new CSVDocument();
                        //csv.AddRow("File");
                        foreach (WorkingFile wf in workingFiles)
                        {
                            csv.AddRow(_pdfPrefix + Path.GetFileName(wf.SourcePath));
                        }
                        csv.SaveAs(csvFilePath);
                        csv.UnloadFile();

                        // move files to live
                        foreach (WorkingFile wf in workingFiles)
                        {
                            FileUtils.MoveLogged(wf.WorkingPath, PPImagesDir + _pdfPrefix + Path.GetFileName(wf.WorkingPath),
                                disableWhenDebugging: true);
                        }
                        FileUtils.MoveLogged(csvFilePath, LiveDataDir + Path.GetFileName(csvFilePath),
                                disableWhenDebugging: true);

                        // add archive command
                        foreach (WorkingFile wf in workingFiles)
                        {
                            archiveCommands.Add("rm " + "\"" + wf.SourcePath + "\"" + Environment.NewLine);
                        }
                    }
                }
                catch (Exception ex)
                {
                    EmailUtils.ErrorReport(ex);
                }
                finally
                {
                    if (archiveCommands.Count() > 0)
                    {
                        Log.Write("----------------------------------------------------");
                        Log.Write("Archiving completed files");
                        ExecuteFTPCommand(
                            Credentials.Get("NSLCloud"),
                            string.Join("\n", archiveCommands),
                            disableWhenDebugging: true);
                    }
                }


                Log.Write("=========================================================");
                Log.Write(this.GetType().Name + " complete!");
            }
            catch (Exception ex)
            {

            }
        }

        private void RunDataFileProcess()
        {
            List<string> archiveCommands = new List<string>();
            foreach (string pickupLocation in new[] { @"/home/Liberty/Out/Warwickshire/TEST/" })
            {

                string dataPattern = pickupLocation.Contains("Permits") ? "*.csv" : "*.WRK";
                string type = pickupLocation.Contains("Permits") ? "Permits" : "StatDocs";

                try
                {
                    Log.Write("=========================================================");
                    //Log.Write("Processing " + Type);

                    List<WorkingFile> workingFiles = GatherFTPData<WorkingFile>(
                        credential: Credentials.Get("NSLCloud"),
                        from: pickupLocation,
                        to: WorkingDir,
                        searchPattern: "*",
                        searchOption: SearchOption.TopDirectoryOnly,
                        clearDestination: true
                    );

                    //check for files
                    if (workingFiles.Count() == 0)
                    {
                        Log.Write("No data");
                        continue;
                    }
                    else
                    {
                        // move files to live
                        foreach (WorkingFile wf in workingFiles)
                        {
                            try
                            {
                                BuildInlist(wf);

                                FileUtils.MoveLogged(wf.WorkingPath, LiveDataDir + (Path.GetFileName(wf.WorkingPath)),
                                    disableWhenDebugging: true);

                                RemoveFromServer(wf.SourcePath);

                            }
                            catch (Exception ex)
                            {
                                Log.Write("Errored for file: " + Path.GetFileName(wf.WorkingPath) + Environment.NewLine + ex);
                                continue;
                            }

                            //try
                            //{
                            //    archiveCommands.Add("rm \"" + wf.SourcePath + "\"");
                            //    if (archiveCommands.Count() > 0)
                            //    {
                            //        Log.Write("----------------------------------------------------");
                            //        Log.Write("Archiving completed files");
                            //        ExecuteFTPCommand(
                            //            credential: Credentials.Get("NSLCloud"),
                            //            command: string.Join("\n", archiveCommands),
                            //            disableWhenDebugging: true);
                            //    }
                            //    Log.Write("Archive finished");
                            //}
                            //catch (Exception ex)
                            //{
                            //    Log.Write("Error deleting file from server. File: " + wf.SourcePath + Environment.NewLine + ex);
                            //    EmailUtils.ErrorReport(ex);
                            //    continue;
                            //}
                        }
                    }
                }
                catch (Exception ex)
                {
                    //EmailUtils.ErrorReport(ex);
                    Log.Write(ex);
                    continue;
                }
            }
        }

        private void BuildInlist(WorkingFile wf)
        {
            int lineCount = File.ReadAllLines(wf.WorkingPath).Count() - 1;
            Console.WriteLine("Inlist Line: " + CSVDocument.QuoteWrapCommaDelimit(new string[] { "Warwickshire", DateTime.Now.ToString("dd-MM-yyyy"), DateTime.Now.ToString("HH:mm:ss"), Path.GetFileName(wf.WorkingPath), lineCount.ToString() }) + Environment.NewLine);
            File.AppendAllText(_inListPath + _inListFilename, CSVDocument.QuoteWrapCommaDelimit(new string[] { "Warwickshire", DateTime.Now.ToString("dd-MM-yyyy"), DateTime.Now.ToString("HH:mm:ss"), Path.GetFileName(wf.WorkingPath), lineCount.ToString() }) + Environment.NewLine);
        }

        private SessionOptions EstablishConnection()
        {
            int port = int.Parse(_ftpCredentials.Server.Substring(_ftpCredentials.Server.LastIndexOf(':') + 1));
            string server = _ftpCredentials.Server;
            int serverIndex = _ftpCredentials.Server.IndexOf(':');
            if (serverIndex > 0) { server = server.Substring(0, serverIndex); }

            SessionOptions sessionOptions = new SessionOptions
            {
                Protocol = Protocol.Sftp,
                HostName = server,
                PortNumber = port,
                UserName = _ftpCredentials.Username,
                Password = _ftpCredentials.Password,
                GiveUpSecurityAndAcceptAnySshHostKey = true // This needs changing to have it go through a security protocol
            };
            return sessionOptions;
        }

        private void RemoveFromServer(string fileName)
        {
            SessionOptions sessionOptions = EstablishConnection();
            Session session = new Session();
            try
            {
                using (session)
                {
                    session.Open(sessionOptions);
                    if (session.Opened)
                    {
                        Log.Write("Removing files");
                        session.RemoveFiles(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("Error Deleting files from the server: " + Environment.NewLine + ex);
            }
        }
    }
}
