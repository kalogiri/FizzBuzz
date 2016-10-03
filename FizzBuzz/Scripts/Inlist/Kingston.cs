using LibertyUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FizzBuzz.Scripts.Inlist
{
    class Kingston
    {
        string _tempPath;
        string _inlistName;
        string _inlistPath;
        public Kingston()
        {
            _tempPath = @"C:\PPProject\c# Projects\Test\ppwatch\Kingston\Test Files\";
            BuildInlist();
        }


        private void BuildInlist()
        {
            List<Tuple<int, string>> f = new List<Tuple<int, string>>();
            
            // Get all the files and check if they have an extension
            foreach(string file in Directory.EnumerateFiles(_tempPath, "*", SearchOption.TopDirectoryOnly))
            {
                if(!Path.HasExtension(file))
                {
                    int lineCount = File.ReadAllLines(file).Count() - 1;
                    f.Add(new Tuple<int, string>(lineCount, file));
                }
            }
            Console.WriteLine("Building Inlist");
            _inlistPath = @"C:\PPProject\c# Projects\Test\ppwatch\Inlist Tests\Kingston\";
            _inlistName = "Kingston-" + DateTime.Now.ToString("dd-MM-yyyy") + ".csv";

            foreach (Tuple<int, string> file in f)
            {
                File.AppendAllText(_inlistPath + _inlistName, CSVDocument.QuoteWrapCommaDelimit(new string[] { "Kingston", DateTime.Now.ToString("dd-MM-yyyy"), DateTime.Now.ToString("HH:mm:ss"), Path.GetFileNameWithoutExtension(file.Item2), file.Item1.ToString()}) + Environment.NewLine);
            }
        }
    }
}
