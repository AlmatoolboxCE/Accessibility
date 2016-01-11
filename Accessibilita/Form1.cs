using Accessibilita.Properties;
using DoctypeEncodingValidation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Accessibilita
{
    public partial class Form1 : Form
    {

        private enum Operazione
        {
            ACCESSIBILITA,
            VALIDAZIONE,
        }

        List<string> UrlDaTestare;

        private string Domain;
        private string User;
        private string Password;

        public Form1()
        {
            UrlDaTestare = new List<string>();
            InitializeComponent();

            backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
            backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
        }

        void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage < this.progressBar1.Maximum)
            {
                this.progressBar1.Value = e.ProgressPercentage;
            }
        }

        void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("Validation Cancelled");
            }
            else
            {
                System.Diagnostics.Process.Start("explorer.exe", Settings.Default.LastDir);
            }
            this.progressBar1.Visible = false;
            this.button3.Enabled = false;
            this.button3.Visible = false;
            this.button1.Enabled = true;
            this.button2.Enabled = true;
        }

        //valida accessibilità
        private void button2_Click(object sender, EventArgs e)
        {
            ExecuteControl(Operazione.ACCESSIBILITA);

        }

        private void InitBGWorkerStart()
        {
            this.progressBar1.Minimum = 0;
            this.progressBar1.Maximum = UrlDaTestare.Count;
            this.progressBar1.Value = this.progressBar1.Minimum;
            this.button3.Enabled = true;
            this.button3.Visible = true;
            this.progressBar1.Visible = true;
            this.button2.Enabled = false;
            this.button1.Enabled = false;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UrlDaTestare = textBox1.Text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (UrlDaTestare.Count > 0)
            {
                Settings.Default.LastUrls = textBox1.Text;
                Settings.Default.Save();
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            Domain = textBox2.Text;
            Settings.Default.Domain = Domain;
            Settings.Default.Save();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            User = textBox3.Text;
            Settings.Default.User = User;
            Settings.Default.Save();
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            Password = textBox4.Text;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = Settings.Default.LastUrls;
            textBox2.Text = Domain = Settings.Default.Domain;
            textBox3.Text = User = Settings.Default.User;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int i = 0;
            foreach (var url in UrlDaTestare)
            {
                System.Threading.Thread.Sleep(1000);
                using (var wc = new WebClient())
                {
                    if (!string.IsNullOrWhiteSpace(User) &&
                        !string.IsNullOrWhiteSpace(Password) &&
                        !string.IsNullOrWhiteSpace(Domain))
                    {
                        wc.Credentials = new System.Net.NetworkCredential(User, Password, Domain);
                    }
                    try
                    {
                        var fileName = (string)((object[])e.Argument)[0];
                        Operazione tipoOperazione = (Operazione)((object[])e.Argument)[1];
                        //var url = this.baseAddress + "/home";
                        var data = wc.DownloadString(url);
                        HtmlValidationSemplified.validationResults responseData = null;
                        switch (tipoOperazione)
                        {
                            case Operazione.ACCESSIBILITA:
                                responseData = HtmlValidationSemplified.ValidateAccessibility(data);
                                break;
                            case Operazione.VALIDAZIONE:
                                responseData = DoctypeEncodingValidation.HtmlValidationSemplified.ValidateSource(data);
                                break;
                            default:
                                throw new Exception("Impossibile");
                        }
                        if (responseData == null)
                        {
                            throw new Exception("Impossibile");
                        }

                        var ext = Path.GetExtension(fileName).ToLower();
                        if (ext == ".html")
                        {
                            responseData.writeResultsOnHtmlFile(fileName, url);
                        }
                        else
                        {
                            //fileName = string.Format("{0}.txt", Path.GetFileNameWithoutExtension(fileName));
                            responseData.writeResultsOnFile(fileName, url);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format("Problema durante l'elaborazione di {0}. Dettagli : {1}", url, ex.Message));
                    }
                    i++;
                    backgroundWorker1.ReportProgress(i);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (backgroundWorker1.IsBusy && backgroundWorker1.WorkerSupportsCancellation)
            {
                backgroundWorker1.CancelAsync();
            }
        }

        //validazione W3C
        private void button1_Click(object sender, EventArgs e)
        {
            ExecuteControl(Operazione.VALIDAZIONE);
        }

        private void ExecuteControl(Operazione tipoOperazione)
        {
            if (UrlDaTestare.Count <= 0)
            {
                MessageBox.Show("Nessun URL selezionato");
                return;
            }

            var lastFile = tipoOperazione == Operazione.ACCESSIBILITA ? Settings.Default.AccessFileName : Settings.Default.W3cFileName;


            using (var dlg = new SaveFileDialog())
            {
                var lastDir = Settings.Default.LastDir;

                dlg.InitialDirectory = lastDir;
                dlg.Title = "Save result in ";
                dlg.Filter = "txt files (*.txt)|*.txt|html files (*.html)|*.html";
                var date = DateTime.Now;
                //var name = string.Format("{0}_{1:dd-MM-yyyy_hh-mm-ss}.txt", Settings.Default.AccessFileName, date);
                var name = string.Format("{0}.txt", lastFile, date);

                dlg.FileName = name;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    Settings.Default.LastDir = Path.GetDirectoryName(dlg.FileName);
                    //Settings.Default.AccessFileName = Path.GetFileNameWithoutExtension(dlg.FileName).Replace(string.Format("_{0:dd-MM-yyyy_hh-mm-ss}", date), "");

                    switch (tipoOperazione)
                    {
                        case Operazione.ACCESSIBILITA:
                            Settings.Default.AccessFileName = Path.GetFileNameWithoutExtension(dlg.FileName);
                            break;
                        case Operazione.VALIDAZIONE:
                            Settings.Default.W3cFileName = Path.GetFileNameWithoutExtension(dlg.FileName);
                            break;
                        default:
                            throw new Exception("Impossibile");
                    }

                    Settings.Default.Save();
                    if (File.Exists(dlg.FileName))
                    {
                        File.Delete(dlg.FileName);
                    }

                    InitBGWorkerStart();
                    //bgWorker do work
                    backgroundWorker1.RunWorkerAsync(new object[] { dlg.FileName, tipoOperazione });
                }
            }
        }

    }
}
