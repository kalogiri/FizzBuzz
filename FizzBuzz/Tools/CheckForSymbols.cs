using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FizzBuzz.Tools
{
    class CheckForSymbols
    {
        public CheckForSymbols()
        {
            Console.WriteLine("Starting Directory Check");
            Console.WriteLine((int)'–');
            Console.WriteLine((int)'-');
            Console.WriteLine((int)'–');
            string newFolder = @"C:\PPProjects\c# Projects\Test\ppwatch\ICES - NCP\Replacement\";
            foreach (string file in Directory.EnumerateFiles(@"C:\PPProjects\c# Projects\Test\ppwatch\ICES - NCP\", "*.pdf"))
            {
                string newFileName = Path.GetFileName(file.Replace(@"–", @"-"));
                File.Move(file, @"C:\PPProjects\c# Projects\Test\ppwatch\ICES - NCP\" + newFileName);
            }
            Console.ReadLine();  
        }
    }
}
