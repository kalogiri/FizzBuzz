using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using LibertyUtils;
using Microsoft.Office.Interop.Excel;
using MySql.Data.MySqlClient;
using TSLib;
using static System.Convert;
using static System.Text.RegularExpressions.Regex;

namespace FizzBuzz.Tools
{
    internal class ExcelShit : BaseDownloadScript
    {
        internal class QueryDetails
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

        [Flags]
        private enum ColumnNumbers
        {
            DateReceived = 1,
            BtId = 2,
            BtBatchNo = 3,
            BtFilterName = 4,
            BtItemsInBatch = 5,
            PrintedAndDispatched = 6,
            ConfirmationReturn = 7
        }

        private int _rows;
        private int _cols;
        private int _count;
        private int _step;
        private int _delta;
        private readonly string _reportPath;
        private readonly string _tempFolder;
        private readonly string _csvFileLocation;
        private string _dateReceived;
        private string _btId;
        private string _btBatchNo;
        private string _btFilterName;
        private string _btItemsInBatch;

        private readonly List<string> _filenamesToDeleteFromCsv = new List<string>();

        public ExcelShit()
        {
            DebugLogDir = @"C:\PPProjects\c# Projects\Test\EPPlus Test\DebugLogDir\";
            _reportPath = @"C:\PPProjects\c# Projects\Test\EPPlus Test\Dartford Daily Report_woReport_v6.xlsx";
            _tempFolder = @"C:\PPProjects\c# Projects\Test\EPPlus Test\TEMP\";
            _csvFileLocation = @"C:\PPProjects\c# Projects\Test\EPPlus Test\CSV Location\" + "DailyReport.csv";

            DirUtils.RecreateLogged(@"C:\PPProjects\c# Projects\Test\EPPlus Test\TEMP\");

            Log.Write("Start...");

            // Work done during download process
            //CreateCsvForProcessing();

            // Separate process for creating spreadsheet
            ReadingCsv();

            CheckContinuity();

            //GetBatchAndTime();

            GetBatchAndTime2();

            //DeleteLinesFromCsv();

            Log.Write("Finished!");
        }

        // Experimental method. Intended to reduce the ammount of potential errors.
        private void CreateCsvForProcessing()
        {
            Log.Write("Building report for Dartford");

            List<string> allZipFiles =
                Directory.EnumerateFiles(@"C:\PPProjects\c# Projects\Test\EPPlus Test\", "*.zip").ToList();
            if (!File.Exists(_csvFileLocation))
            {
                File.WriteAllText(_csvFileLocation,
                    @"DateReceived,ZipFilename,DataFilename,bt_id,bt_batch_no,bt_filter_name,bt_items_in_batch");
            }

            // Read the values from csv and store it to a list
            List<string> existingCsvData = File.ReadAllLines(_csvFileLocation).ToList();

            //DirUtils.RecreateLogged(TempFolder);
            CSVDocument csvDoc = new CSVDocument(_csvFileLocation) {Delimiter = ",", QuotedValues = false};
            csvDoc.LoadFile();

            foreach (string zip in allZipFiles)
            {
                using (ZipArchive archive = ZipFile.OpenRead(zip))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string tempZipDirectory = _tempFolder + Path.GetFileNameWithoutExtension(zip);

                        if ((entry.FullName != null) &&
                            !entry.FullName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        string tempZipFileName = Path.Combine(tempZipDirectory, entry.FullName);

                        if (File.Exists(tempZipFileName))
                        {
                            Log.Write(tempZipFileName + " already exists. Skipping");
                            continue;
                        }

                        Directory.CreateDirectory(tempZipDirectory);

                        entry.ExtractToFile(tempZipFileName);

                        string dataFile = Path.GetFileNameWithoutExtension(entry.FullName);
                        string[] dataFileSplits = dataFile?.Split('_');

                        string zipFileName = Path.GetFileNameWithoutExtension(zip);
                        string[] zipFileSplits = zipFileName?.Split('_');

                        string batchId = dataFileSplits?[dataFileSplits.Length - 1];
                        string batchItems = File.ReadLines(tempZipFileName).Last();

                        string batchNumber = zipFileSplits?[zipFileSplits.Length - 1];

                        string batchFilterName;

                        if ((dataFile != null) && dataFile.Contains("WARN"))
                        {
                            batchFilterName = "WL";
                        }
                        else if ((dataFile != null) && dataFile.Contains("MTCPCN") && !dataFile.Contains("WARN"))
                        {
                            batchFilterName = "PCN";
                        }
                        else if ((dataFile != null) && dataFile.Contains("CORRES"))
                        {
                            batchFilterName = "CORRES";
                        }
                        else if ((dataFile != null) && dataFile.Contains("MTCCC"))
                        {
                            batchFilterName = "CC";
                        }
                        else if ((dataFile != null) && dataFile.Contains("MTCNODR"))
                        {
                            batchFilterName = "NODR";
                        }
                        else
                        {
                            batchFilterName = "UNKNOWN";
                        }

                        bool isPresent = existingCsvData.Any(x => x.Split(',').Any(z => z.Contains(batchId)));

                        if (!isPresent)
                        {
                            Log.Write("Adding Row with batch id: " + batchId);
                            csvDoc.AddRow(DateTime.Today.ToString("dd-MMM"), Path.GetFileName(zip),
                                Path.GetFileName(entry.FullName), batchId, batchNumber, batchFilterName, batchItems);
                        }
                        else
                        {
                            Log.Write($"{batchId} already exists");
                        }
                    }
                }
            }

            if (csvDoc.RowCount > 0)
            {
                csvDoc.SaveAs(_csvFileLocation);
            }

            csvDoc.UnloadFile();
        }

        private void ProcessExcelShit(string excelFileName)
        {
            Log.Write("Preparing zip files and data files for ");
            List<string> allZipFiles =
                Directory.EnumerateFiles(@"C:\PPProjects\c# Projects\Test\EPPlus Test\", "*.zip").ToList();

            //DirUtils.RecreateLogged(TempFolder);
            CSVDocument csvDoc = new CSVDocument {Delimiter = ",", QuotedValues = false};
            foreach (string zip in allZipFiles)
            {
                using (ZipArchive archive = ZipFile.OpenRead(zip))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string tempZipDirectory = _tempFolder + Path.GetFileNameWithoutExtension(zip);

                        if (entry.FullName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        {
                            string tempZipFileName = Path.Combine(tempZipDirectory, entry.FullName);

                            if (File.Exists(tempZipFileName))
                            {
                                Log.Write(tempZipFileName + " already exists. Skipping");
                                continue;
                            }

                            Directory.CreateDirectory(tempZipDirectory);

                            entry.ExtractToFile(tempZipFileName);

                            string dataFile = Path.GetFileNameWithoutExtension(entry.FullName);
                            string[] dataFileSplits = dataFile?.Split('_');

                            string zipFileName = Path.GetFileNameWithoutExtension(zip);
                            string[] zipFileSplits = zipFileName?.Split('_');
                            // Conditional acess to check if the value is null

                            string batchId = dataFileSplits?[dataFileSplits.Length - 1];
                            string batchItems = File.ReadLines(tempZipFileName).Last();

                            string batchNumber = zipFileSplits?[zipFileSplits.Length - 1];

                            //string BatchFilterName = ZipFileSplits[0];

                            string batchFilterName;

                            if ((dataFile != null) && dataFile.Contains("WARN"))
                            {
                                batchFilterName = "WL";
                            }
                            else if ((dataFile != null) && dataFile.Contains("MTCPCN") && !dataFile.Contains("WARN"))
                            {
                                batchFilterName = "PCN";
                            }
                            else if ((dataFile != null) && dataFile.Contains("CORRES"))
                            {
                                batchFilterName = "CORRES";
                            }
                            else if ((dataFile != null) && dataFile.Contains("MTCCC"))
                            {
                                batchFilterName = "CC";
                            }
                            else if ((dataFile != null) && dataFile.Contains("MTCNODR"))
                            {
                                batchFilterName = "NODR";
                            }
                            else
                            {
                                batchFilterName = "UNKNOWN";
                            }

                            Log.Write("Data File: " + Path.GetFileName(entry.FullName));
                            Log.Write("Zip File: " + Path.GetFileName(zip));

                            Iterate(excelFileName, DateTime.Today.ToString("dd-MMM"), batchId, batchNumber,
                                batchFilterName, batchItems);
                            csvDoc.AddRow(dataFile, batchId);
                        }
                    }
                }
            }

            if (csvDoc.RowCount > 0)
            {
                csvDoc.SaveAs(_csvFileLocation);
            }

            csvDoc.UnloadFile();
        }

        private void CheckContinuity()
        {
            // First order the csv file that holds all the processed data.
            // The csv will hold:
            // - DataFile Name
            // - BatchID
            // The DataFileName will be used to get the record to extract the printed and dispatched time if available.
            // The BatchID will be used to find hte Row that needs to be sanitized and the range that needs to be sorted.
            Log.Write("Checking for any missing rows");

            List<object> comObjects = new List<object>();
            Application oXl = new Application();

            Workbooks mWorkBooks = oXl.Workbooks;
            comObjects.Add(mWorkBooks);

            Workbook mWorkBook = mWorkBooks.Open(_reportPath);
            comObjects.Add(mWorkBook);

            Sheets mWorkSheets = mWorkBook.Worksheets;
            comObjects.Add(mWorkSheets);

            Worksheet mSheet1 = (Worksheet) mWorkSheets.Item["Sheet3"];
            comObjects.Add(mSheet1);

            Range cells = mSheet1.Cells;
            comObjects.Add(cells);

            Range last = cells.SpecialCells(XlCellType.xlCellTypeLastCell, Type.Missing);
            comObjects.Add(last);

            Range rangeToLookIn = mSheet1.UsedRange;
            comObjects.Add(rangeToLookIn);

            IEnumerable<string> lines = File.ReadAllLines(_csvFileLocation).Skip(1);

            IEnumerable<int> query = from line in lines
                let elem = line.Split(',')
                select ToInt32(elem[3]);

            List<int> results = query.ToList();

            // Get the highest batch id value from the csv file.
            int max = results.Max();

            // Get the lowest batch id value from the csv file.
            int min = results.Min();

            // Then get the row position of the said batch id and look above it.
            try
            {
                Range find = rangeToLookIn.Find(min.ToString(), Missing.Value, XlFindLookIn.xlValues, Missing.Value,
                    Missing.Value, XlSearchDirection.xlNext, false, false, Missing.Value);
                comObjects.Add(find);

                // Get the value of that row.
                int whichRow = find.Row - 1; // This is the row with the min value

                int previousId = Convert.ToInt32(mSheet1.Cells[whichRow, ColumnNumbers.BtId].Value);
                int lastId = Convert.ToInt32(mSheet1.Cells[last.Row, ColumnNumbers.BtId].Value);

                // Then find the diffference between the batch ID in that row and the maximum value in the csv;
                int qualifierOne = lastId - previousId;
                int qualifierTwo = max - previousId;

                int numberOfItems = qualifierOne > qualifierTwo ? qualifierOne : qualifierTwo;
                // Use whichever out of the 2 is the higer value

                _count = 0;

                for (int i = whichRow; i < whichRow + numberOfItems; i++)
                {
                    string evaluatedValue = Convert.ToString(((Range) mSheet1.Cells[i, 2]).Value2);
                    // get the value in the cell for bt_id for the current row

                    //Log.Write("Evaluated Value: " + evaluatedValue);

                    string nextEvaluatedValue = Convert.ToString(((Range) mSheet1.Cells[i + 1, 2]).Value2);
                    // get the value in the cell for bt_id for the next row

                    //Log.Write("Next Evaluated Value: " + nextEvaluatedValue);
                    if (nextEvaluatedValue != null)
                    {
                        _delta = int.Parse(nextEvaluatedValue) - int.Parse(evaluatedValue);
                        // Find the difference between the current row and the next row
                        //Log.Write("Current Value: " + Convert.ToString(delta));
                        //The value for step is the difference between the 2 consecutive rows minus 1.
                        //This is because the value for the current row and the future delta row is alreayd present.
                        //For instance: If the first row has value 10 and the second row contains value 20, the delta
                        //between the 2 value is 10 how ever since the value for the 10th row already exists, i.e 20.
                        //Therefore to determine how many steps the program needs to take to reach the 9th row is 9
                        //since the 10th row already exists, no need to worry about the 10th row for the step calculation
                        _step = _delta - 1;
                        //Log.Write("Difference: " + Convert.ToString(step));
                    }

                    if ((_delta > 1) && (ToInt32(evaluatedValue) > 0))
                    {
                        for (int row = i; row < i + _step; row++)
                        {
                            _count++;

                            if (row < row + _step)
                            {
                                // Insert an empty row first
                                Range emptyRow = mSheet1.Rows[row + 1];
                                emptyRow.Insert(XlInsertShiftDirection.xlShiftDown, false);
                                comObjects.Add(emptyRow);

                                int add = int.Parse(evaluatedValue) + _count;
                                Log.Write("Adding missing batch: " + Convert.ToString(add));

                                Range cell = (Range) mSheet1.Cells[row + 1, 2];
                                cell.Value2 = Convert.ToString(add);
                                cell.Font.Color = Color.FromArgb(156, 0, 6);
                                // Change the color of the font for the missing values
                                comObjects.Add(cell);
                                for (int col = 1; col <= 8; col++)
                                {
                                    Range colCell = (Range) mSheet1.Cells[row + 1, col]; // Insert Rows
                                    colCell.Interior.Color = ColorTranslator.ToOle(Color.LightPink);
                                    // Color the rows with missing values
                                    comObjects.Add(colCell);
                                }
                            }
                        }
                    }
                    _count = 0; // Reset the count so the previous count increment isn't stored.

                    mWorkBook.Save();
                }
            }
            catch (Exception ex)
            {
                Log.Write("No value for " + min + " in the spreadsheet.");
                Log.Write(ex);
                return;
            }

            // Removing Duplicates if any from the spreadsheet

            //Log.Write("Removing duplicates from the spreadsheet if any exists..."); // Possibly can be optimised to only check if there are any duplicates and then carry this operation 
            // rangeToLookIn.RemoveDuplicates(ColumnNumbers.BtId);
            // mWorkBook.Save();

            oXl.Quit();

            foreach (object coms in comObjects)
            {
                ReleaseComObject(coms);
            }
        }

        private void GetBatchAndTime()
        {
            string[] csvLines = File.ReadAllLines(_csvFileLocation);

            IEnumerable<string> query = from line in csvLines
                let elem = line.Split(',')
                select elem[2];

            List<string> results = query.ToList();

            SimpleQuery sQuery = new SimpleQuery();

            foreach (string datafileName in results)
            {
                using (SQL sql = new SQL())
                {
                    MySqlDataReader sqlRead = sql.Select
                    (
                        @"SELECT log.timestamp, task.task_type, task.task_state, job.filename
                          FROM job
                            INNER JOIN log ON log.job_id = job.id
                            INNER JOIN job_type ON job_type.id = job.job_type_id
                            INNER JOIN client on client.id = job_type.client_id
                            INNER JOIN task_list ON task_list.id = job.task_list_id
                            INNER JOIN task ON task.task_list_id = task_list.id
                          WHERE client.name LIKE '%Dartford%' AND job.filename LIKE '%" +
                        Path.GetFileNameWithoutExtension(datafileName) + @"%'
                            AND log.message LIKE '%EndOfDayReport Unstarted -> Finished%'
                          GROUP BY job.filename");

                    try
                    {
                        while (sqlRead.Read())
                        {
                            string timestamp = sqlRead.GetDateTime("timestamp").ToString("dd-MMM");

                            string filename = sqlRead["filename"].ToString();

                            int lenMinus = filename.Contains(".txt") ? 4 : 2;

                            lenMinus = IsMatch(filename, @"V([0-9]{1})") ? 5 : lenMinus;
                            // If the string contains V{number} in the file name (MTCC) files

                            string taskType = Convert.ToString(sqlRead["task_type"]);
                            string taskState = Convert.ToString(sqlRead["task_state"]);

                            string confirmationStage;

                            string firstSplit = filename.Split('-').First();
                            // Get the string upto the first hyphen('-') 
                            string secondSplit = firstSplit.Substring(0, firstSplit.Length - lenMinus);
                            // Get the string without any added formatting done by the download script
                            string simpleFilename = secondSplit + Path.GetExtension(filename);

                            if ((taskType != "11") && (taskState != "10"))
                            {
                                confirmationStage = string.Empty;
                            }
                            else
                            {
                                confirmationStage = "Yes";
                                _filenamesToDeleteFromCsv.Add(simpleFilename);
                            }

                            if (sQuery.Contains(simpleFilename))
                            {
                                continue;
                            }
                            sQuery.Add(new QueryDetails(simpleFilename, timestamp, confirmationStage));
                        }

                        if (sQuery.Count <= 0)
                        {
                            continue;
                        }
                        foreach (QueryDetails item in sQuery)
                        {
                            string[] batchNumbers = Path.GetFileNameWithoutExtension(item.Filename).Split('_');
                            string batchNumber = batchNumbers[batchNumbers.Length - 1];

                            // Search the spread sheet for the row with the batch number
                            try
                            {
                                SearchAndAdd(batchNumber, item.Timestamp, item.ConfirmationStage);
                            }
                            catch (Exception ex)
                            {
                                Log.Write(ex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex);
                    }
                }
            }
        }

        private void GetBatchAndTime2()
        {
            string[] csvLines = File.ReadAllLines(_csvFileLocation);

            IEnumerable<string> query = from line in csvLines
                let elem = line.Split(',')
                select elem[2];

            List<string> results = query.ToList();

            SimpleQuery sQuery = new SimpleQuery();
            string simpleFilename = string.Empty;
            string timestamp = string.Empty;
            string confirmationStage = string.Empty;
            foreach (string datafileName in results)
            {
                using (SQL sql = new SQL())
                {
                    MySqlDataReader sqlRead = sql.Select
                    (
                        @"SELECT log.timestamp, job.filename
                          FROM job
                            INNER JOIN log ON log.job_id = job.id
                            INNER JOIN job_type ON job_type.id = job.job_type_id
                            INNER JOIN client on client.id = job_type.client_id
                          WHERE client.name LIKE '%Dartford%' AND job.filename LIKE '%" +
                        Path.GetFileNameWithoutExtension(datafileName) + @"%'
                            AND log.message LIKE '%EndOfDayReport Unstarted -> Finished%'
                          GROUP BY job.filename"
                    );

                    try
                    {
                        while (sqlRead.Read())
                        {
                            timestamp = sqlRead.GetDateTime("timestamp").ToString("dd-MMM");

                            string filename = sqlRead["filename"].ToString();

                            int lenMinus = filename.Contains(".txt") ? 4 : 2;

                            lenMinus = IsMatch(filename, @"V([0-9]{1})") ? 5 : lenMinus;
                                // If the string contains V{number} in the file name (MTCC) files

                            string firstSplit = filename.Split('-').First();
                                // Get the string upto the first hyphen('-') 

                            string secondSplit = firstSplit.Substring(0, firstSplit.Length - lenMinus);
                                // Get the string without any added formatting done by the download script

                            simpleFilename = secondSplit + Path.GetExtension(filename);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex);
                    }
                }

                using (SQL sql = new SQL())
                {
                    MySqlDataReader sqlRead2 = sql.Select(
                        @"SELECT job.filename
                    FROM job
                        INNER JOIN log ON log.job_id = job.id
                        INNER JOIN job_type ON job_type.id = job.job_type_id
                        INNER JOIN client on client.id = job_type.client_id
                    WHERE client.name LIKE '%Dartford%' AND job.filename LIKE '%" +
                        Path.GetFileNameWithoutExtension(simpleFilename) + @"%'
                        AND log.message LIKE '%Confirmation Started -> Finished%'
                    GROUP BY job.filename"
                    );

                    try
                    {
                        while (sqlRead2.Read())
                        {
                            confirmationStage = sqlRead2.HasRows ? "Yes" : string.Empty;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex);
                    }
                }


                if (sQuery.Contains(simpleFilename))
                {
                    continue;
                }

                sQuery.Add(new QueryDetails(simpleFilename, timestamp, confirmationStage));

                if (sQuery.Count <= 0)
                {
                    continue;
                }
            }

            Log.Write($"Squery count: {sQuery.Count}");

            foreach (QueryDetails item in sQuery)
            {
                string[] batchNumbers = Path.GetFileNameWithoutExtension(item.Filename).Split('_');
                string batchNumber = batchNumbers[batchNumbers.Length - 1];

                // Search the spread sheet for the row with the batch number
                try
                {
                    //SearchAndAdd(batchNumber, item.Timestamp, item.ConfirmationStage);
                    Log.Write(
                        $"Batch Number: {batchNumber}\r\nTimestamp: {item.Timestamp}\r\nConfirmation Stage: {item.ConfirmationStage}");
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                }
            }
        }

        private void RemoveDuplicates()
        {
            Application oX1 = new Application();
            Workbooks mWorkBooks = oX1.Workbooks;
            Workbook mWorkbook = mWorkBooks.Open(_reportPath);
            Sheets mWorkSheets = mWorkbook.Worksheets;
            Worksheet mSheet1 = mWorkSheets.Item["Sheet3"] as Worksheet;
            Range range = mSheet1.UsedRange;

            range.RemoveDuplicates(ColumnNumbers.BtId);

            mWorkbook.Save();
            oX1.Quit();
            ReleaseComObject(oX1);
            ReleaseComObject(mWorkBooks);
            ReleaseComObject(mWorkbook);
            ReleaseComObject(mWorkSheets);
            ReleaseComObject(mSheet1);
            ReleaseComObject(range);
        }

        private void SearchAndAdd(string batchId, string timestamp, string confirmationReturn)
        {
            Application oXl = new Application();
            Workbooks mWorkBooks = oXl.Workbooks;
            Workbook mWorkBook = mWorkBooks.Open(_reportPath);
            Sheets mWorkSheets = mWorkBook.Worksheets;
            Worksheet mSheet1 = (Worksheet) mWorkSheets.Item["Sheet3"];

            Range range = mSheet1.UsedRange.Columns[ColumnNumbers.BtId];

            Range find = range.Find(batchId, Missing.Value, XlFindLookIn.xlValues, Missing.Value, Missing.Value,
                XlSearchDirection.xlNext, false, false, Missing.Value);

            int whichRow = find.Row;

            mSheet1.Cells[whichRow, (int) ColumnNumbers.PrintedAndDispatched] = timestamp;
            mSheet1.Cells[whichRow, (int) ColumnNumbers.ConfirmationReturn] = confirmationReturn;

            mWorkBook.Save();

            oXl.Quit();

            ReleaseComObject(mWorkBooks);
            ReleaseComObject(mWorkBook);
            ReleaseComObject(mWorkSheets);
            ReleaseComObject(mSheet1);
        }

        private void DeleteLinesFromCsv()
        {
            List<string> lines = new List<string>();

            using (StreamReader reader = new StreamReader(_csvFileLocation))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            List<string> distinctFilenames = _filenamesToDeleteFromCsv.Distinct().ToList();

            foreach (string filename in distinctFilenames)
            {
                lines.RemoveAll(l => (filename != null) && l.Contains(Path.GetFileNameWithoutExtension(filename)));
                using (StreamWriter outfile = new StreamWriter(_csvFileLocation))
                {
                    outfile.Write(string.Join(Environment.NewLine, lines.ToArray()));
                }
            }
        }

        private void ReadingCsv()
        {
            // Load data from csv file
            CSVDocument csvDoc = new CSVDocument(_csvFileLocation) { Delimiter = "," };
            csvDoc.LoadFile();
            List<Dictionary<string, string>> csvDataRows = csvDoc.ReadAllKeyed(true);
            csvDoc.UnloadFile();

            List<object> comObjects = new List<object>();

            Application app = new Application();

            Workbooks mWorkBooks = app.Workbooks;
            comObjects.Add(mWorkBooks);

            Workbook mWorkBook = mWorkBooks.Open(_reportPath);
            comObjects.Add(mWorkBook);

            Sheets mWorkSheets = mWorkBook.Worksheets;
            comObjects.Add(mWorkSheets);

            Worksheet mWorksheet = (Worksheet)mWorkSheets.Item["Sheet3"];
            comObjects.Add(mWorksheet);

            //Range range = mWorksheet.Columns[ColumnNumbers.BtId, Type.Missing];
            Range range = mWorksheet.UsedRange;
            comObjects.Add(range);

            try
            {
                foreach (Dictionary<string, string> csvDataRow in csvDataRows)
                {
                    _dateReceived = csvDataRow.LookupLogged("DateReceived");
                    _btId = csvDataRow.LookupLogged("bt_id");
                    _btBatchNo = csvDataRow.LookupLogged("bt_batch_no");
                    _btFilterName = csvDataRow.LookupLogged("bt_filter_name");
                    _btItemsInBatch = csvDataRow.LookupLogged("bt_items_in_batch");

                    Log.Write($"Date Received: {_dateReceived} \n\r" +
                              $"bt_id: {_btId} \n\r" +
                              $"bt_batch_no: {_btBatchNo} \n\r" +
                              $"bt_filter_name: {_btFilterName} \n\r" +
                              $"bt_items_in_batch: {_btItemsInBatch}"
                    );
                    Iterate2(range, mWorksheet);
                    Log.Write("Saving spreadsheet");
                    mWorkBook.Save();
                }
                
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
            finally
            {
                foreach (object coms in comObjects)
                {
                    ReleaseComObject(coms);
                }

                app.Quit();
                Marshal.FinalReleaseComObject(app);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

        }

        private void Iterate2(Range range, _Worksheet mWorksheet)
        {
            List<object> comObjects = new List<object>();
            try
            {
                _rows = range.Rows.Count;
                _cols = range.Columns.Count;

                int currentRow = _rows;
                Log.Write($"Adding {_btId} to the spreadsheet");

                Range findForDuplicatesInRange = range.Find(_btId, Missing.Value, XlFindLookIn.xlValues, Missing.Value, Missing.Value, XlSearchDirection.xlNext, false, false, Missing.Value);
                //Range findForDuplicatesInRange = range.Find(_btId, Missing.Value, XlFindLookIn.xlValues, XlLookAt.xlPart, XlSearchOrder.xlByColumns, XlSearchDirection.xlNext, true, Missing.Value, Missing.Value);
                comObjects.Add(findForDuplicatesInRange);
                for (int row = currentRow; row < currentRow + 1; row++)
                {
                    if (findForDuplicatesInRange == null)
                    {
                        for (int col = 1; col <= _cols; col++)
                        {
                            Range cell = (Range) mWorksheet.Cells[row + 1, col];
                            switch (col) // Column Number
                            {
                                case 1:
                                    cell.Value2 = _dateReceived;
                                    //cell[row, ColumnNumbers.DateReceived] = _dateReceived;
                                    break;
                                case 2:
                                    cell.Value2 = _btId;
                                    cell[row, ColumnNumbers.BtId] = _btId;
                                    break;
                                case 3:
                                    cell.Value2 = _btBatchNo;
                                    //cell[row, ColumnNumbers.BtBatchNo] = _btBatchNo;
                                    break;
                                case 4:
                                    cell.Value2 = _btFilterName;
                                    //cell[row, ColumnNumbers.BtFilterName] = _btFilterName;
                                    break;
                                case 5:
                                    cell.Value2 = _btItemsInBatch;
                                    //cell[row, ColumnNumbers.BtItemsInBatch] = _btItemsInBatch;
                                    break;
                            }
                            comObjects.Add(cell);
                        }
                    }
                    else
                    {
                        Log.Write($"{_btId} already exisits in the spreadsheet.");
                    }
                }

                range = mWorksheet.UsedRange;
                // Have to re-apply the range of the spreadsheet for the new range after the data that needs to be added has been added
                Log.Write("Sorting new row data based on batch id.");
                range.Sort(range.Columns[2, Type.Missing], XlSortOrder.xlAscending,
                    Type.Missing, Type.Missing, XlSortOrder.xlAscending,
                    Type.Missing, XlSortOrder.xlAscending,
                    XlYesNoGuess.xlYes, Type.Missing, Type.Missing, XlSortOrientation.xlSortColumns,
                    XlSortMethod.xlPinYin,
                    XlSortDataOption.xlSortNormal,
                    XlSortDataOption.xlSortNormal,
                    XlSortDataOption.xlSortNormal);

            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
            finally
            {
                foreach (object coms in comObjects)
                {
                    ReleaseComObject(coms);
                }
            }
        }

        private void Iterate(string fileName, string dateReceived, string btId, string btBatchNo, string btFilterName, string btItemsInBatch)
        {
            List<object> comObjects = new List<object>();

            Application oXl = new Application();

            Workbooks mWorkBooks = oXl.Workbooks;
            comObjects.Add(mWorkBooks);

            Workbook mWorkBook = mWorkBooks.Open(fileName);
            comObjects.Add(mWorkBook);

            Sheets mWorkSheets = mWorkBook.Worksheets;
            comObjects.Add(mWorkSheets);

            Worksheet mSheet1 = (Worksheet) mWorkSheets.Item["Sheet3"];
            comObjects.Add(mSheet1);

            Range range = mSheet1.UsedRange;
            comObjects.Add(range);

            _rows = range.Rows.Count;
            _cols = range.Columns.Count;

            int currentRow = _rows;

            for (int row = currentRow; row < currentRow + 1; row++)
            {
                for (int col = 1; col <= _cols; col++)
                {
                    Range cell = (Range) mSheet1.Cells[row + 1, col];

                    switch (col)
                    {
                        case 1:
                            cell.Value2 = dateReceived;
                            break;
                        case 2:
                            cell.Value2 = btId;
                            break;
                        case 3:
                            cell.Value2 = btBatchNo;
                            break;
                        case 4:
                            cell.Value2 = btFilterName;
                            break;
                        case 5:
                            cell.Value2 = btItemsInBatch;
                            break;
                    }
                    ReleaseComObject(cell);
                }
            }

            range = mSheet1.UsedRange;
            // Have to re-apply the range of the spreadsheet for the new range after the data that needs to be added has been added
            Log.Write("Sorting the columns");
            range.Sort(range.Columns[2, Type.Missing], XlSortOrder.xlAscending,
                Type.Missing, Type.Missing, XlSortOrder.xlAscending,
                Type.Missing, XlSortOrder.xlAscending,
                XlYesNoGuess.xlYes, Type.Missing, Type.Missing, XlSortOrientation.xlSortColumns,
                XlSortMethod.xlPinYin,
                XlSortDataOption.xlSortNormal,
                XlSortDataOption.xlSortNormal,
                XlSortDataOption.xlSortNormal);

            mWorkBook.Save();

            oXl.Quit();

            foreach (object coms in comObjects)
            {
                ReleaseComObject(coms);
            }
        }

        private static void ReleaseComObject(object obj)
        {
            try
            {
                Marshal.FinalReleaseComObject(obj);
                obj = null;
            }
            catch
            {
                obj = null;
            }
        }

        private void ReadQuery()
        {
            string[] csvLines = File.ReadAllLines(_csvFileLocation);

            IEnumerable<string> query = from line in csvLines
                let elem = line.Split(',')
                select elem[0];

            List<string> results = query.ToList();
            List<string> timestamps = new List<string>();

            foreach (string result in results)
            {
                using (SQL sql = new SQL())
                {
                    MySqlDataReader sqlRead = sql.Select(
                        @"SELECT log.timestamp, task.task_type, task.task_state, job.filename
                    FROM job
                        INNER JOIN log ON log.job_id = job.id
                        INNER JOIN job_type ON job_type.id = job.job_type_id
                        INNER JOIN client on client.id = job_type.client_id
                        INNER JOIN task_list ON task_list.id = job.task_list_id
                        INNER JOIN task ON task.task_list_id = task_list.id
                    WHERE client.name LIKE '%Dartford%' 
                        AND job.filename LIKE '%" + result + @"%'
                        AND log.message LIKE '%EndOfDayReport Unstarted -> Finished%'
                    GROUP BY job.filename"
                    );
                    try
                    {
                        while (sqlRead.Read())
                        {
                            string timestamp = sqlRead.GetDateTime("timestamp").ToString("dd-MMM");
                            timestamps.Add(timestamp);

                            sqlRead.GetString("filename");

                            Log.Write(timestamp);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex);
                    }
                }
            }

            Log.Write(timestamps.Count.ToString());
        }


    }


}

#region Old Functions

/*
 * 
 * \
 * 

 * 
 *         private void getPrintedAndDispatched()
{
    List<NameTime> NameTimeCollection = new List<NameTime>();

    List<string> filenames = new List<string>();
    List<string> timestamps = new List<string>();

    string[] csvLines = File.ReadAllLines(csvFileLocation);

    var query = from line in csvLines
                let elem = line.Split(',')
                select elem[0];

    var results = query.ToList();

    foreach (var filename in results)
    {
        using (SQL sql = new SQL())
        {
            //var sql_read = sql.Select
            //(
            //    @"
            //    SELECT log.timestamp, job.filename
            //    FROM job 
            //     INNER JOIN log ON log.job_id = job.id
            //     INNER JOIN job_type ON job_type.id = job.job_type_id
            //     INNER JOIN client ON client.id = job_type.client_id
            //    WHERE client.name LIKE '%Dartford%'
            //     AND job.job_state = 0
            //        AND log.message LIKE '%EndOfDayReport Unstarted -> Finished%'
            //    GROUP BY job.filename;"
            //);

            var sql_read = sql.Select
            (
                @"SELECT log.timestamp, task.task_type, task.task_state, job.filename
              FROM job
                INNER JOIN log ON log.job_id = job.id
                INNER JOIN job_type ON job_type.id = job.job_type_id
                INNER JOIN client on client.id = job_type.client_id
                INNER JOIN task_list ON task_list.id = job.task_list_id
                INNER JOIN task ON task.task_list_id = task_list.id
              WHERE client.name LIKE '%Dartford%' AND job.filename LIKE '%" + filename + @"
                AND ptsnow.log.message LIKE '%EndOfDayReport Unstarted -> Finished%'
              GROUP BY job.filename"
            );
            try
            {
                while (sql_read.Read())
                {
                    string timestamp = sql_read.GetDateTime("timestamp").ToString("dd-MMM");
                    filenames.Add(sql_read["filename"].ToString());
                    timestamps.Add(timestamp);

                    string extension = Path.GetExtension(sql_read["filename"].ToString());

                    //string filename = sql_read["filename"].ToString();

                    int lenMinus = filename.Contains(".txt") ? 4 : 2;
                    lenMinus = Regex.IsMatch(filename, @"V([0-9]{1})") ? 5 : lenMinus; // If the string contains V{number} in the file name (MTCC) files
                    string taskType = Convert.ToString(sql_read["task_type"]);
                    string taskState = Convert.ToString(sql_read["task_state"]);
                    string confirmationStage = string.Empty;

                    if (taskType == "11" && taskState == "10")
                    {
                        confirmationStage = "Yes";
                    }
                    else
                    {
                        confirmationStage = "No";
                    }

                    string firstSplit = filename.Split('-').First(); // Get the string upto the first hyphen('-') 
                    string secondSplit = firstSplit.Substring(0, firstSplit.Length - lenMinus); // Get the string without any added formatting done by the download script
                    string simpleFilename = secondSplit + extension;

                    NameTimeCollection.Add(new NameTime(simpleFilename, timestamp));
                }

                if (NameTimeCollection.Count > 0)
                {
                    foreach (NameTime pair in NameTimeCollection.GroupBy(x => new { x.filename, x.timestamp }).Select(g => g.Last())) // Select the file with the most recent time stamp
                    {
                        string[] batchIds = Path.GetFileNameWithoutExtension(pair.filename).Split('_');

                        string batchId = batchIds[batchIds.Length - 1];



                        // Check if the batch ID exists in the csv with all the data that was put in the data file initially in the download script.
                        // If the batch ID exists, find the row with the batch ID and add the timestamp and the thundersnow confirmation stage.

                        //Log.Write("File: " + pair.filename + " Time: " + pair.timestamp + " Batch ID: " + batchId);
                        //FindThatRow(batchId, pair.timestamp);
                    }
                }
                else
                {
                    Log.Write("Something went wrong");
                }

                #region ZippingList
                //if (filenames.Count > 0)
                //{
                //    foreach (var pair in filenames.Zip(timestamps, (f, t) => new { filenames = f, timestamps = t }))
                //    {
                //        string[] batch_ids = Path.GetFileNameWithoutExtension(pair.filenames).Split('_');

                //        string batch_id = batch_ids[batch_ids.Length - 1];

                //        string extension = Path.GetExtension(pair.filenames);

                //        string filename = pair.filenames;

                //        int lenMinus = filename.Contains(".txt") ? 4 : 2;

                //        if(filename.Contains(".xml") && Regex.IsMatch(filename, @"V([0-9]{1})"))
                //        {
                //            lenMinus = 5;
                //        }
                //        string firstSplit = filename.Split('-').First();
                //        string secondSplit = firstSplit.Substring(0, firstSplit.Length - lenMinus);

                //        Log.Write("File: " + secondSplit + extension + " Time: " + pair.timestamps);



                //        //Log.Write("File: " + filename + " Batch ID: " + batch_id + " Time: " + pair.timestamps);
                //        //Log.Write("File: " + filename);
                //    }
                //}
                //else
                //{
                //    Log.Write("Something went wrong");
                //}
                #endregion

            }
            finally
            {
                sql_read.Close();
            }
        }
    }
}

private void FindThatRow(string BatchId, string TimeStamp)
{
    var oXL = new Excel.Application();
    var mWorkBooks = oXL.Workbooks;
    var mWorkBook = mWorkBooks.Open(ReportPath);
    var mWorkSheets = mWorkBook.Worksheets;
    var mSheet1 = (Excel.Worksheet)mWorkSheets.get_Item("Sheet3");

    Excel.Range last = mSheet1.Cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell, Type.Missing);

    Excel.Range RangeToLookIn = mSheet1.get_Range("B1", last);

    Excel.Range find = RangeToLookIn.Find(BatchId, Missing.Value, Excel.XlFindLookIn.xlValues, Missing.Value, Missing.Value, Excel.XlSearchDirection.xlNext, false, false, Missing.Value);

    Log.Write("Found row with: " + BatchId);

    int whichRow = find.Row;

    Log.Write("Adding timestamp");

    mSheet1.Cells[whichRow, (int)ColumnNumbers.Printed_and_dispatched] = TimeStamp;
    mWorkBook.Save();

    mSheet1 = null;
    mWorkBook = null;

    oXL.Quit();

    GC.WaitForPendingFinalizers();
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
}

private int readNewRowNumber()
{
    oXL = new Excel.Application();

    mWorkBook = oXL.Workbooks.Open(ReportPath);
    mWorkSheets = mWorkBook.Worksheets;
    mSheet1 = (Excel.Worksheet)mWorkSheets.get_Item("Sheet3");

    Excel.Range range = mSheet1.UsedRange;

    rows = range.Rows.Count;

    mSheet1 = null;
    mWorkBook = null;
    oXL.Quit();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    return rows;
}


 * 
private void insertMissingSeqNumber()
{
    oXL = new Excel.Application();

    mWorkBook = oXL.Workbooks.Open(ReportPath);
    mWorkSheets = mWorkBook.Worksheets;
    mSheet1 = (Excel.Worksheet)mWorkSheets.get_Item("Sheet3");
    Excel.Range range = mSheet1.UsedRange;
    rows = range.Rows.Count;
    cols = range.Columns.Count;
    Excel.Range c = mSheet1.Cells[rows, 2];
    int val = (int)c.Value2;
    int count = 0;
    int comparer = 3360;
    int difference = comparer - val;
    Console.WriteLine("Old Row Number: " + val);

    if (difference != 0)
    {
        for (int row = rows; row < rows + difference; row++)
        {
            for (int col = 1; col <= cols; col++)
            {
                count++;
                var cell = (Excel.Range)mSheet1.Cells[row + 1, 2];
                var colCell = (Excel.Range)mSheet1.Cells[row + 1, col];
                cell.Value2 = c.Value2 + count;
                cell.Font.Color = Color.FromArgb(156,0,6);
                // Now coloring the cells
                colCell.Interior.Color = ColorTranslator.ToOle(Color.LightPink);
            }
        }
        mWorkBook.Save();
    }

    mSheet1 = null;
    mWorkBook = null;
    oXL.Quit();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    Console.WriteLine("New Row number: " + readNewRowNumber());
}

  private void SpecificCellProcess()
{
    oXL = new Excel.Application();
    mWorkBook = oXL.Workbooks.Open(ReportPath);
    mWorkSheets = mWorkBook.Worksheets;
    mSheet1 = (Excel.Worksheet)mWorkSheets.get_Item("Sheet3");

    Excel.Range last = mSheet1.Cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell, Type.Missing);

    Excel.Range RangeToLookIn = mSheet1.get_Range("A1", last);

    Excel.Range find = RangeToLookIn.Find("Client1 Item 1", Missing.Value, Excel.XlFindLookIn.xlValues, Missing.Value, Missing.Value, Excel.XlSearchDirection.xlNext, false, false, Missing.Value);

    int whichRow = find.Row;

    mSheet1.Cells[whichRow, (int)ColumnNumbers.Comments] = "This is an addition";

    Console.WriteLine(whichRow);

    mWorkBook.Save();

    mSheet1 = null;
    mWorkBook = null;

    oXL.Quit();

    GC.WaitForPendingFinalizers();
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
}

private void readDataFromCSV()
{
    StreamReader reader = new StreamReader(File.OpenRead(@"C:\PPProjects\c# Projects\Test\EPPlus Test\TEMP\WhatWasProcess.csv"));
    List<string> listA = new List<string>();
    List<string> listB = new List<string>();

    while (!reader.EndOfStream)
    {
        var line = reader.ReadLine();
        var values = line.Split(',');

        string Filename = values[0].Trim('"');
        string BatchID = values[2].Trim('"');

        listA.Add(Filename); // Filename
        listB.Add(BatchID); // Batch ID
    }


    foreach (var pair in listA.Zip(listB, (f, b) => new { filename = f, batchid = b}))
    {
        Log.Write("Filename: " + pair.filename + " Batch ID: " + pair.batchid);
    }
}

private void CheckContinuity_OLD(int NumberOfItems)
{
    // First order the csv file that holds all the processed data.
    // The csv will hold:
    // - DataFile Name
    // - BatchID
    // The DataFileName will be used to get the record to extract the printed and dispatched time if available.
    // The BatchID will be used to find hte Row that needs to be sanitized and the range that needs to be sorted.

    oXL = new Excel.Application();
    mWorkBook = oXL.Workbooks.Open(ReportPath);
    mWorkSheets = mWorkBook.Worksheets;
    mSheet1 = (Excel.Worksheet)mWorkSheets.get_Item("Sheet3");

    Excel.Range last = mSheet1.Cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell, Type.Missing);
    Excel.Range RangeToLookIn = mSheet1.get_Range("A1", last);

    Excel.Range find = RangeToLookIn.Find("3365", Missing.Value, Excel.XlFindLookIn.xlValues, Missing.Value, Missing.Value, Excel.XlSearchDirection.xlNext, false, false, Missing.Value);

    // Get the value of that row.
    int whichRow = find.Row; // This is the row with the min value

    Console.WriteLine(find.Value);  

    Excel.Range c = mSheet1.Cells[whichRow, 2];

    count = 0;

    for (int i = whichRow; i < whichRow + NumberOfItems; i++)
    {
        string evaluatedValue = Convert.ToString(((Excel.Range)mSheet1.Cells[i, 2]).Value2); // get the value in the cell for bt_id for the current row
        //Log.Write("Evaluated Value: " + evaluatedValue);

        string nextEvaluatedValue = Convert.ToString(((Excel.Range)mSheet1.Cells[i + 1, 2]).Value2); // get the value in the cell for bt_id for the next row
        //Log.Write("Next Evaluated Value: " + nextEvaluatedValue);

        if (nextEvaluatedValue != null)
        {
            delta = int.Parse(nextEvaluatedValue) - int.Parse(evaluatedValue); // Find the difference between the current row and the next row
            //Log.Write("Current Value: " + Convert.ToString(delta));
            /// The value for step is the difference between the 2 consecutive rows minus 1.
            /// This is because the value for the current row and the future delta row is alreayd present.
            /// For instance: If the first row has value 10 and the second row contains value 20, the delta
            /// between the 2 value is 10 how ever since the value for the 10th row already exists, i.e 20.
            /// Therefore to determine how many steps the program needs to take to reach the 9th row is 9
            /// since the 10th row already exists, no need to worry about the 10th row for the step calculation
            step = delta - 1;
            //Log.Write("Difference: " + Convert.ToString(step));
        }

        if (delta > 1 && Convert.ToInt32(evaluatedValue) > 0)
        {
            for (int row = i; row < i + step; row++)
            {
                Log.Write("Count: " + Convert.ToString(count));

                count++;

                if (row < row + step)
                {
                    // Insert an empty row first
                    Excel.Range EmptyRow = mSheet1.Rows[row + 1];
                    EmptyRow.Insert(Excel.XlInsertShiftDirection.xlShiftDown, false);

                    int add = int.Parse(evaluatedValue) + count;
                    Log.Write("Value getting added: " + Convert.ToString(add));

                    Excel.Range cell = (Excel.Range)mSheet1.Cells[row + 1, 2];
                    cell.Value2 = Convert.ToString(add);
                    cell.Font.Color = Color.FromArgb(156, 0, 6); // Change the color of the font for the missing values

                    for (int col = 1; col <= 8; col++)
                    {
                        var colCell = (Excel.Range)mSheet1.Cells[row + 1, col]; // Insert Rows
                        colCell.Interior.Color = ColorTranslator.ToOle(Color.LightPink); // Color the rows with missing values
                    }
                }
            }
        }
        count = 0; // Reset the count so the previous count increment isn't stored.

        mWorkBook.Save();
    };

    mSheet1 = null;
    mWorkBook = null;

    oXL.Quit();
    ReleaseComObject(mSheet1);
    ReleaseComObject(mWorkSheets);
    ReleaseComObject(mWorkBook);
}

private void Test_AddItemsToTheSpreadSheet()
{
    List<Tuple<string, string, string, string, string, string, string>> RowsToAdd = new List<Tuple<string, string, string, string, string, string, string>>
    {
        Tuple.Create("Client1 Item 1", "3350", "Client1 Item 3", "Client1 Item 4", "Client1 Item 5", "Client1 Item 6", "Client1 Item 7"),
        Tuple.Create("Client2 Item 1", "3355", "Client2 Item 3", "Client2 Item 4", "Client2 Item 5", "Client2 Item 6", "Client2 Item 7"),
        Tuple.Create("Client3 Item 1", "3360", "Client3 Item 3", "Client3 Item 4", "Client3 Item 5", "Client3 Item 6", "Client3 Item 7"),
        Tuple.Create("Client4 Item 1", "3396", "Client4 Item 3", "Client4 Item 4", "Client4 Item 5", "Client4 Item 6", "Client4 Item 7"),
        Tuple.Create("Client5 Item 1", "3397", "Client5 Item 3", "Client5 Item 4", "Client5 Item 5", "Client5 Item 6", "Client5 Item 7"),
        Tuple.Create("Client6 Item 1", "3398", "Client6 Item 3", "Client6 Item 4", "Client6 Item 5", "Client6 Item 6", "Client6 Item 7"),
        Tuple.Create("Client7 Item 1", "3400", "Client7 Item 3", "Client7 Item 4", "Client7 Item 5", "Client7 Item 6", "Client7 Item 7"),
        Tuple.Create("Client8 Item 1", "3401", "Client8 Item 3", "Client8 Item 4", "Client8 Item 5", "Client8 Item 6", "Client8 Item 7"),
        Tuple.Create("Client9 Item 1", "3402", "Client9 Item 3", "Client9 Item 4", "Client9 Item 5", "Client9 Item 6", "Client9 Item 7"),
        Tuple.Create("Client10 Item 1", "3403", "Client10 Item 3", "Client10 Item 4", "Client10 Item 5", "Client10 Item 6", "Client10 Item 7"),
        Tuple.Create("Client11 Item 1", "3408", "Client11 Item 3", "Client11 Item 4", "Client11 Item 5", "Client11 Item 6", "Client11 Item 7")
    };

    foreach (var x in RowsToAdd)
    {
        string one = x.Item1;
        Console.WriteLine("Adding file with batch number: " + x.Item2);
        Iterate(ReportPath, x.Item1, x.Item2, x.Item3, x.Item4, x.Item5, x.Item6, x.Item7, "");
    }
}



private void Iterate(string FileName, string date_recieved, string bt_id, string bt_batch_no, string bt_filter_name, string bt_items_in_batch, string printed_and_dispatched, string confirmation_return, string comments)
{
    oXL = new Excel.Application();

    mWorkBook = oXL.Workbooks.Open(FileName);
    mWorkSheets = mWorkBook.Worksheets;
    mSheet1 = (Excel.Worksheet)mWorkSheets.get_Item("Sheet3");
    Excel.Range range = mSheet1.UsedRange;
    rows = range.Rows.Count;
    cols = range.Columns.Count;

    int currentRow = rows + count;

    for (var row = currentRow; row < currentRow + 1; row++)
    {
        for (var col = 1; col <= cols; col++)
        {
            var cell = (Excel.Range)mSheet1.Cells[row + 1, col];
            switch (col)
            {
                case 1:
                    cell.Value2 = date_recieved;
                    break;
                case 2:
                    cell.Value2 = bt_id;
                    break;
                case 3:
                    cell.Value2 = bt_batch_no;
                    break;
                case 4:
                    cell.Value2 = bt_filter_name;
                    break;
                case 5:
                    cell.Value2 = bt_items_in_batch;
                    break;
                case 6:
                    cell.Value2 = printed_and_dispatched;
                    break;
                case 7:
                    cell.Value2 = confirmation_return;
                    break;
                case 8:
                    cell.Value2 = comments;
                    break;
            }
        }
    }

    OrderColumn(oXL, mWorkBook, mSheet1, rows);

    mWorkBook.Save();

    ReleaseComObject(mSheet1);
    ReleaseComObject(mWorkSheets);
    ReleaseComObject(mWorkBook);

    oXL.Quit();
}
*/

#endregion
