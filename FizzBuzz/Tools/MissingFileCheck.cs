using LibertyUtils;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FizzBuzz.Tools
{
    class MissingFileCheck : BaseDownloadScript
    {
        class FileClass
        {
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public bool Pdf { get; set; }
            public bool Txt { get; set; }
        }

        public MissingFileCheck()
        {
            DebugLogDir = @"C:\PPProject\c# Projects\Test\ppwatch\FindFolders\DebugFolder\";
            foreach (string dir in Directory.EnumerateDirectories(@"C:\PPProject\c# Projects\Test\FindFolders\", "*", SearchOption.AllDirectories))
            {
                // Check all dir except for "xx_NEW_xx"
                if (!dir.Contains("xx_NEW_xx"))
                {
                    Thingy(dir);
                }
            }
        }

        private void Thingy(string dir)
        {
            List<FileClass> list = new List<FileClass>();

            DirectoryInfo dirConfig = new DirectoryInfo(dir);
            FileInfo[] allFiles = dirConfig.GetFiles("*");

            foreach (FileInfo fileInfo in allFiles)
            {
                string fileName = fileInfo.Name.Substring(0, fileInfo.Name.Length - fileInfo.Extension.Length);
                FileClass fileClass = list.Where(fc => fc.FileName == fileName).FirstOrDefault();
                if (fileClass == null)
                {
                    fileClass = new FileClass
                    {
                        FileName = fileName,
                        FilePath = Path.Combine(fileInfo.DirectoryName, fileName),
                        Pdf = (fileInfo.Extension.ToLower() == ".pdf"),
                        Txt = (fileInfo.Extension.ToLower() == ".txt")
                    };

                    list.Add(fileClass);
                }
                else
                {
                    switch (fileInfo.Extension.ToLower())
                    {
                        case ".pdf": fileClass.Pdf = true; break;
                        case ".txt": fileClass.Txt = true; break;
                    }
                }
            }

            foreach (FileClass file in list)
            {
                if (file.Txt == false)
                {
                    Log.Write("Missing: " + file.FilePath + ".txt");
                }
                if (file.Pdf == false)
                {
                    Log.Write("Missing: " + file.FilePath + ".pdf");
                }
            }
        }
    }
}
