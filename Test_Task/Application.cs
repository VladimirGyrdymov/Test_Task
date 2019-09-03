using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.Sql;
using System.Data.SqlClient;
using System.IO;
using System.Threading;

namespace Test_Task
{
    public partial class Application : Form
    {
        public Application()
        {
            InitializeComponent();
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
        }

        string connectionString = @"Data Source = ВЛАДИМИР-ПК\SQLEXPRESS; Initial Catalog = FileProcess; Trusted_Connection = True";
        string fileName, request;
        int minCount = 0, maxCount;
        const int FIRST_ELEMENT = 0, CORRECTION_ONE = 1, CORRECTION_TWO = 2;

        private void openFileBtn_Click(object sender, EventArgs e)
        {
            openFileBtn.Enabled = false;

            fileChoice.ShowDialog();
            fileName = fileChoice.FileName;

            backgroundWorker1.RunWorkerAsync();
            label1.Text = "Процесс загрузки";

            dataGridView1.DataSource = null;
            stopDownload.Enabled = true;

            maxCount = System.IO.File.ReadAllLines(fileName).Length;
            progressBar1.Minimum = minCount;
            progressBar1.Maximum = maxCount - CORRECTION_ONE;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string[] headLineCut, cutLines;
            string[,] cutFileText;
            List<string> textLines = new List<string>();

            StreamReader sr = new StreamReader(fileName);
            while (!sr.EndOfStream)
            {
                if (backgroundWorker1.CancellationPending == true)
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    textLines.Add(sr.ReadLine());
                }
            }

            headLineCut = textLines[FIRST_ELEMENT].Split(';');
            textLines.RemoveAt(FIRST_ELEMENT);
            cutFileText = new string[textLines.Count, headLineCut.Length - CORRECTION_ONE];

            for (int i = 0; i < textLines.Count; i++)
            {
                cutLines = textLines[i].Split(';');
                for (int j = 0; j < cutLines.Length - CORRECTION_ONE; j++)
                {
                    cutFileText[i, j] = cutLines[j];
                }
            }

            request = "CREATE TABLE data (id int, ";
            for (int i = 0; i < headLineCut.Length - CORRECTION_ONE; i++)
            {
                request += '"' + headLineCut[i] + '"' + " nvarchar(MAX)" + ", ";
            }
            int lng = request.Length;
            request = request.Remove(lng - CORRECTION_TWO);

            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            SqlCommand cmdCreate = new SqlCommand(request + ")", conn);
            cmdCreate.ExecuteNonQuery();

            for (int i = 0; i < textLines.Count; i++)
            {
                if (backgroundWorker1.CancellationPending == true)
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    backgroundWorker1.ReportProgress(i + CORRECTION_ONE);

                    SqlCommand cmdInsert = new SqlCommand("INSERT INTO data (id) VALUES (@id)", conn);
                    cmdInsert.Parameters.AddWithValue("@id", i);
                    cmdInsert.ExecuteNonQuery();

                    for (int j = 0; j < headLineCut.Length - CORRECTION_ONE; j++)
                    {
                        SqlCommand cmdUpdate = new SqlCommand("UPDATE data SET " + '"' + headLineCut[j] + '"' + " = " + "'" + cutFileText[i, j] + "'" + "where id = " + i, conn);
                        cmdUpdate.ExecuteNonQuery();
                    }
                }
            }
            conn.Close();

            request = string.Empty;
            for (int i = 0; i < headLineCut.Length - CORRECTION_ONE; i++)
            {
                request += '"' + headLineCut[i] + '"' + ", ";
            }
            lng = request.Length;
            request = request.Remove(lng - CORRECTION_TWO);
        }

        private void stopDownload_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            if (e.Cancelled == true)
            {
                label1.Text = "Загрузка прервана";
                stopDownload.Enabled = false;
                openFileBtn.Enabled = true;
                progressBar1.Value = progressBar1.Minimum;
                SqlCommand cmd = new SqlCommand("DROP TABLE data", conn);
                cmd.ExecuteNonQuery();
            }
            else
            {
                label1.Text = "Загрузка успешно завершена";
                stopDownload.Enabled = false;
                openFileBtn.Enabled = true;
                SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT " + request + " FROM data", conn);
                DataTable dt = new DataTable();
                dataAdapter.Fill(dt);
                dataGridView1.DataSource = dt;
                SqlCommand cmd = new SqlCommand("DROP TABLE data", conn);
                cmd.ExecuteNonQuery();
            }

            conn.Close();
        }
    }
}
