using System.IO;
using System.Windows.Forms;
using CSharpTest.Net.IO;
using System.Threading;
using System.ComponentModel;
using System;

namespace FizzBuzz_Forms.Processes.Tree_View
{
    public partial class MissingFiles : Form
    {
        private string _lookupDir;

        public MissingFiles()
        {
            InitializeComponent();
        }

        private void MissingFiles_Load(object sender, EventArgs e)
        {
            _lookupDir = @"\\mass-storage\MassStorage\ParkingArchive\Haringey Council\BLCC\";
            treeview_DirectoryBrowser.Enabled = false;
            treeview_DirectoryBrowser.Visible = false;
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.RunWorkerAsync();
        }
        
        private void panel_TreeView_Paint(object sender, PaintEventArgs e)
        {
            ListDirectory(treeview_DirectoryBrowser, _lookupDir);
        }

        private void ListDirectory(TreeView treeView, string path)
        {
            treeView.Nodes.Clear();
            var rootDirectoryInfo = new DirectoryInfo(path);
            treeView.Nodes.Add(CreateDirectoryNode(rootDirectoryInfo));
        }

        private void ShowEnumerationProgress()
        {
            long total = 0;
            FindFile counter = new FindFile(_lookupDir, "*", true, true, true) {RaiseOnAccessDenied = false};
            counter.FileFound += (o, e) =>
            {
                if (!e.IsDirectory)
                {
                    Interlocked.Increment(ref total);
                }
            };

            Thread t = new Thread(counter.Find)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = "file enum"
            };
            t.Start();

            // Allow the accumulator thread to get a head-start on us
            do { Thread.Sleep(100); }
            while (total < 100 && t.IsAlive);

            // Now we can process the files normally and update a percentage
            long count = 0, percentage = 0;
            FindFile task = new FindFile(_lookupDir, "*", true, true, true) {RaiseOnAccessDenied = false};
            task.FileFound += (o, e) =>
            {
                long progress = ++count * 100 / Interlocked.Read(ref total);
                if (e.IsDirectory || (progress <= percentage) || (progress > 100))
                {
                    return;
                }

                percentage = progress;
                listbox_Loading.Items.Add(progress + Environment.NewLine);
            };
            task.Find();
        }

        private static TreeNode CreateDirectoryNode(DirectoryInfo directoryInfo)
        {
            TreeNode directoryNode = new TreeNode(directoryInfo.Name);
            foreach(DirectoryInfo directory in directoryInfo.GetDirectories())
            {
                directoryNode.Nodes.Add(CreateDirectoryNode(directory));
            }

            foreach(var file in directoryInfo.GetFiles())
            {
                directoryNode.Nodes.Add(new TreeNode(file.Name));
            }

            return directoryNode;
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            using (sender as BackgroundWorker) {
                ShowEnumerationProgress();
            }
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            listbox_Loading.Visible = false;
            treeview_DirectoryBrowser.Enabled = true;
            treeview_DirectoryBrowser.Visible = true;
        }
    }
}


#region Test 1
//// Enumerate Directories
//string[] DirectoriesFound = Directory.EnumerateDirectories(@"\\mass-storage\MassStorage\ParkingArchive\").ToArray();
//List<TreeNode> Children = new List<TreeNode>();
//foreach(var Dir in DirectoriesFound)
//{
//    Children.Add(new TreeNode(Dir));
//}

//// Windows
//TreeNode treeNode = new TreeNode("Windows");
//treeview_DirectoryBrowser.Nodes.Add(treeNode);

//// Linux
//treeNode = new TreeNode("Linux");
//treeview_DirectoryBrowser.Nodes.Add(treeNode);

//// creating 2 childs
//TreeNode treeChildCS = new TreeNode("C#");
//TreeNode treeChildVB = new TreeNode("VB.NET");
//TreeNode[] TreeChilds = new TreeNode[] { treeChildCS, treeChildVB };
//treeNode = new TreeNode("Dot Net Perls", TreeChilds);
//treeview_DirectoryBrowser.Nodes.Add(treeNode);
#endregion