using LibertyUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSCP;

namespace FizzBuzz
{
    class EalingUploadFtp : BaseDownloadScript
    {
        public EalingUploadFtp()
        {
            DebugLogDir = @"C:\PPProject\c# Projects\Test\uploads\ppwatch\Ealing\DebugLog\";
            CredentialsPath = HostPath.ppwatch_1 + @"LibertyConfig\ExternalCredentials.xml";
            LibertyConfigExternalCredentials.Credential ftpCredentials = Credentials.Get("Parking - Ealing Uploads");
            string[] gatherFilesToUpload = Directory.GetFiles(@"C:\PPProject\c# Projects\Test\uploads\ppwatch\Ealing\", "COR*.zip");
            foreach (string file in gatherFilesToUpload)
            {
                Log.Write("Moving " + file + " to " + "/TEST/");
                MoveToServer(ftpCredentials, "/TEST/", file);
            }
        }

        private SessionOptions EstablishConnections(LibertyConfigExternalCredentials.Credential credentials)
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

        private void MoveToServer(LibertyConfigExternalCredentials.Credential credentials, string archiveFolder, string localZipFile)
        {
            // Move those files to the ftp
            SessionOptions sessionOptions = EstablishConnections(credentials);
            try
            {
                using (Session session = new Session())
                {
                    session.Open(sessionOptions);
                    if (session.Opened)
                    {
                        Log.Write("Archiving files...");
                        // If a folder does not need to be created for the zip to be put in
                        session.PutFiles(localZipFile, archiveFolder);
                        Log.Write("Finished moving " + localZipFile + " to the server.");
                    }
                }
                Log.Write("Archiving Finished!");
            }
            catch (Exception ex)
            {
                Log.Write("Server File Transfer Error: " + Environment.NewLine + ex);
            }
        }

    }
}
