using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace FizzBuzz_Forms.Tools
{
    public partial class LoadingBar : Form
    {

        bool _isFinished;
        public LoadingBar()
        {
            InitializeComponent();
            _isFinished = false;
        }

        private void Calculate(int i)
        {
            double pow = Math.Pow(i, i);
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            progressBar1.Maximum = 100;
            progressBar1.Step = 1;
            progressBar1.Value = 0;
            backgroundWorker.RunWorkerAsync();
        }

        private void backgroundWorker_DoWork_1(object sender, DoWorkEventArgs e)
        {
            var backgroundWorker = sender as BackgroundWorker;
            for (long j = 0; j < 100000; j++)
            {
                Calculate((int)j);
                backgroundWorker.ReportProgress((int)(j * 100) / 100000);
            }
            _isFinished = true;
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(_isFinished)
            {
                progressBar1.Enabled = false;
                MessageBox.Show("Finished");
            }
        }
    }
}
