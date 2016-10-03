using LibertyUtils;
using System;
using System.IO;
using WinSCP;

namespace FizzBuzz.Tools
{
    class FtpFileTransferTest : BaseDownloadScript
    {
        string _zipFolder;

        LibertyConfigExternalCredentials.Credential _ftpCredentials;

        public FtpFileTransferTest()
        {
            DebugLogDir = @"C:\PPProject\c# Projects\Test\uploads\ppwatch\Test\DebugLog\";
            WorkingDir = @"C:\PPProject\c# Projects\Test\ppwatch\Ealing\Upload\WorkingFolder\";
            LiveDataDir = @"C:\PPProject\c# Projects\Test\ppwatch\Ealing\Upload\AwaitingUpload\";

            CredentialsPath = HostPath.ppwatch_1 + @"LibertyConfig\ExternalCredentials.xml";
            _ftpCredentials = Credentials.Get("Capita - Production Server");

            _zipFolder = @"C:\PPProject\c# Projects\Test\FizzBuzzTests\FTPUtilsUpload\";

            MutexUtils.OpenMutexUnique("~TEST~");
            DirUtils.RecreateLogged(WorkingDir);

            Log.Write("Program Starts Here");
            //MoveToServerFTPSingleFile(@"/Processed Permits/");
            CheckConnection();
        }

        private static void SessionFileTransferProgress(object sender, FileTransferProgressEventArgs e)
        {
            // New Line for every new File
            if ((_lastFileName != null) && (_lastFileName != e.FileName))
            {
                Console.WriteLine();
            }

            Console.Write("\r{0} ({1:P0})", e.FileName, e.FileProgress);

            // Remember a name of the last file reported
            _lastFileName = e.FileName;
        }

        private string[] GatherTheFiles()
        {
            string[] zipFilesToArchive = Directory.GetFiles(_zipFolder);
            return zipFilesToArchive;
        }
        private static string _lastFileName;
        private int _numberUploaded;

        private void SendEmail()
        {
            int filesInNumeber = GatherTheFiles().Length;
            int filesUploaded = _numberUploaded;

            Log.Write("Number of files came in: " + filesInNumeber + Environment.NewLine + "Number of files uploaded: " + filesUploaded);
        }
        private void CheckConnection()
        {
            //Move these files to the ftp
            SessionOptions sessionOptions = EstablishConnection();
            Session session = new Session();
            try
            {
                using (session)
                {
                    session.Open(sessionOptions);
                    if (session.Opened)
                    {
                        Log.Write("Session Opened");
                    }
                    else
                    {
                        Log.Write("No Connection");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("Exception: " + ex);
            }
        }
        private void MoveToServerFtpSingleFile(string serverFolder)
        {
            SessionOptions sessionOptions = EstablishConnection();
            Session session = new Session();
            try
            {
                string count;
                using (session)
                {
                    session.Open(sessionOptions);
                    if(session.Opened)
                    {
                        //if(session.FileExists(serverFolder + Path.GetFileName(@"C:\RG Scripts\FizzBuzz\Files\TEST.txt")))
                        //{
                        //    count = RandomUtils.IntegerString(5);    
                        //    Log.Write("File already exists");
                        //    session.PutFiles(@"C:\RG Scripts\FizzBuzz\Files\TEST.txt", serverFolder + Path.GetFileNameWithoutExtension(@"C:\RG Scripts\FizzBuzz\Files\TEST.txt") + "_" + count +".txt");
                        //}
                        Log.Write("Archive files...");
                        session.PutFiles(@"C:\RG Scripts\FizzBuzz\Files\TEST.txt", serverFolder);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("Exception: " + ex);
            }
        }

        private void MoveToServerFtp(string archiveFolder)
        {
            //Move these files to the ftp
            SessionOptions sessionOptions = EstablishConnection();
            Session session = new Session();
            try
            {
                using (session)
                {
                    session.FileTransferProgress += SessionFileTransferProgress;
                    session.Open(sessionOptions);
                    if (session.Opened)
                    {
                        Log.Write("Archiving Files...");
                        foreach (string wf in GatherTheFiles())
                        {
                            session.PutFiles(wf, archiveFolder);
                        }
                    }
                }
                Log.Write("Finished Archiving!");
            }
            catch (Exception ex)
            {
                Log.Write("Exception: " + ex);
            }
        }

        private void MoveToServerLibertyUtils()
        {
            string fileToSend = @"C:\PPProject\c# Projects\Test\FizzBuzzTests\FTPUtilsUpload\Test File.txt";
            string remoteLoc = @"/TEST/";
            string archiveCommand = string.Concat(" put ", fileToSend, " ", remoteLoc, Path.GetFileName(fileToSend));

            FTPUtils.Execute
            (
                credential: _ftpCredentials,
                command: archiveCommand,
                disableWhenDebugging: false,
                useShellExecute: false
            );
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

            }
        }
    }
}
