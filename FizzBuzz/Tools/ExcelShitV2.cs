using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using LibertyUtils;
using MySql.Data.MySqlClient;
using TSLib;
using Excel = Microsoft.Office.Interop.Excel;

namespace FizzBuzz.Tools
{
    internal class ExcelShitV2 : BaseDownloadScript
    {
        internal class QueryDetails
        {
            public readonly string Filename;
            public readonly string Timestamp;
            public readonly string ConfirmationStage;

            public QueryDetails(string filename, string timestamp, string confirmationStage)
            {
                Filename = filename;
                Timestamp = timestamp;
                ConfirmationStage = confirmationStage;
            }
        }

        private class SimpleQuery : KeyedCollection<string, QueryDetails>
        {
            protected override string GetKeyForItem(QueryDetails details)
            {
                return details.Filename;
            }
        }

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

        private readonly string _reportPath;
        private readonly string _csvFileLocation;
        private int _count;
        private int _step;
        private int _delta;

        public List<string> FilenamesToDeleteFromCsv { get; } = new List<string>();

        // Global office variables
        private readonly Excel.Workbook _xWorkbook;
        private readonly Excel.Worksheet _xWorksheet;

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

        public ExcelShitV2()
        {
            DebugLogDir = @"C:\PPProjects\c# Projects\Test\EPPlus Test\DebugLogDir\";
            _reportPath = @"C:\PPProjects\c# Projects\Test\EPPlus Test\Dartford Daily Report_woReport_v6.xlsx";
            _csvFileLocation = @"C:\PPProjects\c# Projects\Test\EPPlus Test\CSV Location\" + "DailyReport.csv";

            // Excel variables instantiation
            Excel.Application xApp = new Excel.Application();
            _xWorkbook = xApp.Workbooks.Open(_reportPath);
            Excel.Sheets xSheets = _xWorkbook.Worksheets;
            _xWorksheet = (Excel.Worksheet) xSheets.Item["Sheet3"];

            try
            {
                AddDataToReport();
                CheckForMissingValuesInReport();
                CheckForCompletedDataRow();
                //DeleteLinesFromCsv();
            }
            finally
            {
                ReleaseComObject(xSheets);
                xApp.Quit();
                Marshal.FinalReleaseComObject(xApp);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Log.Write("Finished");
        }

        // Read the csv and collect the appropriate data and add to the spreadsheet
        private void AddDataToReport()
        {
            Log.Write($"Reading data from CSV file: {Path.GetFileName(_csvFileLocation)}");
            CSVDocument csvDoc = new CSVDocument(_csvFileLocation) {Delimiter = ","};
            csvDoc.LoadFile();
            List<Dictionary<string, string>> csvDocRows = csvDoc.ReadAllKeyed(true);
            csvDoc.UnloadFile();

            Log.Write("Setting up excel file");
            List<object> comObjects = new List<object>();

            Excel.Range usedRange = _xWorksheet.UsedRange;
            comObjects.Add(usedRange);

            Excel.Range currentLast = _xWorksheet.Cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell, Type.Missing);
            comObjects.Add(currentLast);
            int currentRowTotal = currentLast.Row;

            Excel.Range btIdRange = _xWorksheet.Range["B2", $"B{currentRowTotal}"];
            comObjects.Add(btIdRange);

            // Add rows to the excel file
            Log.Write($"Adding values to {Path.GetFileName(_reportPath)}");
            try
            {
                // Loop through each of the csv columns and add them to the excel file
                foreach (Dictionary<string, string> row in csvDocRows)
                {
                    string dateReceived = row.LookupLogged("DateReceived");
                    string btId = row.LookupLogged("bt_id");
                    string btBatchNo = row.LookupLogged("bt_batch_no");
                    string btFilterName = row.LookupLogged("bt_filter_name");
                    string btItemsInBatch = row.LookupLogged("bt_items_in_batch");

                    int currentRow = usedRange.Row + 1;

                    Excel.Range findDuplicateBtId = btIdRange.Find(btId, Type.Missing, Excel.XlFindLookIn.xlValues,
                        Type.Missing, Type.Missing, Excel.XlSearchDirection.xlNext, false, false, Type.Missing);

                    // First check if the value already exists in the report before adding it.
                    if (findDuplicateBtId == null)
                    {
                        Log.Write($"Adding row with Batch ID {btId} and Batch Number {btBatchNo}");

                        for (int i = currentRow; i < currentRow + csvDocRows.Count; i++)
                        {
                            Excel.Range emptyRow = _xWorksheet.Rows[currentRow + 1];
                            comObjects.Add(emptyRow);

                            // Insert an empty row for the data to be added before actually adding any data. 
                            // If this doesnt happen only the last value from this iteration will be shown since 
                            // It keeps overwriting the same row with the new data.
                            
                            emptyRow.Insert(Excel.XlInsertShiftDirection.xlShiftDown, false);

                            // Add the data with corresponding data to the report.
                            _xWorksheet.Cells[i + 1, ColumnNumbers.DateReceived] = dateReceived;
                            _xWorksheet.Cells[i + 1, ColumnNumbers.BtId] = btId;
                            _xWorksheet.Cells[i + 1, ColumnNumbers.BtBatchNo] = btBatchNo;
                            _xWorksheet.Cells[i + 1, ColumnNumbers.BtFilterName] = btFilterName;
                            _xWorksheet.Cells[i + 1, ColumnNumbers.BtItemsInBatch] = btItemsInBatch;
                        }
                    }
                    else
                    {
                        Log.Write(
                            $"Duplicate value: Batch ID = {btId} and Batch Number = {btBatchNo} in {findDuplicateBtId.Row}");
                    }
                }

                usedRange = _xWorksheet.UsedRange;
                Log.Write("Soring data based on batch id");

                // Sort the data so that the order it's displayed makes sense.
                usedRange.Sort(usedRange.Columns[ColumnNumbers.BtId, Type.Missing], Excel.XlSortOrder.xlAscending,
                    Type.Missing, Type.Missing, Excel.XlSortOrder.xlAscending,
                    Type.Missing, Excel.XlSortOrder.xlAscending, Excel.XlYesNoGuess.xlYes, Type.Missing, Type.Missing,
                    Excel.XlSortOrientation.xlSortColumns, Excel.XlSortMethod.xlPinYin,
                    Excel.XlSortDataOption.xlSortNormal, Excel.XlSortDataOption.xlSortNormal,
                    Excel.XlSortDataOption.xlSortNormal);

                //Trim();

                _xWorkbook.Save();
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
            finally
            {
                //Release ComObjects
                foreach (object coms in comObjects)
                {
                    ReleaseComObject(coms);
                }

                // Get rid of all the smaller com objects that might have been created that we don't know about.
                // Microsoft.Office.Interop.Excel can be shitty.
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private void CheckForMissingValuesInReport()
        {
            // First order the csv file that holds all the processed data.
            // The csv will hold:
            // - DataFile Name
            // - BatchID
            // The DataFileName will be used to get the record to extract the printed and dispatched time if available.
            // The BatchID will be used to find hte Row that needs to be sanitized and the range that needs to be sorted.
            Log.Write("Checking for any missing rows");

            List<object> comObjects = new List<object>();
            try
            {
                Excel.Range cells = _xWorksheet.Cells;
                comObjects.Add(cells);

                Excel.Range last = cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell, Type.Missing);
                comObjects.Add(last);

                Excel.Range rangeToLookIn = _xWorksheet.UsedRange;
                comObjects.Add(rangeToLookIn);

                IEnumerable<string> lines = File.ReadAllLines(_csvFileLocation).Skip(1);

                IEnumerable<int> query = from line in lines
                    let elem = line.Split(',')
                    select Convert.ToInt32(elem[3]);

                List<int> results = query.ToList();

                int max = results.Max();
                int min = results.Min();

                try
                {
                    Log.Write("Finding last added value on the spreadsheet");
                    Excel.Range find = rangeToLookIn.Find(min.ToString(), Type.Missing, Excel.XlFindLookIn.xlValues,
                        Type.Missing, Type.Missing, Excel.XlSearchDirection.xlNext, false, false, Type.Missing);
                    comObjects.Add(find);

                    int whichRow = find.Row - 1;

                    int oldLadRowId = Convert.ToInt32(_xWorksheet.Cells[whichRow, ColumnNumbers.BtId].Value);
                    int newLastRowId = Convert.ToInt32(_xWorksheet.Cells[whichRow, ColumnNumbers.BtId].Value);

                    int qualifierOne = newLastRowId - oldLadRowId;
                    int qualifierTwo = max - oldLadRowId;

                    int totalNumberOfItemsToAdd = qualifierOne > qualifierTwo ? qualifierOne : qualifierTwo;

                    _count = 0;

                    for (int i = whichRow; i < whichRow + totalNumberOfItemsToAdd; i++)
                    {
                        string currentEvaluatedValue = Convert.ToString(((Excel.Range) _xWorksheet.Cells[i, 2]).Value2);
                        string nextEvaluatedValue = Convert.ToString(((Excel.Range) _xWorksheet.Cells[i + 1, 2]).Value2);

                        if (nextEvaluatedValue != null)
                        {
                            _delta = int.Parse(nextEvaluatedValue) - int.Parse(currentEvaluatedValue);
                            _step = _delta - 1;
                        }

                        if ((_delta <= 1) || (Convert.ToInt32(currentEvaluatedValue) <= 0))
                        {
                            continue;
                        }
                        for (int row = i; row < i + _step; row++)
                        {
                            _count++;

                            if (row >= row + _step)
                            {
                                continue;
                            }
                            // Insert an empty row first
                            Log.Write("Adding empty row");
                            Excel.Range emptyRow = _xWorksheet.Rows[row + 1];
                            emptyRow.Insert(Excel.XlInsertShiftDirection.xlShiftDown, false);
                            comObjects.Add(emptyRow);

                            int add = int.Parse(currentEvaluatedValue) + _count;
                            Log.Write("Adding missing batch: " + Convert.ToString(add));

                            Excel.Range nextCell = (Excel.Range) _xWorksheet.Cells[row + 1, 2];
                            nextCell.Value2 = Convert.ToString(add);

                            // Change the color of the font for the missing values to red and the row to pink
                            Log.Write("Coloring in the missing rows.");
                            nextCell.Font.Color = Color.FromArgb(156, 0, 6);
                            comObjects.Add(nextCell);
                            for (int col = 1; col <= 8; col++)
                            {
                                Excel.Range colCell = (Excel.Range) _xWorksheet.Cells[row + 1, col];
                                // Select the empty row
                                colCell.Interior.Color = ColorTranslator.ToOle(Color.LightPink);
                                comObjects.Add(colCell);
                            }
                        }

                        _count = 0;

                        Log.Write("Saving changes after sanitation");
                        _xWorkbook.Save();
                    }
                }
                catch (Exception ex)
                {
                    Log.Write("No value for " + min + " in the spreadsheet.");

                    Log.Write("================= Error =================");
                    Log.Write(ex);
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
            finally
            {
                Log.Write("Attempting to realease the COM objects.");
                Log.Write("Please check and close EXCEL.exe processes in the task manager if any are open.");

                foreach (object comObject in comObjects)
                {
                    ReleaseComObject(comObject);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private void CheckForCompletedDataRow()
        {
            List<object> comObjects = new List<object>();
            string[] csvLines = File.ReadAllLines(_csvFileLocation);

            // Split the lines from the csv file to get only the filenames (.txt) files.
            IEnumerable<string> query = from line in csvLines
                let elem = line.Split(',')
                select elem[2]; // 0 index so 3rd column

            List<string> results = query.ToList();

            SimpleQuery simpleQuery = new SimpleQuery();

            string simpleFileName = string.Empty;
            string printedAndDispatchedTime = string.Empty;
            string confirmationStage = string.Empty;

            try
            {

                foreach (string result in results)
                {
                    Log.Write(
                        $"Querying database for printed and dispatched time and confirmation stage for file {result}");
                    using (SQL sql = new SQL())
                    {
                        // First get the printed and dispatched time. With the filename aswell.
                        MySqlDataReader sqlRead = sql.Select(
                            @"SELECT log.timestamp, job.filename
                          FROM job
                            INNER JOIN log ON log.job_id = job.id
                            INNER JOIN job_type ON job_type.id = job.job_type_id
                            INNER JOIN client on client.id = job_type.client_id
                            INNER JOIN task_list ON task_list.id = job.task_list_id
                            INNER JOIN task ON task.task_list_id = task_list.id
                          WHERE client.name LIKE @client_name AND job.filename LIKE '%" +
                            Path.GetFileNameWithoutExtension(result) + @"%'
                            AND task.task_state = @task_state
                            AND task.task_type = @task_type
                          GROUP BY job.filename", new
                            {
                                client_name = "Dartford",
                                task_type = TaskType.EndOfDayReport,
                                task_state = TaskState.Finished
                            }.PropertyDict()
                        );
                        try
                        {
                            while (sqlRead.Read())
                            {
                                printedAndDispatchedTime = sqlRead.GetDateTime("timestamp").ToString("dd/MM/yyyy");
                                string filename = sqlRead["filename"].ToString();
                                int lenMinus = filename.Contains(".txt") ? 4 : 2;
                                lenMinus = Regex.IsMatch(filename, @"V([0-9]{1})") ? 5 : lenMinus;
                                string firstSplit = filename.Split('-').First();
                                string secondSplit = firstSplit.Substring(0, firstSplit.Length - lenMinus);
                                simpleFileName = secondSplit + Path.GetExtension(filename);
                            }
                        }

                        catch (Exception ex)
                        {
                            Log.Write(ex);
                        }
                    }

                    using (SQL sql = new SQL())
                    {
                        // Now get the confirmation stage state for the the data file.
                        MySqlDataReader sqlRead2 = sql.Select(
                            @"SELECT job.filename
                        FROM job
                            INNER JOIN job_type ON job_type.id = job.job_type_id
                            INNER JOIN client on client.id = job_type.client_id
                            INNER JOIN task_list ON task_list.id = job.task_list_id
                            INNER JOIN task ON task.task_list_id = task_list.id
                        WHERE client.name = @client_name AND job.filename LIKE '%" +
                            Path.GetFileNameWithoutExtension(simpleFileName) + @"%'
                            AND task.task_state = @task_state
                            AND task.task_type = @task_type
                        GROUP BY job.filename", new
                            {
                                client_name = "Dartford",
                                task_type = TaskType.Confirmation,
                                task_state = TaskState.Finished
                            }.PropertyDict()
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

                    // If any duplicates in filename, skip it.
                    if (simpleQuery.Contains(simpleFileName))
                    {
                        continue;
                    }

                    // Add all 3 data to the helper class which will add the appropriate data to the appropriate row and columns.
                    simpleQuery.Add(new QueryDetails(simpleFileName, printedAndDispatchedTime, confirmationStage));
                }

                // Get the last row of the report file after the data from the csv has been added.
                Excel.Range currentLast = _xWorksheet.Cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell,
                    Type.Missing);

                int rangeBeforeAmmend = currentLast.Row - (csvLines.Length - 1);

                foreach (QueryDetails queryDetails in simpleQuery)
                {

                    if (queryDetails.ConfirmationStage.Equals("Yes"))
                    {
                        FilenamesToDeleteFromCsv.Add(queryDetails.Filename);
                    }
                    string[] batchIds = Path.GetFileNameWithoutExtension(queryDetails.Filename)?.Split('_');
                    string batchId = batchIds?[batchIds.Length - 1];

                    // Search the excel spreadsheet for the row with the batch number
                    try
                    {
                        // Only search the second column upto the last row before the new data from the csv was added.
                        Excel.Range range = _xWorksheet.Range["B2", $"B{rangeBeforeAmmend}"];
                        //Excel.Range range = _xWorksheet.UsedRange.Columns[ColumnNumbers.BtId];
                        comObjects.Add(range);

                        // Find the range row for the matching id.
                        Excel.Range find = range.Find(batchId, Type.Missing, Excel.XlFindLookIn.xlValues, Type.Missing,
                            Type.Missing, Excel.XlSearchDirection.xlNext, false, false, Type.Missing);
                        comObjects.Add(find);

                        int whichRow = find.Row;
                        Log.Write($"WHICHROW: {whichRow} BATCHID: {batchId}");
                        if (!string.IsNullOrEmpty(batchId))
                        {
                            _xWorksheet.Cells[whichRow, (int)ColumnNumbers.PrintedAndDispatched] = printedAndDispatchedTime;
                            _xWorksheet.Cells[whichRow, (int)ColumnNumbers.ConfirmationReturn] = confirmationStage;
                            comObjects.Add(_xWorksheet);
                        }
                        

                        _xWorkbook.Save();
                        comObjects.Add(_xWorkbook);
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex);
                    }
                }
            }
            finally
            {
                foreach (object comObject in comObjects)
                {
                    ReleaseComObject(comObject);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

        }

        private void DeleteLinesFromCsv()
        {
            Log.Write("Removing unwanted lines from the csv");

            List<string> lines = new List<string>();

            using (StreamReader reader = new StreamReader(_csvFileLocation))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            List<string> distinctFilenames = FilenamesToDeleteFromCsv.Distinct().ToList();

            foreach (string distinctFilename in distinctFilenames)
            {
                lines.RemoveAll(
                    l => (distinctFilename != null) && l.Contains(Path.GetFileNameWithoutExtension(distinctFilename)));
                using (StreamWriter outFile = new StreamWriter(_csvFileLocation))
                {
                    outFile.Write(string.Join("\r\n", lines.ToArray()));
                }
            }
        }

        private void Trim()
        {
            Log.Write("Trimming Rows");
            TrimRows();
            Log.Write("Trimming columns");
            TrimColumns();
        }

        private void TrimRows()
        {
            List<object> comObjects = new List<object>();
            try
            {
                Excel.Range range = _xWorksheet.UsedRange;
                comObjects.Add(range);

                while (_xWorksheet.Application.WorksheetFunction.CountA(range.Rows[range.Rows.Count]) == 0)
                {
                    Excel.Range emptyRow = range.Rows[range.Rows.Count] as Excel.Range;
                    comObjects.Add(emptyRow);
                    emptyRow?.Delete();
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
            finally
            {
                //Release ComObjects
                foreach (object coms in comObjects)
                {
                    ReleaseComObject(coms);
                }

                // Get rid of all the smaller com objects that might have been created that we don't know about.
                // Microsoft.Office.Interop.Excel can be shitty.
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private void TrimColumns()
        {
            List<object> comObjects = new List<object>();
            try
            {
                Excel.Range range = _xWorksheet.UsedRange;
                comObjects.Add(range);

                while (_xWorksheet.Application.WorksheetFunction.CountA(range.Columns[range.Columns.Count]) == 0)
                {
                    Excel.Range emptyColumn = range.Columns[range.Columns.Count] as Excel.Range;
                    comObjects.Add(emptyColumn);
                    emptyColumn?.Delete();
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
            finally
            {
                //Release ComObjects
                foreach (object coms in comObjects)
                {
                    ReleaseComObject(coms);
                }

                // Get rid of all the smaller com objects that might have been created that we don't know about.
                // Microsoft.Office.Interop.Excel can be shitty.
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

    }

    internal class AmmendReport : BaseDownloadScript
    {
        // Variable Classes
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

        // Private Variables
        [Flags]
        private enum ColumnNumbers
        {
            DateReceived = 1,
            BtId = 2,
            BtBatchNo = 3,
            BtFilterName = 4,
            BtItemsInBatch = 5,
            PrintedAndDispatched = 6,
            ConfirmationReturn = 7,
            Comments = 8
        }

        private int _rows;
        private int _cols;
        private int _count;
        private int _step;
        private int _delta;
        private readonly string _reportPath;
        private readonly string _csvFileLocation;


        private List<string> _filenamesToDelete;
        public AmmendReport()
        {
            //DebugLogDir = HostPath.ppwatch_3 + @"Data\Dartford\Process\DailyReports\DebugLog\";
            //_reportPath = HostPath.fileserver +
            //                @"ParkingSystem\Resources\Internal Trackers\Dartford Daily Report_woReport_v6.xlsx";
            //_csvFileLocation = HostPath.ppwatch_3 + @"Data\Dartford\Process\DailyReports\CSV\DailyReport.csv";
            DebugLogDir = @"C:\PPProjects\c# Projects\Test\EPPlus Test\DebugLogDir\";
            _reportPath = @"C:\PPProjects\c# Projects\Test\EPPlus Test\Dartford Daily Report_woReport_v6.xlsx";
            _csvFileLocation = @"C:\PPProjects\c# Projects\Test\EPPlus Test\CSV Location\DailyReport.csv";

            Log.Write("Building daily reports...");

            while (IsFileLocked(_reportPath))
            {
                Console.WriteLine(@"Excel report is still open. Please close it before proceeding.");
                Console.ReadKey();
            }

            Ammend();

            CheckContinuity();

            AskThundersnow();

            DeleteLinesFromCsv();

            Log.Write("Finished!");
        }

        private static bool IsFileLocked(string filename)
        {
            FileInfo file = new FileInfo(filename);
            FileStream stream = null;
            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                stream?.Close();
            }
            return false;
        }

        private void Ammend()
        {
            List<object> comObjects = new List<object>();

            Excel.Application app = new Excel.Application();

            Excel.Workbooks mWorkBooks = app.Workbooks;
            comObjects.Add(mWorkBooks);

            Excel.Workbook mWorkBook = mWorkBooks.Open(_reportPath);
            comObjects.Add(mWorkBook);

            Excel.Sheets mWorkSheets = mWorkBook.Worksheets;
            comObjects.Add(mWorkSheets);

            Excel.Worksheet mWorksheet = (Excel.Worksheet)mWorkSheets.Item["Sheet3"];
            comObjects.Add(mWorksheet);

            Excel.Range range = mWorksheet.UsedRange;
            comObjects.Add(range);

            Excel.Range btIdRange = mWorksheet.UsedRange.Columns[ColumnNumbers.BtId];
            comObjects.Add(btIdRange);

            try
            {
                Log.Write("Loading data from csv.");
                Log.Write("Ammending excel spreadsheet to add new data.");
                // Load data from csv
                CSVDocument csvDoc = new CSVDocument(_csvFileLocation) { Delimiter = "," };
                csvDoc.LoadFile();
                List<Dictionary<string, string>> csvDataRows = csvDoc.ReadAllKeyed(true);
                csvDoc.UnloadFile();

                foreach (Dictionary<string, string> csvDataRow in csvDataRows)
                {
                    string dateReceived = csvDataRow.LookupLogged("DateReceived");
                    string btId = csvDataRow.LookupLogged("bt_id");
                    string btBatchNo = csvDataRow.LookupLogged("bt_batch_no");
                    string btFilterName = csvDataRow.LookupLogged("bt_filter_name");
                    string btItemsInBatch = csvDataRow.LookupLogged("bt_items_in_batch");

                    Log.Write($"Adding filename details with BatchID (Data file): {btId} and BatchNo(Zip File): {btBatchNo}");
                    _rows = range.Rows.Count;
                    _cols = range.Columns.Count;

                    int currentRow = _rows;
                    Log.Write($"Adding {btId} to the spreadsheet");
                    Excel.Range findForDuplicatesInRange = range.Find(btId, Missing.Value, Excel.XlFindLookIn.xlValues, Missing.Value, Missing.Value, Excel.XlSearchDirection.xlNext, false, false, Missing.Value);
                    comObjects.Add(findForDuplicatesInRange);

                    if (findForDuplicatesInRange == null)
                    {
                        for (int row = currentRow; row < currentRow + 1; row++)
                        {
                            for (int col = 1; col <= _cols; col++)
                            {
                                Excel.Range cell = (Excel.Range) mWorksheet.Cells[row + 1, col];
                                switch (col) // Column Number
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
                                comObjects.Add(cell);
                            }
                        }
                    }
                    else
                    {
                        Log.Write($"{btId} already exists in the spreadsheet");
                    }

                    range = mWorksheet.UsedRange;
                    // Have to re-apply the range of the spreadsheet for the new range after the data that needs to be added has been added
                    Log.Write("Sorting new row data based on batch id.");
                    range.Sort(range.Columns[2, Type.Missing], Excel.XlSortOrder.xlAscending,
                        Type.Missing, Type.Missing, Excel.XlSortOrder.xlAscending,
                        Type.Missing, Excel.XlSortOrder.xlAscending,
                        Excel.XlYesNoGuess.xlYes, Type.Missing, Type.Missing, Excel.XlSortOrientation.xlSortColumns,
                        Excel.XlSortMethod.xlPinYin,
                        Excel.XlSortDataOption.xlSortNormal,
                        Excel.XlSortDataOption.xlSortNormal,
                        Excel.XlSortDataOption.xlSortNormal);
                }

                mWorkBook.Save();
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

        private void CheckContinuity()
        {
            // Check the value of bt_id above the smallest bt_id value added.
            // Check the difference between that row and the row below it
            // Fill in the gaps if any and fill that row with pink as well.
            // Additional function (needs testing): Delete any duplicates - This probably won't happen but if and when it does uncomment the section that does this below.

            Log.Write("Checking for any missing row");

            List<object> comObjects = new List<object>();
            Excel.Application app = new Excel.Application();

            try
            {
                Excel.Workbooks mWorkBooks = app.Workbooks;
                comObjects.Add(mWorkBooks);

                Excel.Workbook mWorkBook = mWorkBooks.Open(_reportPath);
                comObjects.Add(mWorkBook);

                Excel.Sheets mWorkSheets = mWorkBook.Worksheets;
                comObjects.Add(mWorkSheets);

                Excel.Worksheet mWorkSheet = (Excel.Worksheet)mWorkSheets.Item["Sheet3"];
                comObjects.Add(mWorkSheet);

                Excel.Range cells = mWorkSheet.Cells;
                comObjects.Add(cells);

                Excel.Range last = cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell, Type.Missing);
                comObjects.Add(last);

                Excel.Range rangeToLookIn = mWorkSheet.UsedRange;
                comObjects.Add(rangeToLookIn);

                IEnumerable<string> lines = File.ReadAllLines(_csvFileLocation).Skip(1);

                IEnumerable<int> query = from line in lines
                                            let elem = line.Split(',')
                                            select Convert.ToInt32(elem[3]);

                List<int> results = query.ToList();

                int max = results.Max();
                int min = results.Min();

                try
                {
                    Log.Write("Finding last added value on the spreadsheet");
                    Excel.Range find = rangeToLookIn.Find(min.ToString(), Missing.Value, Excel.XlFindLookIn.xlValues,
                        Missing.Value,
                        Missing.Value, Excel.XlSearchDirection.xlNext, false, false, Missing.Value);
                    comObjects.Add(find);

                    int whichRow = find.Row - 1;

                    int oldLastRowId = Convert.ToInt32(mWorkSheet.Cells[whichRow, ColumnNumbers.BtId].Value);
                    int newLastRowId = Convert.ToInt32(mWorkSheet.Cells[last.Row, ColumnNumbers.BtId].Value);

                    int qualifierOne = newLastRowId - oldLastRowId;
                    int qualifierTwo = max - oldLastRowId;

                    int totalNumberOfItemsToAdd = qualifierOne > qualifierTwo ? qualifierOne : qualifierTwo;

                    _count = 0;

                    for (int i = whichRow; i < whichRow + totalNumberOfItemsToAdd; i++)
                    {
                        string currentEvaluatedValue = Convert.ToString(((Excel.Range)mWorkSheet.Cells[i, 2]).Value2);
                        string nextEvaluatedValue = Convert.ToString(((Excel.Range)mWorkSheet.Cells[i + 1, 2]).Value2);

                        if (nextEvaluatedValue != null)
                        {
                            _delta = int.Parse(nextEvaluatedValue) - int.Parse(currentEvaluatedValue);
                            _step = _delta - 1;
                        }

                        if ((_delta <= 1) || (Convert.ToInt32(currentEvaluatedValue) <= 0))
                        {
                            continue;
                        }
                        for (int row = i; row < i + _step; row++)
                        {
                            _count++;

                            if (row >= row + _step)
                            {
                                continue;
                            }
                            // Insert an empty row first
                            Excel.Range emptyRow = mWorkSheet.Rows[row + 1];
                            emptyRow.Insert(Excel.XlInsertShiftDirection.xlShiftDown, false);
                            comObjects.Add(emptyRow);

                            int add = int.Parse(currentEvaluatedValue) + _count;
                            Log.Write("Adding missing batch: " + Convert.ToString(add));

                            Excel.Range nextCell = (Excel.Range)mWorkSheet.Cells[row + 1, 2];
                            nextCell.Value2 = Convert.ToString(add);

                            // Change the color of the font for the missing values to red and the row to pink
                            nextCell.Font.Color = Color.FromArgb(156, 0, 6);
                            comObjects.Add(nextCell);
                            for (int col = 1; col <= 8; col++)
                            {
                                Excel.Range colCell = (Excel.Range)mWorkSheet.Cells[row + 1, col];
                                // Select the empty row
                                colCell.Interior.Color = ColorTranslator.ToOle(Color.LightPink);
                                comObjects.Add(colCell);
                            }
                        }

                        _count = 0;

                        mWorkBook.Save();
                    }
                }
                catch (Exception ex)
                {
                    Log.Write("No value for " + min + " in the spreadsheet.");

                    Log.Write("================= Error =================");
                    Log.Write(ex);
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
            finally
            {
                Log.Write("Attempting to realease the COM objects.");
                Log.Write("Please check and close EXCEL.exe processes in the task manager if any are open.");

                foreach (object comObject in comObjects)
                {
                    ReleaseComObject(comObject);
                }

                app.Quit();
                Marshal.FinalReleaseComObject(app);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private void AskThundersnow()
        {
            string[] csvLines = File.ReadAllLines(_csvFileLocation);
            IEnumerable<string> query = from line in csvLines
                                        let elem = line.Split(',')
                                        select elem[2];
            List<string> results = query.ToList();

            SimpleQuery simpleQuery = new SimpleQuery();

            string simpleFilename = string.Empty;
            string printedAndDispatchedTime = string.Empty;
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
                            WHERE client.name LIKE '%Dartford%' AND job.filename LIKE '%" + Path.GetFileNameWithoutExtension(datafileName) + @"%'
                            AND log.message LIKE '%EndOfDayReport Unstarted -> Finished%'
                            GROUP BY job.filename");

                    try
                    {
                        while (sqlRead.Read())
                        {
                            printedAndDispatchedTime = sqlRead.GetDateTime("timestamp").ToString("dd/MM/yyyy");

                            string filename = sqlRead["filename"].ToString();

                            int lenMinus = filename.Contains(".txt") ? 4 : 2;

                            lenMinus = Regex.IsMatch(filename, @"V([0-9]{1})") ? 5 : lenMinus;
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
                            INNER JOIN job_type ON job_type.id = job.job_type_id
                            INNER JOIN client on client.id = job_type.client_id
                            INNER JOIN task_list ON task_list.id = job.task_list_id
                            INNER JOIN task ON task.task_list_id = task_list.id
                        WHERE client.name LIKE '%Dartford%' AND job.filename LIKE '%" +
                            Path.GetFileNameWithoutExtension(simpleFilename) + @"%'
                            AND task.task_state = @task_state
                            AND task.task_type = @task_type
                        GROUP BY job.filename", new
                            {
                                task_state = TaskState.Finished,
                                task_type = TaskType.Confirmation
                            }.PropertyDict()
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

                if (simpleQuery.Contains(simpleFilename))
                {
                    continue;
                }

                simpleFilename = simpleFilename.Replace(".xml", ".txt");

                if (!string.IsNullOrWhiteSpace(simpleFilename))
                {
                    simpleQuery.Add(new QueryDetails(simpleFilename, printedAndDispatchedTime, confirmationStage));
                }

                confirmationStage = string.Empty;
            }

            _filenamesToDelete = new List<string>();
            foreach (QueryDetails item in simpleQuery)
            {
                string[] batchNumbers = Path.GetFileNameWithoutExtension(item.Filename)?.Split('_');
                string batchNumber = batchNumbers?[batchNumbers.Length - 1];

                // Search the spread sheet for the row with the batch number
                try
                {
                    AddData(batchNumber, item.Timestamp, item.ConfirmationStage);

                    if (item.ConfirmationStage.Contains("Yes"))
                    {
                        Log.Write($"File {item.Filename} is at confirmation stage.");
                        _filenamesToDelete.Add(item.Filename);
                    }
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                }
            }
        }

        private void AddData(string batchId, string timestamp, string confirmationReturn)
        {
            List<object> comObjects = new List<object>();

            Excel.Application app = new Excel.Application();

            try
            {
                Excel.Workbooks mWorkBooks = app.Workbooks;
                comObjects.Add(mWorkBooks);

                Excel.Workbook mWorkBook = mWorkBooks.Open(_reportPath);
                comObjects.Add(mWorkBook);

                Excel.Sheets mWorkSheets = mWorkBook.Worksheets;
                comObjects.Add(mWorkSheets);

                Excel.Worksheet mWorkSheet = (Excel.Worksheet)mWorkSheets.Item["Sheet3"];
                comObjects.Add(mWorkSheet);

                Excel.Range range = mWorkSheet.UsedRange;
                comObjects.Add(range);

                Excel.Range find = range.Find(batchId, Missing.Value, Excel.XlFindLookIn.xlValues, Missing.Value, Missing.Value,
                    Excel.XlSearchDirection.xlNext, false, false, Missing.Value);
                comObjects.Add(find);

                int whichRow = find.Row;

                if (!string.IsNullOrWhiteSpace(batchId))
                {
                    mWorkSheet.Cells[whichRow, (int)ColumnNumbers.PrintedAndDispatched] = timestamp;
                    mWorkSheet.Cells[whichRow, (int)ColumnNumbers.ConfirmationReturn] = confirmationReturn;

                    mWorkBook.Save();
                }
                
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }

            finally
            {
                foreach (object comObject in comObjects)
                {
                    ReleaseComObject(comObject);
                }

                app.Quit();
                Marshal.FinalReleaseComObject(app);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private void DeleteLinesFromCsv()
        {
            while (IsFileLocked(_csvFileLocation))
            {
                Console.WriteLine($@"The csv file is still open. Please close the file {_csvFileLocation} before proceeding.");
                Console.ReadKey();
            }

            Log.Write("Removing unwanted lines from the csv");

            List<string> lines = new List<string>();

            using (StreamReader reader = new StreamReader(_csvFileLocation))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            List<string> distinctFilenames = _filenamesToDelete.Distinct().ToList();

            foreach (string distinctFilename in distinctFilenames)
            {
                
                Log.Write($"Deleteing {distinctFilename} from the csv");
                lines.RemoveAll(l => l.Contains(Path.GetFileNameWithoutExtension(distinctFilename)));
                using (StreamWriter outFile = new StreamWriter(_csvFileLocation))
                {
                    outFile.Write(string.Join("\r\n", lines.ToArray()));
                }
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
    }
}
