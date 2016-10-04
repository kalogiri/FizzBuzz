using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FizzBuzz.Tools
{
    internal class CreateFolderStructure
    {
        private readonly string _uploadFolder;
        private readonly string _downloadFolder;

        public CreateFolderStructure(string client, string scriptType, string whichWatch)
        {
            string type = scriptType.Equals("d", StringComparison.CurrentCultureIgnoreCase) ? "download": "upload";
            type = scriptType.Equals("du", StringComparison.CurrentCultureIgnoreCase) ? "download and upload" : type;

            bool isDownload = type.Equals("download");
            bool isUpload = type.Equals("upload");

            if (type == "download and upload")
            {
                isDownload = true;
                isUpload = true;
            }

            if (isDownload)
            {
                Console.WriteLine(@"Creating folder structure for download script");
                // Create download folders
                File.Create($@"{_downloadFolder}DebugLogs\");
                File.Create($@"{_downloadFolder}WorkingFolder\");
            }

            if (isUpload)
            {
                Console.WriteLine(@"Creating folder structure for upload script");
                // Create upload folders
                File.Create($@"{_uploadFolder}DebugLogs\");
                File.Create($@"{_uploadFolder}WorkingFolder\");
            }


        }
    }
}
