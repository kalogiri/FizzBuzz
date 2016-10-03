namespace FizzBuzz_Forms.Tools
{
    partial class ListBoxLoading
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
            this.textbox2_Loading = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // textbox2_Loading
            // 
            this.textbox2_Loading.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.textbox2_Loading.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.textbox2_Loading.Location = new System.Drawing.Point(12, 12);
            this.textbox2_Loading.Name = "textbox2_Loading";
            this.textbox2_Loading.ReadOnly = true;
            this.textbox2_Loading.Size = new System.Drawing.Size(189, 20);
            this.textbox2_Loading.TabIndex = 1;
            this.textbox2_Loading.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // ListBoxLoading
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(212, 42);
            this.ControlBox = false;
            this.Controls.Add(this.textbox2_Loading);
            this.Name = "ListBoxLoading";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Searching Folder(s)";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.ListBoxLoading_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox textbox2_Loading;
    }
}