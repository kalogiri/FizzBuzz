namespace FizzBuzz_Forms.Tools
{
    partial class CreateFolderTree
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
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.textbox_RootLocation = new System.Windows.Forms.TextBox();
            this.button_SelectRoot = new System.Windows.Forms.Button();
            this.button_CreateFolders = new System.Windows.Forms.Button();
            this.checkbox_LiveDataDir = new System.Windows.Forms.CheckBox();
            this.checkbox_ImageDir = new System.Windows.Forms.CheckBox();
            this.checkbox_DownloadDir = new System.Windows.Forms.CheckBox();
            this.checkbox_UploadDir = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // textbox_RootLocation
            // 
            this.textbox_RootLocation.Location = new System.Drawing.Point(12, 12);
            this.textbox_RootLocation.MaximumSize = new System.Drawing.Size(260, 25);
            this.textbox_RootLocation.Multiline = true;
            this.textbox_RootLocation.Name = "textbox_RootLocation";
            this.textbox_RootLocation.Size = new System.Drawing.Size(214, 25);
            this.textbox_RootLocation.TabIndex = 0;
            // 
            // button_SelectRoot
            // 
            this.button_SelectRoot.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_SelectRoot.Location = new System.Drawing.Point(12, 48);
            this.button_SelectRoot.Name = "button_SelectRoot";
            this.button_SelectRoot.Size = new System.Drawing.Size(214, 32);
            this.button_SelectRoot.TabIndex = 1;
            this.button_SelectRoot.Text = "Select Root";
            this.button_SelectRoot.UseVisualStyleBackColor = true;
            this.button_SelectRoot.Click += new System.EventHandler(this.button_SelectRoot_Click);
            // 
            // button_CreateFolders
            // 
            this.button_CreateFolders.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_CreateFolders.Location = new System.Drawing.Point(12, 153);
            this.button_CreateFolders.Name = "button_CreateFolders";
            this.button_CreateFolders.Size = new System.Drawing.Size(214, 32);
            this.button_CreateFolders.TabIndex = 2;
            this.button_CreateFolders.Text = "Create Folders";
            this.button_CreateFolders.UseVisualStyleBackColor = true;
            this.button_CreateFolders.Click += new System.EventHandler(this.button_CreateFolders_Click);
            // 
            // checkbox_LiveDataDir
            // 
            this.checkbox_LiveDataDir.AutoSize = true;
            this.checkbox_LiveDataDir.Location = new System.Drawing.Point(12, 98);
            this.checkbox_LiveDataDir.Name = "checkbox_LiveDataDir";
            this.checkbox_LiveDataDir.Size = new System.Drawing.Size(72, 17);
            this.checkbox_LiveDataDir.TabIndex = 3;
            this.checkbox_LiveDataDir.Text = "Live Data";
            this.checkbox_LiveDataDir.UseVisualStyleBackColor = true;
            // 
            // checkbox_ImageDir
            // 
            this.checkbox_ImageDir.AutoSize = true;
            this.checkbox_ImageDir.Location = new System.Drawing.Point(129, 98);
            this.checkbox_ImageDir.Name = "checkbox_ImageDir";
            this.checkbox_ImageDir.Size = new System.Drawing.Size(60, 17);
            this.checkbox_ImageDir.TabIndex = 5;
            this.checkbox_ImageDir.Text = "Images";
            this.checkbox_ImageDir.UseVisualStyleBackColor = true;
            // 
            // checkbox_DownloadDir
            // 
            this.checkbox_DownloadDir.AutoSize = true;
            this.checkbox_DownloadDir.Location = new System.Drawing.Point(12, 121);
            this.checkbox_DownloadDir.Name = "checkbox_DownloadDir";
            this.checkbox_DownloadDir.Size = new System.Drawing.Size(111, 17);
            this.checkbox_DownloadDir.TabIndex = 6;
            this.checkbox_DownloadDir.Text = "Download Folders";
            this.checkbox_DownloadDir.UseVisualStyleBackColor = true;
            // 
            // checkbox_UploadDir
            // 
            this.checkbox_UploadDir.AutoSize = true;
            this.checkbox_UploadDir.Location = new System.Drawing.Point(129, 121);
            this.checkbox_UploadDir.Name = "checkbox_UploadDir";
            this.checkbox_UploadDir.Size = new System.Drawing.Size(97, 17);
            this.checkbox_UploadDir.TabIndex = 7;
            this.checkbox_UploadDir.Text = "Upload Folders";
            this.checkbox_UploadDir.UseVisualStyleBackColor = true;
            // 
            // CreateFolderTree
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(240, 199);
            this.Controls.Add(this.checkbox_UploadDir);
            this.Controls.Add(this.checkbox_DownloadDir);
            this.Controls.Add(this.checkbox_ImageDir);
            this.Controls.Add(this.checkbox_LiveDataDir);
            this.Controls.Add(this.button_CreateFolders);
            this.Controls.Add(this.button_SelectRoot);
            this.Controls.Add(this.textbox_RootLocation);
            this.Name = "CreateFolderTree";
            this.Text = "CreateFolderTree";
            this.Load += new System.EventHandler(this.CreateFolderTree_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.TextBox textbox_RootLocation;
        private System.Windows.Forms.Button button_SelectRoot;
        private System.Windows.Forms.Button button_CreateFolders;
        private System.Windows.Forms.CheckBox checkbox_LiveDataDir;
        private System.Windows.Forms.CheckBox checkbox_ImageDir;
        private System.Windows.Forms.CheckBox checkbox_DownloadDir;
        private System.Windows.Forms.CheckBox checkbox_UploadDir;
    }
}