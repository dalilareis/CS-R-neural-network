using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FeatureExtractor
{
    public partial class Form1 : Form
    {
        DataTable features = Program.CreateTable();
        List<int> CD = new List<int>();
        List<int> TBC = new List<int>();
        List<int> DC = new List<int>();
        List<double> MV = new List<double>();
        List<double> MA = new List<double>();
        List<double> DBC = new List<double>();
        List<double> ED = new List<double>();
        List<double> AED = new List<double>();
        List<double> DMSL = new List<double>();
        List<double> ADMSL = new List<double>();
        List<double> ASA = new List<double>();
        List<double> SSA = new List<double>();
        Stopwatch timer = new Stopwatch();
        List<KeyValuePair<string, double>> notas = Program.ReadCSV("notas.csv");

        public Form1()
        {
            InitializeComponent();
        }

        private void buttonLoad_Click_1(object sender, EventArgs e)
        {
            LoadNewFile();
        }

        private void buttonSave_Click_1(object sender, EventArgs e)
        {
            SaveFile();
        }

        private void buttonProcess_Click_1(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(userSelectedFiles))
            {
                timer.Start();
                using (Loading frm = new Loading(Process))
                {
                    frm.ShowDialog(this);
                }
                timer.Stop();
                featuresDataView.DataSource = features;
                MessageBox.Show("Features extracted in " + Math.Round(timer.Elapsed.TotalMinutes, 2) + " minutes.", "Finished successfully!", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                MessageBox.Show("You need to load files before extracting features!",
                      "Button Crazy Alert...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        } 

        private void buttonOK_Click_1(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(userSaveFile))
            {
                try
                {
                    if (features != null && features.Rows.Count > 0)
                    {
                        timer.Start();
                        using (StreamWriter writer = new StreamWriter(userSaveFile))
                        {
                            Program.Write2CSV(features, writer, true);
                            timer.Stop();
                            MessageBox.Show("Features saved to file!", "My work here is done!", MessageBoxButtons.OK, MessageBoxIcon.Information);                           
                        }
                    }                           
                    else
                        MessageBox.Show("Please, press the Extract Features Button before saving.",
                                "No features extracted!", MessageBoxButtons.OK, MessageBoxIcon.Warning);                        
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not save to file: " + ex.Message, "Computer says noooo...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
                MessageBox.Show("You need to select a location to save the file!",
                    "Button Crazy Alert", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Exit application?", "Close", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void openFileDialog_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }

        public string userSelectedFiles
        {
            get
            {
                return textFilePath.Text;
            }
            set
            {
                textFilePath.Text = value;
            }
        }

        public string userSaveFile
        {
            get
            {
                return textSaveFile.Text;
            }
            set
            {
                textSaveFile.Text = value;
            }
        }

        private void LoadNewFile()
        {
            System.Windows.Forms.DialogResult dr = openFileDialog.ShowDialog();
            if (dr == DialogResult.OK)
            {
                try
                {
                    foreach (string fileName in openFileDialog.FileNames)
                    {
                        userSelectedFiles += fileName + Environment.NewLine;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not read file from disk. Original error: " + ex.Message, "Computer says nooooo...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }      

        private void SaveFile()
        {
            System.Windows.Forms.DialogResult dr2 = saveFileDialog.ShowDialog();
            if (dr2 == DialogResult.OK)
            {
                userSaveFile = saveFileDialog.FileName;
            }
        }

        private void textFilePath_TextChanged(object sender, EventArgs e)
        {
            userSelectedFiles = textFilePath.Text;
        }

        private void textSaveFile_TextChanged(object sender, EventArgs e)
        {
            userSaveFile = textSaveFile.Text;
        }

        private void Process()
        {
            List<List<MouseEvents>> fullData = Program.ReadFiles(userSelectedFiles);
            foreach (List<MouseEvents> file in fullData)
            {
                List<int> CD = Program.ClickDuration(file);
                List<int> DC = Program.DoubleClick(file);
                List<int> TBC = Program.TimeBtwClicks(file);                
                List<double> MV = Program.Velocity(file);
                List<double> MA = Program.Acceleration(file);
                List<double> DBC = Program.DistBtwClicks(file);
                List<double> ED = Program.ExcessDist(file);
                List<double> AED = Program.AvgED(file);
                List<double> DMSL = Program.DistStraightLine(file);
                List<double> ADMSL = Program.AvgDistSL(file);
                List<double> ASA = Program.SumAnglesAbs(file);
                List<double> SSA = Program.SumAnglesSign(file);

                if (!CD.Any())
                    MessageBox.Show("Please check data format. Event type should be MDL (Mouse_Down_Left) and MUR (Mouse_Up_Right).",
                        "Mouse Click Events not found!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                {
                  // Includes only 14 metrics:
                    double grade = notas.FirstOrDefault(kvp => kvp.Key == file[0].getID()).Value;
                    features.Rows.Add(file[0].getID(), CD.Count, Program.Avg_Int(CD), DC.Count, Program.Avg_Int(DC), Program.Avg_Int(TBC), Program.Avg_Doub(MV), 
                        Program.Avg_Doub(MA), Program.Avg_Doub(DBC), Program.Avg_Doub(ED), Program.Avg_Doub(AED), Program.Avg_Doub(DMSL), 
                        Program.Avg_Doub(ADMSL), Program.Avg_Doub(ASA), Program.Avg_Doub(SSA), grade);
                   
              // To include all metrics, use this instead and uncomment corresponding columns in Program.CreateTable()

                    /*features.Rows.Add(CD.Count, Program.Avg_Int(CD), Program.StdD_Int(CD), DC.Count, Program.Avg_Int(DC), Program.StdD_Int(DC),
                        Program.Avg_Int(TBC), Program.StdD_Int(TBC), Program.Avg_Doub(MV), Program.StdD_Doub(MV), Program.Avg_Doub(MA), Program.StdD_Doub(MA),
                        Program.Avg_Doub(DBC), Program.StdD_Doub(DBC), Program.Avg_Doub(ED), Program.StdD_Doub(ED), Program.Avg_Doub(AED), Program.StdD_Doub(AED),
                        Program.Avg_Doub(DMSL), Program.StdD_Doub(DMSL), Program.Avg_Doub(ADMSL), Program.StdD_Doub(ADMSL), Program.Avg_Doub(ASA),
                        Program.StdD_Doub(ASA), Program.Avg_Doub(SSA), Program.StdD_Doub(SSA), grade);*/
                }
            }            
        }

        private void DelButton_Click(object sender, EventArgs e)
        {
            if (features != null)
                features.Clear(); 
        }
    }
}

