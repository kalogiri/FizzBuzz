using LibertyUtils;
using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using TSLib;

namespace FizzBuzz.Scripts
{
    class MeteorReport
    {
        public MeteorReport()
        {
            Log.Default = new Log { Dir = @"C:\RG Scripts\FizzBuzz\Meteor Logs\", DatePrefix = Log.DatePrefixOption.SCRIPT_START_TIME };
            MeteorReports(@"C:\PPProject\c# Projects\Test\ppwatch\Ealing\Upload\Report.csv", null, null, "Meteor");
            Log.Default.Write("====================================================================================");
            Log.Default.Write("PROGRAM START. USING SERVER: " + ConfigThunderSnow.Local.UseServer);
            Console.ReadLine();
        }
        /*
         -------
        | Query |
         -------
        : Search for jobs that are either Confirmation.Unstarted or Confirmation.Aborted.
        : Return the job_id.
        : Return the timestamp for the job_id's when "Finishing Started -> Finished" happens and the print_uids.
        */
        public static void MeteorReports(string outputPath, DateTime? startDate, DateTime? pastEndDate, string clientName = "")
        {
            List<int> jobIds = new List<int>();
            List<string> finishedTimes = new List<string>();
            List<string> printUids = new List<string>();

            using (SQL sql = new SQL())
            {
                MySqlDataReader sqlRead = sql.Select
                (
                    @"
                        SELECT log.timestamp, record.print_uid
                        FROM job
                            INNER JOIN log ON log.job_id = job.id
                            INNER JOIN job_type ON job_type.id = job.job_type_id
                            INNER JOIN client ON client.id = job_type.client_id
                            INNER JOIN task_list ON task_list.id = job.task_list_id
                            INNER JOIN record ON record.job_id = job.id
                            INNER JOIN task ON task.task_list_id = task_list.id 
                        WHERE client.name LIKE @client_name
                            AND ptsnow.task.task_type = @task_type
	                        AND (ptsnow.task.task_state = @current_task_state_unstarted OR ptsnow.task.task_state = @current_task_state_aborted)
	                        AND ptsnow.record.record_state = @record_state
	                        AND ptsnow.job.job_state = @job_state
                            AND log.message LIKE '%Finishing Started -> Finished%'
                        GROUP BY job.id;",
                    parameters: new
                    {
                        client_name = clientName,
                        task_type = TaskType.Confirmation,
                        current_task_state_unstarted = TaskState.Unstarted,
                        current_task_state_aborted = TaskState.Aborted,
                        record_state = RecordState.Active,
                        job_state = JobState.Active
                    }
                    .PropertyDict()
                );

                Log.Default.Write("Generating Report: " + outputPath);
                CSVDocument csv = new CSVDocument();
                csv.Delimiter = ",";
                csv.AddRow("reference", "post_date");

                try
                {
                    while (sqlRead.Read())
                    {
                        string timestamp = sqlRead.GetDateTime("timestamp").ToString("dd/MM/yyyy");
                        csv.AddRow(sqlRead["print_uid"].ToString(), timestamp);

                        finishedTimes.Add(timestamp);
                        printUids.Add(sqlRead["print_uid"].ToString());
                    }
                }
                finally
                {
                    sqlRead.Close();
                }
                Log.Default.Write("Saving CSV");
                csv.SaveAs(outputPath);
                csv.UnloadFile();
            }
        }
    }
}
