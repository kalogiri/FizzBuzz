using LibertyUtils;
using System;
using System.IO;

namespace FizzBuzz.Scripts
{
    class LambethDataIntegrityCheck
    {
        public LambethDataIntegrityCheck()
        {
            Check();
        }

        private void Check()
        {
            string folderToCheck = @"C:\RG Scripts\FizzBuzz\TestFolder\Lambeth\Data Files\";
            string quarantinedFolder = @"C:\RG Scripts\FizzBuzz\TestFolder\Lambeth\Quarantine Folder\" + DateTime.Now.ToString("yyyyMMdd") + @"\";

            foreach (FileInfo file in new DirectoryInfo(folderToCheck).GetFiles())
            {
                using (CSVDocument csvDoc = new CSVDocument(file.FullName) { Delimiter = "\t" })
                {
                    csvDoc.LoadFile();
                    int recordCount = csvDoc.RowCount - 1;
                    csvDoc.UnloadFile();

                    if (recordCount < 1)
                    {
                        Directory.CreateDirectory(quarantinedFolder);
                        Console.WriteLine("Empty data file " + file.Name + ", moving it to quarantine folder.");
                        file.MoveTo(quarantinedFolder + file.Name);
                        Console.WriteLine("Moved: " + file.Name + " to " + quarantinedFolder);
                    }
                    else
                    {
                        Console.WriteLine("File: " + file.Name + " is good!");
                    }
                }
            }
        }
    }
}
