using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibertyUtils;
using System.IO;

namespace FizzBuzz.Scripts
{
    
    class WalthamForest : BaseDownloadScript
    {
        public string SequenceFilePath;
        
        public WalthamForest()
        {
            DebugLogDir = @"C:\PPProject\c# Projects\Test\ppwatch\Waltham Forest\DebugLog\";
            WorkingDir = @"C:\PPProject\c# Projects\Test\ppwatch\Waltham Forest\WorkingDir\";
            PPImagesDir = @"C:\PPProject\c# Projects\Test\ppwatch\Waltham Forest\Images\"; // PDF Folder
            LiveDataDir = @"C:\PPProject\c# Projects\Test\ppwatch\Waltham Forest\LiveDataDir\"; // CSV Folder
            CredentialsPath = HostPath.ppwatch_2 + LibertyConfig.APP_DATA_NAME + @"\ExternalCredentials.xml";
            LocalSftpDir = HostPath.sftp + @"Cobalt Waltham Forest\TEST\";
            SequenceFilePath = @"C:\PPProject\c# Projects\Test\ppwatch\Waltham Forest\SeqNumber.txt";

            Log.Write("Processing Download");
            ProcessDownload();
        }

        private void ProcessDownload()
        {
            string sequenceNumber = NextSequenceNumber(SequenceFilePath);

            // Gather the pdfs
            List<WorkingFile> workingFiles = GatherLocalData<WorkingFile>(
                from: LocalSftpDir,
                to: WorkingDir,
                searchPattern: "*.txt",
                searchOption: SearchOption.TopDirectoryOnly,
                clearDestination: true
            );

            if (workingFiles.Count > 0)
            {
                CSVDocument csvDoc = new CSVDocument() { Delimiter = "\n", QuotedValues = true };
                csvDoc.AddRow("PDF Name");

                foreach (WorkingFile file in workingFiles)
                {
                    csvDoc.AddRow(Path.GetFileName(file.WorkingPath));
                    FileUtils.MoveLogged(file.WorkingPath, PPImagesDir + Path.GetFileName(file.WorkingPath), overwrite: true);

                    FileUtils.DeleteLogged(file.SourcePath);
                }

                csvDoc.SaveAs(LiveDataDir + @"Waltham_Forest_Permit_" + DateTime.Today.ToString("yyyyMMdd") + "_" + sequenceNumber + ".csv");
                csvDoc.UnloadFile();
            }
            else
            {
                Log.Write("No permits found.");
            }
        }
    }
}
