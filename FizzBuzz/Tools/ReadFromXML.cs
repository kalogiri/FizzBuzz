using System;
using System.Xml;
using LibertyUtils;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;

namespace FizzBuzz.Tools
{
    class ReadFromXml : BaseDownloadScript
    {
        string _clientName = string.Empty;
        string _rootFolder = string.Empty;
        string _serverLocation = string.Empty;
        string _fullPath = string.Empty;
        string _folderToSkip = string.Empty;
        string _patterns = string.Empty;
        string _xmlItems = string.Empty;
        string _input = string.Empty;

        public ReadFromXml()
        {
            DebugLogDir = @"C:\PPProject\c# Projects\Test\CreateCustomXML\";
            //ReadXML(@"C:\PPProject\c# Projects\Missing File Checker\CheckFileConfig.xml", "Test Client");
            _input = "Test Client 50";

            ProcessDetails(ReadSpecificElement(@"C:\PPProject\c# Projects\Missing File Checker\CheckFileConfig.xml", _input));
        }

        private void ReadXml(string xmlFilename, string client)
        {

            try
            {
                XElement xElem = XElement.Load(xmlFilename);
                var results = xElem.Descendants("Client")
                    .Descendants("ClientName")
                    .Where(e => e.Value == client)
                    .Select(e => e.Parent)
                    .Descendants("ClientDetails")
                    .Select(e => new
                {
                    RootFolder = e.Descendants("RootFolder").FirstOrDefault().Value,
                    ServerLocation = e.Descendants("ServerLocation").FirstOrDefault().Value,
                    FullPath = e.Descendants("FullPath").FirstOrDefault().Value,
                    FolderToSkip = e.Descendants("FolderToSkip").FirstOrDefault().Value,
                    Patterns = e.Descendants("Patterns").FirstOrDefault().Value
                });


                foreach(var result in results)
                {
                    Console.WriteLine(
                        result.RootFolder + "{0}" + 
                        result.ServerLocation + "{0}" +
                        result.FullPath + "{0}" +
                        result.FolderToSkip + "{0}" +
                        result.Patterns, 
                        Environment.NewLine
                    );
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }

        private void ProcessDetails(int item)
        {
            if(item > 0)
            {
                Log.Write("Client already exists. Please provide a unique name if a new config for that client is required or use the existing one");
            }
            else
            {
                Log.Write("Adding " + _input);
            }
        }

        private int ReadSpecificElement(string xmlFileName, string input)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(xmlFileName);
            XmlNodeList xNodeList = xDoc.GetElementsByTagName("ClientName");
            List<string> clients = new List<string>();
            int count = 0;
            foreach (XmlNode node in xNodeList)
            {
                clients.Add(node.InnerText);    
            }

            foreach(string client in clients)
            {
                if(client.Contains(input))
                {
                    count++;
                }
                else
                {
                    Log.Write("Client: " + client);
                }
            }

            return count;
        }
    }
}
