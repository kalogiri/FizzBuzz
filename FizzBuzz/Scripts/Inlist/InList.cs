using LibertyUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using WinSCP;
using static LibertyUtils.LibertyConfigExternalCredentials;

namespace FizzBuzz.Scripts.Inlist
{
    internal class InList : BaseDownloadScript
    {
        readonly string[] _roots = {
                @"/Stat Doc/DTE OFR/a/",
                @"/Stat Doc/DTE OFR/b/",
                @"/Stat Doc/CCTV CEO OFR/",
                @"/Stat Doc/BLE OFR/",
                @"/Correspondence/1st Class/",
                @"/Correspondence/2nd Class/"
            };

        readonly string _time = DateTime.Now.ToString("H:mm:ss");
        public InList()
        {
            DebugLogDir = @"C:\PPProject\c# Projects\Test\ppwatch\Ealing\InList\";
            WorkingDir = @"C:\PPProject\c# Projects\Test\uploads\ppwatch\Ealing\DataIn\";

            CredentialsPath = HostPath.ppwatch_1 + @"LibertyConfig\ExternalCredentials.xml";
            Credentials.Get("Parking - Ealing");

            //Log.Default.Write("Gathering Local Data File Information...");
            //CSVProcess(root);

            //Log.Default.Write("Gathering Server PDF Information...");
            //PDFProcess(ftpCredentials, roots);

            Log.Default.Write("Creating CSV files for email");
            Csvforemail();
            Log.Default.Write("Finished Sending Email!");
        }

/*
        private void CsvProcess(string root)
        {
            // Gather all the csv files in the downloaded folder
            string[] fileListcsv = System.IO.Directory.GetFiles(root, "*.csv");

            List<string> datafiles = fileListcsv.ToList();

            // Add the data files to a cumulative list

            foreach (string data in datafiles)
            {
                int linecount = System.IO.File.ReadAllLines(data).Length - 1;
                _statdocsContents += "DataFile: " + System.IO.Path.GetFileName(data) + " | Total Lines: " + linecount + Environment.NewLine;
            }
        }
*/

        protected void PdfProcess(Credential cred, IEnumerable<string> root)
        {
            // get file info from the ftp
            SessionOptions sessionOptions = EstablishConnection(cred);
            Session session = new Session();

            try
            {
                using (session)
                {
                    session.Open(sessionOptions);
                    if (!session.Opened)
                    {
                        return;
                    }
                    StatdocsContents += Environment.NewLine + "Stat Docs [Server]" + Environment.NewLine +
                                         "------------------" + Environment.NewLine;
                    foreach (string dir in root)
                    {
                        RemoteDirectoryInfo directory = session.ListDirectory(dir);
                        int count = 0;

                        foreach (RemoteFileInfo info in directory.Files)
                        {
                            if (!info.IsDirectory && (info.Name.Contains("PDF") || info.Name.Contains("pdf")))
                            {
                                count++;
                            }
                        }

                        bool corres = dir.IndexOf("Correspondence", StringComparison.Ordinal) != -1;

                        if (corres)
                        {
                            CorresContents += "Directory: " + dir + " | Total PDFs : " + count +
                                              Environment.NewLine;
                        }
                        else
                        {
                            StatdocsContents += "Directory: " + dir + " | Total PDFs : " + count +
                                                Environment.NewLine;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed when talking with the server: " + ex);
            }
        }

        protected List<Tuple<string, int>> Data
        {
            get
            {
                string[] csvfiles = System.IO.Directory.GetFiles(WorkingDir, "*.csv");
                List<Tuple<string, int>> info = (from csv in csvfiles let linecount = System.IO.File.ReadAllLines(csv).Length - 1 select new Tuple<string, int>(System.IO.Path.GetFileNameWithoutExtension(csv), linecount)).ToList();

                if (!csvfiles.Any())
                {
                    info.Add(new Tuple<string, int>("NO DATA FOUND", 0)); // Data filename, Line count
                }
                return info;
            }
        }

        protected List<Tuple<string, int>> Pdf
        {
            get
            {
                List<Tuple<string, int>> info = new List<Tuple<string, int>>();
                Credential ftpCredentials = Credentials.Get("Parking - Ealing");
                SessionOptions sessionOptions = EstablishConnection(ftpCredentials);
                Session session = new Session();
                using (session)
                {
                    session.Open(sessionOptions);
                    if (!session.Opened)
                    {
                        return info;
                    }
                    foreach (string dir in _roots)
                    {
                        RemoteDirectoryInfo directory = session.ListDirectory(dir);
                        int count = 0;
                        foreach (RemoteFileInfo rinfo in directory.Files)
                        {
                            if (!rinfo.IsDirectory && (rinfo.Name.Contains("PDF") || rinfo.Name.Contains("pdf"))) { count++; }
                        }
                        info.Add(new Tuple<string, int>(dir, count)); // Pdf Directory, Count of pdfs in that directory
                    }
                }
                return info;
            }
        }

        public string StatdocsContents { get; set; } = Environment.NewLine + "Data Files [Downloaded]" + Environment.NewLine + "-----------------------" + Environment.NewLine;

        public string CorresContents { get; set; } = Environment.NewLine + "Correspondence [Server]" + Environment.NewLine + "-----------------------" + Environment.NewLine;

        protected void Csvforemail()
        {

            string pdfCsv = @"C:\PPProject\c# Projects\Test\uploads\ppwatch\Ealing\Pdf_" + DateTime.Today.ToString("yyyMMdd") + ".csv";
            Log.Write("Calculating pdf count and adding it to: Pdf_" + DateTime.Today.ToString("yyyMMdd") + ".csv");
            Pdfcsv(pdfCsv);
            string dataCsv = @"C:\PPProject\c# Projects\Test\uploads\ppwatch\Ealing\Data_" + DateTime.Today.ToString("yyyMMdd") + ".csv";
            Log.Write("Calculating line counts in the data files and adding it to: Data_" + DateTime.Today.ToString("yyyMMdd") + ".csv");
            Datacsv(dataCsv);
        }

        protected void Pdfcsv(string file)
        {
            //string file = @"C:\PPProject\c# Projects\Test\uploads\ppwatch\Ealing\PDF" + DateTime.Today.ToString("yyyMMdd") + ".csv";
            if (!System.IO.File.Exists(file))
            {
                Log.Write(Environment.NewLine + "CSV file not found for email. - PDF");
                Log.Write("Creating csv for emails - PDF");
                string header = "Location" + "," + "Total PDFs" + "\r\n";
                System.IO.File.WriteAllText(file, header);
            }
            string rows = string.Empty;
            rows += _time + "\r\n";
            foreach (Tuple<string, int> item in Pdf)
            {
                rows += item.Item1 + "," + item.Item2.ToString() + "\r\n";
            }
            System.IO.File.AppendAllText(file, rows);
        }

        protected void Datacsv(string file)
        {
            //string file = @"C:\PPProject\c# Projects\Test\uploads\ppwatch\Ealing\Data" + DateTime.Today.ToString("yyyMMdd") + ".csv";
            if (!System.IO.File.Exists(file))
            {
                Log.Write(Environment.NewLine + "CSV file not found for email. - Data File");
                Log.Write("Creating csv for emails - Data File");
                string header = "Data File" + "," + "Total Lines" + "\r\n";
                System.IO.File.WriteAllText(file, header);
            }
            string rows = string.Empty;
            rows += _time + "\r\n";
            foreach (Tuple<string, int> line in Data)
            {
                rows += line.Item1 + "," + line.Item2 + "\r\n";
            }
            System.IO.File.AppendAllText(file, rows);
        }

/*
        private void SendEmail()
        {
            string emailString = _statdocsContents + _corresContents;

            // Parse the csv files create for the email process at different times
            string[] filesToCollect = System.IO.Directory.GetFiles(@"C:\PPProject\c# Projects\Test\uploads\ppwatch\Ealing\", "*.csv");

            string emailStringBuilder = String.Empty;
            // read each of the files
            foreach (string file in filesToCollect)
            {
                System.IO.StreamReader reader = new System.IO.StreamReader(System.IO.File.OpenRead(file));
            }

            #region The Actual Email

            EmailUtils.Send(
                new[] {"rachit.giri@libertyservices.co.uk"},
                "rachit.giri@libertyservices.co.uk",
                fromName: "Liberty Services",
                subject: "Ealing: Script Report",
                body:
                @"The list of data files that were initally downloaded to be processed and the total number of PDFs that are to be processed are listed below.
                    " + emailString + @"           
Kind regards,

Liberty Services Team
Liberty Services
3 Stafford Cross
Stafford Road
Croydon
CR0 4TU

Tel: +44 0208 681 4110 < Option 2 >
Email: data@libertyservices.co.uk
                    "
            );
            #endregion

        }
*/

/*
        private void AddString(ref string holder, string input) => holder += input;
*/

        protected static SessionOptions EstablishConnection(Credential credentials)
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
                GiveUpSecurityAndAcceptAnySshHostKey = true // This needs changing to have it go through a security protocol
            };
            return sessionOptions;
        }
    }
}
