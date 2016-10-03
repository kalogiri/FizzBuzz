using LibertyUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FizzBuzz.Tools
{
    class LibertyUtilsLoadingBar : BaseDownloadScript
    {

        string _from, _to = string.Empty;

        public LibertyUtilsLoadingBar()
        {
            DebugLogDir = @"C:\PPProject\c# Projects\Test\FizzBuzzTests\LibertyUtilsLoadingBar\DebugLog\";
            _from = @"C:\PPProject\c# Projects\Test\FizzBuzzTests\LibertyUtilsLoadingBar\From\";
            _to = @"C:\PPProject\c# Projects\Test\FizzBuzzTests\LibertyUtilsLoadingBar\To\";
            try
            {
                string[] files = Directory.GetFiles(_from);
                CopyFiles(files, _to, ShowPercentageProgress, true);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }

        private void CopyFiles(string[] sourceFiles, string destPath, Action<string, long, long> reportProgress, bool create = false, int blockSizeToRead = 4096)
        {
            if (create == false)
            {
                if (!Directory.Exists(destPath))
                {
                    Log.Write(new DirectoryNotFoundException(destPath));
                }
            }
            else
            {
                Directory.CreateDirectory(destPath);
            }

            foreach (string sourceFile in sourceFiles)
            {
                if (!File.Exists(sourceFile))
                {
                    Log.Write(sourceFile + " not found. Skipping it.");
                }

                FileInfo sourceFileInfo = new FileInfo(sourceFile);
                string message = string.Format("Copying {0} ", sourceFileInfo.Name);
                string destFilePath = Path.Combine(destPath, sourceFileInfo.Name);
                byte[] buffer = new byte[blockSizeToRead];
                using (FileStream destfs = File.OpenWrite(destFilePath))
                {
                    using (FileStream sourcefs = File.OpenRead(sourceFile))
                    {
                        int bytesRead, totalBytesRead = 0;
                        while ((bytesRead = sourcefs.Read(buffer, 0, buffer.Length - 1)) > 0)
                        {
                            destfs.Write(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                            reportProgress?.Invoke(message, totalBytesRead, sourceFileInfo.Length);
                        }
                    }
                }
            }
        }

        private void ShowPercentageProgress(string message, long processed, long total)
        {
            long percent = (100 * (processed + 1)) / total;
            Log.Write("\r" + message + percent + " complete");
            if (processed >= total - 1)
            {
                Log.Write(Environment.NewLine);
            }
        }
    }
}
