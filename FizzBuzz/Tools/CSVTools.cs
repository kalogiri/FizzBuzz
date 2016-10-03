using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using LibertyUtils;

namespace FizzBuzz.Tools
{
    class CsvTools
    {
        private const string RootPath = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\CSVUtlsTest\Files\";
        private const string CsvFile = @"C:\PPProjects\c# Projects\Test\EPPlus Test\CSV Location\DailyReport.csv";
        private const string ExcelFile = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\CSVUtlsTest\EXCELDOC.xlsx";

        public CsvTools()
        {
            //AddNewRowAndData();
            CheckIfCsvIsOpen();

            DeleteLinesFromCsv();
        }

        private static void AddShit()
        {
            DirectoryInfo fi = new DirectoryInfo(RootPath);

            foreach (FileInfo file in fi.GetFiles("*.txt", SearchOption.TopDirectoryOnly))
            {
                File.AppendAllText(CsvFile, file.Name + Environment.NewLine);
            }
        }

        private static void AddNewRowAndData()
        {
            string[] allDataFiles = Directory.EnumerateFiles(RootPath, "*.txt").ToArray();
            Console.WriteLine(allDataFiles.Length);
            foreach (string allDataFile in allDataFiles)
            {
                if (allDataFile.Contains("BPC Liberty"))
                {
                    string[] lines = File.ReadAllLines(allDataFile);

                    List<List<string>> csv = lines.Select(x => x.Split('|').ToList()).ToList();

                    for (int i = 0; i < csv.Count; i++)
                    {
                        csv[i].Insert(0, i == 0 ? "\"LetterCode\"" : "\"BRNPCN\"");
                    }

                    File.WriteAllLines(RootPath + System.IO.Path.GetFileName(allDataFile),
                        csv.Select(x => string.Join("|", x)));
                }
            }
        }

        private static void CheckIfCsvIsOpen()
        {
            while (IsFileLocked(ExcelFile))
            {
                Console.WriteLine("Please close the file before proceeding");
                Console.ReadKey();
            }
            Console.WriteLine("Its closed");
            //CSVDocument csvDoc = new CSVDocument(CsvFile) {Delimiter = ","};
            //csvDoc.LoadFile();
            //DirectoryInfo fi = new DirectoryInfo(rootPath);

            //foreach (FileInfo file in fi.GetFiles("*.txt", SearchOption.TopDirectoryOnly))
            //{
            //    csvDoc.AddRow(file.Name);
            //    csvDoc.SaveAs(CsvFile);
            //}
            //csvDoc.UnloadFile();
        }

        private static bool IsFileLocked(string filename)
        {
            FileInfo file = new FileInfo(filename);
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                stream?.Close();
            }
            return false;
        }

        private static void DeleteLinesFromCsv()
        {
            //List<string> filenamesToDeleteFromCsv = Directory.GetFiles(RootPath, "*.txt").ToList();

            List<string> filenamesToDeleteFromCsv = new List<string>();

            filenamesToDeleteFromCsv.Add("DFFC_IA_160919_MTCPCN_3593.txt");
            filenamesToDeleteFromCsv.Add("WARN_DFFC_IA_160919_MTCPCN_3594.txt");
            filenamesToDeleteFromCsv.Add("DFFC_IA_160919_MTCPCN_3593.txt");
            filenamesToDeleteFromCsv.Add("DFFC_IA_160919_MTCPCN_3591.txt");
            filenamesToDeleteFromCsv.Add("WARN_DFFC_IA_160919_MTCPCN_3592.txt");
            filenamesToDeleteFromCsv.Add("DFFC_IA_160919_MTCNODR_3595.txt");

            Console.WriteLine(@"Removing unwanted lines from the csv");

            List<string> lines = new List<string>();

            using (StreamReader reader = new StreamReader(CsvFile))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            List<string> distinctFilenames = filenamesToDeleteFromCsv.Distinct().ToList();

            foreach (string distinctFilename in distinctFilenames)
            {
                lines.RemoveAll(
                    l => (distinctFilename != null) && l.Contains(Path.GetFileNameWithoutExtension(distinctFilename)));
                using (StreamWriter outFile = new StreamWriter(CsvFile))
                {
                    outFile.Write(string.Join("\r\n", lines.ToArray()));
                }
            }
        }

    }
}
