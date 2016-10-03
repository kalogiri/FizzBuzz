using System;
using System.Collections.Generic;
using System.IO;
using LibertyUtils;

namespace FizzBuzz.Scripts
{
    class BarnetDownload : BaseDownloadScript
    {
        private readonly LibertyConfigExternalCredentials.Credential _ftpCredential;
        private readonly string _serverLocation;
        private readonly string _serverArchiveLocation;

        public BarnetDownload()
        {
            DebugLogDir = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\Barnet\DebugLog\";
            WorkingDir = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\Barnet\WorkingDir\";
            PPImagesDir = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\Barnet\ImagesDir\";
            LiveDataDir = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\Barnet\LiveDataDir\";
            LocalSftpDir = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\Barnet\CollectionDir\";
            CredentialsPath = HostPath.ppwatch_1 + @"LibertyConfig\ExternalCredentials.xml";

            _ftpCredential = Credentials.Get("Parking - Barnet");
            _serverLocation = "/home/BarnetLiberty/TEST/";
            _serverArchiveLocation = $"{_serverLocation}{DateTime.Today:dd-MM-yyyy}/";

            DirUtils.RecreateLogged(WorkingDir);

            Log.Write("Starting barnet download script");
            string[] patterns = { "*.BNT", "*.zip" };
            ProcessDownload(patterns);
            Log.Write(this.GetType() + @" finished!");
        }

        private void ProcessDownload(string[] patternsInUse)
        {
            foreach (string pattern in patternsInUse)
            {
                // Gather the files
                List<WorkingFile> workingFiles = GatherFTPData<WorkingFile>(
                    credential: _ftpCredential,
                    from: _serverLocation,
                    to: WorkingDir,
                    searchPattern: "*.zip",
                    searchOption: SearchOption.TopDirectoryOnly
                );

                if (workingFiles.Count == 0)
                {
                    Log.Write("No data");
                    return;
                }

                List<string> ftpCommand = new List<string>();

                // Extract the data
                foreach (WorkingFile wf in workingFiles)
                {
                    ftpCommand.Add($"mv {wf.SourcePath} {_serverArchiveLocation}");
                    Log.Write("Extracting data from zip: " + wf.WorkingFileName);
                    ZipUtils.DeCompressLogged(wf.WorkingPath, WorkingDir);

                    string[] images = pattern.Contains("BNT") ? null : Directory.GetFiles(WorkingDir + wf.WorkingFileNameWithoutExtension + @"\", "*.jpg");
                    string[] dataFiles = pattern.Contains("BNT") ? Directory.GetFiles(wf.WorkingFileName) : Directory.GetFiles(WorkingDir + wf.WorkingFileNameWithoutExtension + @"\", "*.BNT");

                    foreach (string image in images)
                    {
                        FileUtils.MoveLogged(image, PPImagesDir + Path.GetFileName(image));
                    }

                    foreach (string dataFile in dataFiles)
                    {
                        FileUtils.MoveLogged(dataFile, LiveDataDir + Path.GetFileName(dataFile));
                    }
                }

                // Check if archive dir exists.
                if (!FTPUtils.RemoteFolderExists(_ftpCredential, _serverArchiveLocation))
                {
                    FTPUtils.Execute(_ftpCredential, $" mkdir {_serverArchiveLocation}"); // Create archive location
                }

                FTPUtils.Execute(_ftpCredential, string.Join("\r\n", ftpCommand)); // Move the files to the archive location
            }
        }
    }
}
