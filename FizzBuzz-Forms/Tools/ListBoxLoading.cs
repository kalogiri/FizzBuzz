using CSharpTest.Net.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
namespace FizzBuzz_Forms.Tools
{
    public partial class ListBoxLoading : Form
    {
        public List<string> Files { get; set; }

        private delegate void SetTextCallback(string text);

        public ListBoxLoading()
        {
            InitializeComponent();
        }

        private void ListBoxLoading_Load(object sender, EventArgs e)
        {
            Files = new List<string>();

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (o, args) => MethodToDoWork();
            bw.RunWorkerCompleted += (o, args) => MethodToUpdateControl();
            bw.RunWorkerAsync();
        }

        private void MethodToDoWork()
        {
            //ListBox lb = new ListBox() { FormattingEnabled = true, Location = new Point(12,10), Size = new Size(297, 290), TabIndex = 0 };

            string pathToEnumerate = @"\\mass-storage\MassStorage\ParkingArchive\Haringey Council\BLCC\";
            long total = 0;
            FindFile counter = new FindFile
            (
                rootDirectory: pathToEnumerate,
                filePattern: "*",
                recursive: true,
                includeFolders: true,
                includeFiles: true
            ) {RaiseOnAccessDenied = false};

            counter.FileFound += (o, e) =>
            {
                if (!e.IsDirectory)
                {
                    Interlocked.Increment(ref total);
                }
            };

            // Start a high-priority thread to get a head-start on the loading bar
            Thread t = new Thread(counter.Find)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = "File Enum"
            };
            t.Start();

            // Allow the accumulator thread to get a head-start on the loading bar
            do { Thread.Sleep(100); }
            while (total < 100 && t.IsAlive);

            // Now we can process the files normally and update a percentage
            long count = 0, percentage = 0;
            FindFile task = new FindFile
            (
                rootDirectory: pathToEnumerate,
                filePattern: "*",
                recursive: true,
                includeFolders: true,
                includeFiles: true
            ) {RaiseOnAccessDenied = false};

            task.FileFound += (o, e) =>
            {
                long progress = ++count * 100 / Interlocked.Read(ref total);

                if (e.IsDirectory || progress <= percentage || progress > 100)
                {
                    return;
                }

                percentage = progress;
                Files.Add($"{percentage}% complete");
                //textbox_Loading.Items.Add(string.Format("{0}% complete", percentage));
                SetText($"{percentage}%");
            };
            task.Find();
        }
        
        private void SetText(string text)
        {
            if(textbox2_Loading.InvokeRequired)
            {
                SetTextCallback d = SetText;
                textbox2_Loading.Invoke(d, text);
            }
            else
            {
                this.textbox2_Loading.Text = text;
            }
        }

        private void MethodToUpdateControl()
        {
            textbox2_Loading.BackColor = Color.FromName("Green");
        }
    }
}
