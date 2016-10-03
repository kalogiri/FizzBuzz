namespace FizzBuzz_Forms.Processes.Tree_View
{
    partial class MissingFiles
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel_MainWindow = new System.Windows.Forms.Panel();
            this.treeview_DirectoryBrowser = new System.Windows.Forms.TreeView();
            this.panel_TreeView = new System.Windows.Forms.Panel();
            this.listbox_Loading = new System.Windows.Forms.ListBox();
            this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.panel_TreeView.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel_MainWindow
            // 
            this.panel_MainWindow.Location = new System.Drawing.Point(211, 6);
            this.panel_MainWindow.Name = "panel_MainWindow";
            this.panel_MainWindow.Size = new System.Drawing.Size(274, 342);
            this.panel_MainWindow.TabIndex = 1;
            // 
            // treeview_DirectoryBrowser
            // 
            this.treeview_DirectoryBrowser.Location = new System.Drawing.Point(0, 0);
            this.treeview_DirectoryBrowser.Name = "treeview_DirectoryBrowser";
            this.treeview_DirectoryBrowser.Size = new System.Drawing.Size(200, 342);
            this.treeview_DirectoryBrowser.TabIndex = 0;
            // 
            // panel_TreeView
            // 
            this.panel_TreeView.Controls.Add(this.listbox_Loading);
            this.panel_TreeView.Controls.Add(this.treeview_DirectoryBrowser);
            this.panel_TreeView.Location = new System.Drawing.Point(5, 6);
            this.panel_TreeView.Name = "panel_TreeView";
            this.panel_TreeView.Size = new System.Drawing.Size(200, 342);
            this.panel_TreeView.TabIndex = 0;
            this.panel_TreeView.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_TreeView_Paint);
            // 
            // listbox_Loading
            // 
            this.listbox_Loading.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listbox_Loading.FormattingEnabled = true;
            this.listbox_Loading.Location = new System.Drawing.Point(0, 0);
            this.listbox_Loading.Name = "listbox_Loading";
            this.listbox_Loading.Size = new System.Drawing.Size(200, 338);
            this.listbox_Loading.TabIndex = 2;
            // 
            // backgroundWorker
            // 
            this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_DoWork);
            this.backgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker_RunWorkerCompleted);
            // 
            // MissingFiles
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(490, 359);
            this.Controls.Add(this.panel_TreeView);
            this.Controls.Add(this.panel_MainWindow);
            this.Name = "MissingFiles";
            this.Text = "Missing Files";
            this.Load += new System.EventHandler(this.MissingFiles_Load);
            this.panel_TreeView.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panel_MainWindow;
        private System.Windows.Forms.TreeView treeview_DirectoryBrowser;
        private System.Windows.Forms.Panel panel_TreeView;
        private System.ComponentModel.BackgroundWorker backgroundWorker;
        private System.Windows.Forms.ListBox listbox_Loading;
    }
}

