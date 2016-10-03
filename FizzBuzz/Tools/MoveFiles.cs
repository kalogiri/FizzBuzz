using LibertyUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FizzBuzz.Tools
{
    class MoveFiles : BaseDownloadScript
    {
        public MoveFiles()
        {
            DebugLogDir = @"C:\PPProject\c# Projects\Test\FizzBuzzTests\LibertyUtilsLoadingBar\DebugLog\";
            SimpleMove();
            //Move();
        }

        private void SimpleMove()
        {
            FileUtils.MoveLogged(@"C:\PPProject\c# Projects\Test\FizzBuzzTests\FTPUtilsUpload\Test File.txt", @"C:\PPProject\c# Projects\Test\FizzBuzzTests\FTPUtilsUpload\Archive\Test File.txt");
        }
        private void Move()
        {
            string path = @"C:\RG Scripts\Move Files\";

            //string[] p = Directory.GetFiles(path, "*REGENERATION.csv");
            IEnumerable<FileInfo> p = new DirectoryInfo(path).GetFiles("*REGENERATION.csv").Where(f => !Regex.IsMatch(f.Name, "CC|NODR"));
            if (p.Count() > 0)
            {
                foreach (FileInfo file in p)
                {
                    Console.WriteLine("Moving file: " + file.Name + " to: " + path + @"Files\" + file.Name);
                    File.Move(file.FullName, path + @"Files\" + file.Name);
                }
            }
            else
            {
                Console.WriteLine("No regeneration files found in " + path);
            }
        }
    }
}
