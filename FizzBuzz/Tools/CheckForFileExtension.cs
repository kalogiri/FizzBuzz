using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FizzBuzz.Tools
{
    class CheckForFileExtension
    {
        public CheckForFileExtension()
        {
            string[] filesToCheck = Directory.GetFiles(@"C:\PPProject\c# Projects\Test\ppwatch\Kingston\Test Files\", "*");
            List<string> files = Directory.EnumerateFiles(@"C:\PPProject\c# Projects\Test\ppwatch\ICES - NCP\").ToList();
            Console.WriteLine("First dealing with pdf files");
            List<string> pdfFiles = files.Where(wf => Path.GetExtension(wf) == ".pdf").ToList();
            foreach(string file in pdfFiles)
            {
                Console.WriteLine(Path.GetFileName(file));
            }

            List<string> csvFiles = files.Where(wf => Path.GetExtension(wf) == ".csv").ToList();
            Console.WriteLine("Now dealing with csv files");
            foreach(string file in csvFiles)
            {
                Console.WriteLine(Path.GetFileName(file));
            }
        }
    }
}
