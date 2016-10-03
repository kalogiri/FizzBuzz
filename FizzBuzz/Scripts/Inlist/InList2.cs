using LibertyUtils;
using System;
using System.Collections.Generic;
using System.IO;
using WinSCP;

namespace FizzBuzz.Scripts.Inlist
{
    class InList2 : BaseDownloadScript
    {
        string[] _serverLookupLocations =
        {
            @"/Stat Doc/DTE OFR/a/",
            @"/Stat Doc/DTE OFR/b/",
            @"/Stat Doc/CCTV CEO OFR/",
            @"/Stat Doc/BLE OFR/",
            @"/Correspondence/1st Class/",
            @"/Correspondence/2nd Class/"
        };

        LibertyConfigExternalCredentials.Credential _ftpCredentials;
        public InList2()
        {
            CredentialsPath = HostPath.ppwatch_1 + @"LibertyConfig\ExternalCredentials.xml";
            _ftpCredentials = Credentials.Get("Parking - Ealing");

            List<Tuple<string, int>> remoteFileInfo = new List<Tuple<string, int>>();

            // Gather data from the remote server
            SessionOptions sessionOptions = SessionConfig();
            Session session = new Session();
            int count;
            using (session)
            {
                session.Open(sessionOptions);
                if (session.Opened)
                {
                    RemoteDirectoryInfo directory;
                    foreach (string dir in _serverLookupLocations)
                    {
                        directory = session.ListDirectory(dir);
                        count = 0;
                        foreach (RemoteFileInfo rinfo in directory.Files)
                        {
                            if (!rinfo.IsDirectory && (rinfo.Name.Contains("PDF") || rinfo.Name.Contains("pdf"))) { count++; }
                        }
                        remoteFileInfo.Add(new Tuple<string, int>(dir, count));
                    }
                }
            }

            MakeInlist(remoteFileInfo, "Brent Download");
        }

        private void MakeInlist(List<Tuple<string, int>> fileInfo, string processType)
        {
            string time = DateTime.Now.ToString("H:mm:ss");
            string inlistFilename = HostPath.ppwatch_2 + @"Data\Ealing\Process\Inlist\" + processType + " Report_" + DateTime.Today.ToString("ddMMyyyy") + ".csv";
            if (!File.Exists(inlistFilename))
            {
                Log.Write("Creating reports file: " + inlistFilename);
                string header = "Location" + "," + "Total PDFs" + "\r\n";
                File.WriteAllText(inlistFilename, header);
            }
            string rows = string.Empty;
            rows += processType + " at: " + time + "\r\n";
            foreach (Tuple<string, int> info in fileInfo)
            {
                rows += info.Item1 + "," + info.Item2.ToString() + "\r\n";
            }
            File.AppendAllText(path: inlistFilename, contents: rows);
        }

        private SessionOptions SessionConfig()
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
    }
}
