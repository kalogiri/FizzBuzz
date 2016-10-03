using LibertyUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FizzBuzz
{
    public class CorrespondingFiles : BaseDownloadScript
    {
        public CorrespondingFiles()
        {
            DebugLogDir = @"C:\PPProject\c# Projects\Test\ppwatch\FindFolders\DebugFolder\";

            IEnumerable<string> dirs = Directory.EnumerateDirectories(@"C:\PPProject\c# Projects\Test\FindFolders\", "*", SearchOption.AllDirectories);
            List<string> foldersToCheck = new List<string>();

            Log.Write("Preparing directories...");

            foreach (string dir in dirs)
            {
                if (!dir.Contains("xx_NEW_xx"))
                {
                    Log.Write("Found: " + dir);
                    GatherFolders(dir);
                }
            }

            DebugUtils.ConsolePause();
        }


        private void GatherFolders(string path)
        {
            Dictionary<string, List<string>> d = new Dictionary<string, List<string>>();
            DirectoryInfo dirConfig = new DirectoryInfo(path);
            FileInfo[] allFiles = dirConfig.GetFiles("*");
            foreach (FileInfo fileInfo in allFiles)
            {
                string coreName = Path.GetFileName(fileInfo.Name);
                if (!d.ContainsKey(coreName)) d.Add(coreName, new List<string>());
                d[coreName].Add(fileInfo.Extension);
            }

            foreach (KeyValuePair<string, List<string>> file in d)
            {
                if (!File.Exists(file.Key))
                {
                    Log.Write("Missing: " + file.Key);
                }
            }
        }

        //private void gatherFolders(string path)
        //{
        //    string SearchDir = path;
        //    string SearchExpression = " *.pdf";
        //    string MustHave = "{pdfFile}.txt";
        //    string name, find;
        //    List<string> missingFile = new List<string>();
        //    List<string> files = new List<string>();
        //    try
        //    {                
        //        foreach (var file in Directory.EnumerateFiles(SearchDir, SearchExpression, SearchOption.TopDirectoryOnly))
        //        {
        //            name = file.Substring(SearchDir.Length + 1);
        //            find = SearchDir + @"\" + MustHave.Replace("{pdfFile}", Path.GetFileNameWithoutExtension(file));
        //            files.Add(file);
        //            if(!File.Exists(find))
        //            {
        //                Log.Write("Missing: " + Path.GetDirectoryName(file) + @"\" + Path.GetFileName(file) + ".txt");
        //                missingFile.Add(Path.GetDirectoryName(file) + @"\" + Path.GetFileName(file) + ".txt");
        //            }
        //            else
        //            {
        //                Log.Write("File: " + find + "'s pair exists.");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Write("[ERR] Exception: " + ex.Message);
        //    }
        //}
    }
}
