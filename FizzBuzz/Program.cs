using LibertyUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using FizzBuzz.Scripts;
using FizzBuzz.Tools;
using static System.IO.Directory;

namespace FizzBuzz
{
    class Program
    {
        static void Main()
        {
            try
            {
                //new LibertyUtilsTests();
                //new CsvTools();
                //new ExcelShit();
                //new ExcelShitV2();
                //new AmmendReport();
                //new ExcelShitV3();
                new CheckThundersnow();
                //new CsvTools();
                //new BarnetDownload();
                //new StringInterpolationTests();
                //new CheckThundersnow();
                //new DeployBatch();
            }
            catch (Exception ex)
            {
                //EmailUtils.ErrorReport(ex);
                Console.WriteLine(ex);
            }
            DebugUtils.ConsolePause();
        }

        private static void Test()
        {
            string[] folders = { "One", "Two" };
            List<string> dir = EnumerateFiles(@"C:\PPProject\c# Projects\Test\ppwatch\ICES - NCP\").ToList();
            foreach(string folder in folders)
            {
                List<string> whichList = folder.Equals("One") ? dir.Where(wf => Path.GetExtension(wf) == ".csv").ToList() : dir.Where(wf => Path.GetExtension(wf) == ".pdf").ToList();

                if(folder.Equals("One") && whichList.Count == 0)
                {
                    Console.WriteLine(@"No CSV data files found");
                    continue;
                }
                if(folder.Equals("Two") && whichList.Count == 0)
                {
                    Console.WriteLine(@"No Pdf data files found");
                    continue;
                }

                foreach(string list in whichList)
                {
                    Console.WriteLine(Path.GetFileName(list));
                }
            }
        }
        
        private static void Fizzbuzz()
        {
            for (int i = 1; i <= 100; i++)
            {
                bool shouldFizz = i % 3 == 0;
                bool shouldBuzz = i % 5 == 0;

                if (shouldFizz && shouldBuzz) Console.Write(@"Buzz ");
                else if (shouldBuzz) Console.Write(@"Buzz ");
                else if (shouldFizz) Console.Write(@"Buzz ");
                else Console.Write(i + @" ");
                if (i % 25 == 0) Console.WriteLine();
            }
            Console.Read();
        }
    }
}