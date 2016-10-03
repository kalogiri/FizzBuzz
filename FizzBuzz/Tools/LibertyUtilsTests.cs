using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibertyUtils;

namespace FizzBuzz.Tools
{
    class LibertyUtilsTests : BaseDownloadScript
    {
        private string _from;
       
        public LibertyUtilsTests()
        {
            DebugLogDir = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\LibertyUtilsTests\DebugLogDir\";
            WorkingDir = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\LibertyUtilsTests\WorkingFolder\";
            LiveDataDir = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\LibertyUtilsTests\LiveDataDir\";
            _from = @"C:\PPProjects\c# Projects\Test\FizzBuzzTests\LibertyUtilsTests\CollectFilesFrom\";

            //GatherLocalFiles(true);
            MoveLoggedTest();
        }


        private void MoveLoggedTest()
        {
            string[] files = Directory.EnumerateFiles(_from, "*").ToArray();

            foreach (string file in files)
            {
                FileUtils.MoveLogged(file, LiveDataDir + Path.GetFileName(file));
            }
        }
        private void GatherLocalFiles(bool clearDestination = false)
        {
            List<WorkingFile> workingFiles = GatherLocalData<WorkingFile>(
                from: _from,
                to: WorkingDir,
                searchPattern: "*.txt",
                searchOption: System.IO.SearchOption.TopDirectoryOnly,
                clearDestination: clearDestination                
            );
            //foreach(var file in workingFiles)
            //{
            //    Log.Write("Moving files: " + file.WorkingFileName);
            //    FileUtils.MoveLogged(file.WorkingPath, LiveDataDir + file.WorkingFileName, true, false);
            //}
        }
    }
}
