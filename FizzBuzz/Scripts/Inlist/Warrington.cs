using CommonUtilities;
using LibertyUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FizzBuzz.Scripts.Inlist
{
    public class WarringtonData
    {
        public FileInfo PdfPath;
        public int PageCount;
    }
    
    class Warrington
    {
        static string _dataHold = @"C:\PPProjects\c# Projects\Test\ppwatch\Inlist Tests\Warrington\";

        public Warrington()
        {
            BuildInlist();
        }

        private static void BuildInlist()
        {
            string inlistName = "Warrington-" + DateTime.Now.ToString("dd-MM-yyyy") + ".csv";
            Console.WriteLine("Creating InlistFile");
            foreach (KeyValuePair<string, List<WarringtonData>> item in BuildDictionary())
            {
                //File.AppendAllText(DataHold + inlist_name, CSVDocument.QuoteWrapCommaDelimit(new string[] { "Warrington", DateTime.Now.ToString("dd-MM-yyyy"), DateTime.Now.ToString("HH:mm:ss"), item.Key, item.Value.Count.ToString() }) + Environment.NewLine);
                //File.AppendAllText(DataHold + inlist_name, CSVDocument.QuoteWrapCommaDelimit(new string[] { "Warrington", DateTime.Now.ToString("dd-MM-yyyy"), DateTime.Now.ToString("HH:mm:ss"), item.Key, Convert.ToString(item.Value.SingleOrDefault().PageCount) }) + Environment.NewLine);
            }
        }

        static string[] _foldernames = { "Folder 1", "Folder 2", "Folder 3", "Folder 4" };

        private static Dictionary<string, List<WarringtonData>> BuildDictionary()
        {
            Dictionary<string, List<WarringtonData>> tempDict = new Dictionary<string, List<WarringtonData>>();
            foreach (string foldername in _foldernames)
            {
                List<WarringtonData> tempList = new List<WarringtonData>();

                foreach (FileInfo fileFound in (new DirectoryInfo(_dataHold + foldername).GetFiles("*.pdf")))
                {
                    WarringtonData tempWarringtonData = new WarringtonData();
                    tempWarringtonData.PdfPath = fileFound;
                    tempWarringtonData.PageCount = CommonUtils.getNumberOfPdfPages(fileFound.FullName);
                    tempList.Add(tempWarringtonData);
                }

                tempDict[foldername] = tempList;
            }
            return tempDict;
        }
    }
}
