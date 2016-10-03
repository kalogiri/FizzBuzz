using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FizzBuzz_Forms.Tools
{
    public partial class CreateFolderTree : Form
    {

        private string RootFolderLocation { get; set; }

        public CreateFolderTree()
        {
            InitializeComponent();
        }

        private void button_SelectRoot_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                RootFolderLocation = folderBrowserDialog.SelectedPath;
            }
        }

        private void button_CreateFolders_Click(object sender, EventArgs e)
        {
            // Declare all the folder locations
            string downloadRoot = $@"{RootFolderLocation}\Download\";
            string uploadRoot = $@"{RootFolderLocation}\Upload\";
            string debugDir = @"DebugLogs\";
            string workingDir = @"WorkingFolder\";
            string imagesDir = $@"{RootFolderLocation}\Images\";
            string tsIncoming = $@"{RootFolderLocation}\Thundersnow\Incoming\";

            List<string> foldersToCreate = new List<string>();
            

        }

        private void CreateFolderTree_Load(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = @"C:\PPProjects\c# Projects\";
        }
    }
}
