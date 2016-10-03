using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibertyUtils;
using MySql.Data.MySqlClient;
using TSLib;

namespace FizzBuzz.Tools
{
    class CheckThundersnow : BaseDownloadScript
    {
        private List<Job> _jobs;
        private string _simpleFilename;
        private string _printedAndDispatchedTime;
        private string _confirmationStage;
        private readonly string _csvFileLocation;

        public CheckThundersnow()
        {
            DebugLogDir = @"C:\PPProjects\c# Projects\Test\EPPlus Test\";
            _csvFileLocation = @"C:\PPProjects\c# Projects\Test\EPPlus Test\CSV Location\DailyReport.csv";
            ThundersnowQueryTest();
        }

        private void QueryTest()
        {
            string result = @"DFFC_IA_160921_CORRES_3613";
            string simpleFilename = string.Empty;
            string timestamp = string.Empty;
            string confirmationStage = string.Empty;
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
                    WHERE client.name LIKE @client_name 
                        AND job.filename LIKE @filename_Lookup
                        AND task.task_type = @task_type AND task.task_state = @task_state
                    GROUP BY job.filename", new
                    {
                        client_name = "Dartford",
                        filename_Lookup = Path.GetFileNameWithoutExtension(result),
                        task_type = TaskType.Confirmation,
                        task_state = TaskState.Finished
                    }.PropertyDict());

                try
                {
                    while (sqlRead.Read())
                    {
                        timestamp = sqlRead.GetDateTime("timestamp").ToString("dd/MM/yyyy");
                        string filename = sqlRead["filename"].ToString();

                        int lenMinus = filename.Contains(".txt") ? 4 : 2;

                        lenMinus = Regex.IsMatch(filename, @"V([0-9]{1})") ? 5 : lenMinus;
                        // If the string contains V{number} in the file name (MTCC) files

                        int taskType = Convert.ToInt32(sqlRead["task_type"]);
                        int taskState = Convert.ToInt32(sqlRead["task_state"]);

                        string firstSplit = filename.Split('-').First(); // Get the string upto the first hyphen(-)
                        string secondSplit = firstSplit.Substring(0, firstSplit.Length - lenMinus);
                        simpleFilename = secondSplit + Path.GetExtension(filename);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
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
                    confirmationStage = sqlRead2.HasRows ? "Yes" : "No";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            Console.WriteLine(confirmationStage);
            Console.WriteLine(simpleFilename);
            Console.WriteLine(timestamp);
        }

        private void ThundersnowQueryTest()
        {
            string[] csvLines = File.ReadAllLines(_csvFileLocation).Skip(1).ToArray();
            IEnumerable<string> query = from line in csvLines
                let elem = line.Split(',')
                select elem[2];
            List<string> results = query.ToList();

            foreach (string result in results)
            {
                Log.Write($"Querying database for file {result}");
                _jobs = Job.GetJobsWithFilename("Dartford", result);
            }

            Log.Write(_jobs.Count);
            foreach (Job job in _jobs)
            {
                using (SQL sql = new SQL())
                {
                    MySqlDataReader sqlRead = sql.Select
                    (
                        @"
                        SELECT log.timestamp, job.filename
                        FROM job
                            INNER JOIN log ON log.job_id = job.id
                            INNER JOIN job_type ON job_type.id = job.job_type_id
                            INNER JOIN client on client.id = job_type.client_id
                        WHERE client.name LIKE '%Dartford%' AND job.filename LIKE '%" +
                        Path.GetFileNameWithoutExtension(job.Filename) + @"%'
                            AND log.message LIKE '%EndOfDayReport Unstarted -> Finished%'
                        GROUP BY job.filename"
                    );

                    try
                    {
                        while (sqlRead.Read())
                        {
                            _printedAndDispatchedTime = sqlRead.GetDateTime("timestamp").ToString("dd/MM/yyyy");

                            string filename = sqlRead["filename"].ToString();

                            int lenMinus = filename.Contains(".txt") ? 4 : 2;

                            lenMinus = Regex.IsMatch(filename, @"V([0-9]{1})") ? 5 : lenMinus;
                            // If the string contains V{number} in the file name (MTCC) files

                            string firstSplit = filename.Split('-').First();
                            // Get the string upto the first hyphen('-') 
                            string secondSplit = firstSplit.Substring(0, firstSplit.Length - lenMinus);
                            // Get the string without any added formatting done by the download script
                            _simpleFilename = secondSplit + Path.GetExtension(filename);
                        }                        
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex);
                        throw new Exception(ex.Message);
                    }
                }

                Log.Write($"Filename: {_simpleFilename} \r\nPrinted and dispatched: {_printedAndDispatchedTime}\r\nConfirmation Stage: {_confirmationStage}");
            }
        }
    }
}
