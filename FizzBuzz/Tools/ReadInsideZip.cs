using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using LibertyUtils;
namespace FizzBuzz.Tools
{
    class ReadInsideZip
    {
        public ReadInsideZip()
        {
            ReadZip();
        }

        private void ReadZip()
        {
            string temploc = @"C:\PPProject\c# Projects\Test\EPPlus Test\TEMP\";
            foreach (string zipFile in Directory.EnumerateFiles(@"C:\PPProject\c# Projects\Test\EPPlus Test\", "*.zip"))
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipFile))
                {
                    foreach(ZipArchiveEntry entry  in archive.Entries)
                    {
                        string extractPath = temploc + Path.GetFileNameWithoutExtension(zipFile);

                        if (entry.FullName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        {
                            string extractTo = Path.Combine(extractPath, entry.FullName);

                            if (File.Exists(extractTo))
                            {
                                Console.WriteLine(Path.GetFileName(extractTo) + " already exists, skipping." + Environment.NewLine);
                                continue;
                            }

                            Directory.CreateDirectory(extractPath);

                            entry.ExtractToFile(extractTo);

                            string dataFile = Path.GetFileNameWithoutExtension(entry.FullName);
                            string[] dataFileSplits = dataFile.Split('_');

                            string zipFileName = Path.GetFileNameWithoutExtension(zipFile);
                            string[] zipFileSplits = zipFileName.Split('_');

                            string batchId = dataFileSplits[dataFileSplits.Length - 1];
                            string batchItems = File.ReadLines(extractTo).Last();

                            string batchNumber = zipFileSplits[zipFileSplits.Length - 1];

                            string batchFilterName = zipFileSplits[0];

                            Console.WriteLine("DataFile: " + Path.GetFileName(entry.FullName) + " | " + "Zip File: " + Path.GetFileName(zipFile));
                            Console.WriteLine("-------------------------------------------------------");
                            Console.WriteLine("Date Recieved => " + DateTime.Today.ToString("dd-MMM"));
                            Console.WriteLine("bt_id => " + batchId);
                            Console.WriteLine("bt_batch_no => " + batchNumber);
                            Console.WriteLine("bt_filter_name => " + batchFilterName);
                            Console.WriteLine("bt_items_in => " + batchItems);
                            Console.WriteLine("-------------------------------------------------------");
                            Console.WriteLine("Finished processing " + Path.GetFileName(zipFile) + Environment.NewLine);
                        }
                    }
                }
            }
        }
    }
}
