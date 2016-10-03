using System.Xml.Linq;
using LibertyUtils;
using System;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace FizzBuzz.Tools
{
    public class CreatingACustomXml : BaseDownloadScript
    {
        public CreatingACustomXml()
        {
            DebugLogDir = @"C:\PPProject\c# Projects\Test\CreateCustomXML\";
            AddToXml(@"C:\PPProject\c# Projects\Missing File Checker\CheckFileConfig.xml", "Test Client 2", "Test Root 2", "Test Server Location 2", "xx_TEST_xx 2", ".pdf, .txt 2");
        }

        private void AddToXml(string xmlFilePath, string clientName, string rootFolderName, string serverLocation, string foldersToSkip, string lookUpPatterns)
        {
            try
            {
                XDocument xDoc = XDocument.Load(xmlFilePath);
                XElement root = xDoc.Element("Clients");
                IEnumerable<XElement> rows = root.Descendants("Client");
                XElement firstRow = rows.First();
                firstRow.AddBeforeSelf(
                    new XElement("Client",
                        new XElement("ClientName", clientName),
                        new XElement("ClientDetails", 
                            new XElement("RootFolder", rootFolderName),
                            new XElement("ServerLocation", serverLocation),
                            new XElement("FullPath", serverLocation + @"\" + clientName + @"\"),
                            new XElement("FolderToSkip", foldersToSkip),
                            new XElement("Patterns", lookUpPatterns)
                        )
                    )
                );
                xDoc.Save(xmlFilePath);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
