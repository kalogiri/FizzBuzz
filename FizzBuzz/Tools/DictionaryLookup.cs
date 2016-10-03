using CommonUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FizzBuzz.Tools
{
    class DictionaryLookup
    {
        static string[] _foldernames = { "PDF 1", "PDF 2", "PDF 3", "PDF 4"};

        public DictionaryLookup()
        {
            string value = string.Empty;
            foreach(KeyValuePair<string, List<WarringtonData>> dataFile in BuildDictionary())
            {
                try
                {
                    value = Convert.ToString(dataFile.Value.SingleOrDefault().PageCount);
                    Console.WriteLine(dataFile.Key + " "  + value);
                }
                catch
                {
                    Console.WriteLine("Nothing found for " + dataFile.Key);
                }
            }
        }

        private static Dictionary<string, List<WarringtonData>> BuildDictionary()
        {
            Dictionary<string, List<WarringtonData>> tempDict = new Dictionary<string, List<WarringtonData>>();
            foreach (string foldername in _foldernames)
            {
                List<WarringtonData> tempList = new List<WarringtonData>();

                foreach (FileInfo fileFound in (new DirectoryInfo(@"C:\PPProject\c# Projects\Test\PDF Test\" + foldername).GetFiles("*.pdf")))
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

        public class WarringtonData
        {
            public FileInfo PdfPath;
            public int PageCount;
        }
    }
}
