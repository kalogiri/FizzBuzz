using System;
using System.Collections.ObjectModel;
using System.IO;

namespace FizzBuzz.Tools
{
    

    class KeyedCollectionTest
    {
        private class QueryDetails
        {
            public readonly string Filename;
            public readonly string Timestamp;
            public readonly string ConfirmationStage;

            public QueryDetails(string filename, string timestamp, string confirmationstage)
            {
                Filename = filename;
                Timestamp = timestamp;
                ConfirmationStage = confirmationstage;
            }
        }

        private class SimpleQuery : KeyedCollection<string, QueryDetails>
        {
            protected override string GetKeyForItem(QueryDetails details)
            {
                return details.Filename;
            }
        }

        public KeyedCollectionTest()
        {
            SimpleQuery query = new SimpleQuery();

            foreach(string datafile in Directory.EnumerateFiles(@"C:\PPProjects\c# Projects\Test\EPPlus Test\", "*.*"))
            {
                query.Add(new QueryDetails(Path.GetFileName(datafile), DateTime.Today.ToString("dd-MMM"), Path.GetExtension(datafile)));
            }

            Display(query);
        }

        private void Display(SimpleQuery query)
        {
            Console.WriteLine();

            foreach(QueryDetails item in query)
            {
                Console.WriteLine("Filename: " + item.Filename + Environment.NewLine + "Timestamp: " + item.Timestamp + Environment.NewLine + "Confirmation Stage: " + item.ConfirmationStage);
            }
        }
    }
}
