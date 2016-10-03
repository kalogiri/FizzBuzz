using System;
using System.Collections.Generic;
using System.Linq;
using LibertyUtils;
using System.IO;

namespace FizzBuzz.Scripts.Inlist
{
    class Warwickshire : BaseDownloadScript
    {
        string _awaitingDownloadsDir;
        public Warwickshire()
        {
            DebugLogDir = @"C:\PPProject\c# Projects\Test\ppwatch\Inlist Tests\Warwickshire\";
            WorkingDir = @"C:\PPProject\c# Projects\Test\ppwatch\Inlist Tests\Warwickshire\WorkingDir\";
            _awaitingDownloadsDir = @"C:\PPProject\c# Projects\Test\ppwatch\Inlist Tests\Warwickshire\AwaitingUpload\";
            string[] patterns = { "*.csv", "*.WRK" };
            foreach(string pattern in patterns)
            {
                List<WorkingFile> workingFiles = GatherLocalData<WorkingFile>(
                    from: _awaitingDownloadsDir,
                    to: WorkingDir,
                    searchPattern: pattern,
                    searchOption: SearchOption.TopDirectoryOnly,
                    clearDestination: true
                );

                if(workingFiles.Count == 0)
                {
                    Log.Write("No Data");
                }
                else
                {
                    Log.Write("Building InList");
                    foreach(WorkingFile wf in workingFiles)
                    {
                        try
                        {
                            BuildInList(wf);
                        }
                        catch (Exception ex)
                        {
                            Log.Write(ex);
                            continue;
                        }
                        Log.Write("Finished Building inlist file for ");                   
                    }
                }
            }
        }

        private void BuildInList(WorkingFile wf)
        {
            int lineCount = File.ReadAllLines(wf.WorkingPath).Count() - 1;
            string inListFilename = "Warwickshire-" + DateTime.Now.ToString("dd-MM-yyyy") + ".csv";
            string inListFileLocation = @"C:\PPProject\c# Projects\Test\ppwatch\Inlist Tests\Warwickshire\InList\";
            Console.WriteLine("Inlist Line: " + CSVDocument.QuoteWrapCommaDelimit(new string[] { "Warwickshire", DateTime.Now.ToString("dd-MM-yyyy"), DateTime.Now.ToString("HH:mm:ss"), Path.GetFileName(wf.WorkingPath), lineCount.ToString() }) + Environment.NewLine);
            File.AppendAllText(inListFileLocation + inListFilename, CSVDocument.QuoteWrapCommaDelimit(new string[] { "Warwickshire", DateTime.Now.ToString("dd-MM-yyyy"), DateTime.Now.ToString("HH:mm:ss"), Path.GetFileName(wf.WorkingPath), lineCount.ToString() }) + Environment.NewLine);
        }
    }
}

