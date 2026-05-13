using LIS.BusinessLogic.Helper;
using LIS.Com.Businesslogic;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarcodePrint
{
    public partial class Home : Form
    {
        CheckBox headerCheckBox = new CheckBox();
        public Home()
        {
            InitializeComponent();
            string serverURL = ConfigurationManager.AppSettings["ServerURL"];
            string apiKey = ConfigurationManager.AppSettings["ApiKey"];
            BarcodePrintCommand.LisDOM.InitAPI(serverURL, apiKey);
            LoadData();

            //Add a CheckBox Column to the DataGridView Header Cell.

            //Find the Location of Header Cell.
            Point headerCellLocation = this.dataGridView1.GetCellDisplayRectangle(0, -1, true).Location;
         
            //Place the Header CheckBox in the Location of the Header Cell.
            headerCheckBox.Location = new Point(headerCellLocation.X + 8, headerCellLocation.Y + 2);
            headerCheckBox.BackColor = Color.White;
            headerCheckBox.Size = new Size(18, 18);

            //Assign Click event to the Header CheckBox.
            headerCheckBox.Click += new EventHandler(HeaderCheckBox_Clicked);
            dataGridView1.Controls.Add(headerCheckBox);

            //Assign Click event to the DataGridView Cell.
            dataGridView1.CellContentClick += new DataGridViewCellEventHandler(DataGridView_CellClick);
        }
        
        private async Task LoadData()
        {
            var testlist = await BarcodePrintCommand.LisDOM.GetAllNewSampleDetails();

            var list = new List<BarCode>();
            foreach (var item in testlist)
            {
                BarCode br = new BarCode
                {
                    BarcodeNo = item.SampleNo,
                    PatientName = item.Patient.Name,
                    TestName = item.HISTestName,
                    CollectionDate = item.SampleCollectionDate,
                    BedNo = item.BedNo,
                    IPNo = item.IPNo,
                    LabNo = item.HISRequestNo,
                    GroupName = Helper.GetGroupName(item.SampleNo)
                };
                list.Add(br);
            }
            dataGridView1.DataSource = list;
        }

        private void HeaderCheckBox_Clicked(object sender, EventArgs e)
        {
            //Necessary to end the edit mode of the Cell.
            dataGridView1.EndEdit();

            //Loop and check and uncheck all row CheckBoxes based on Header Cell CheckBox.
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                DataGridViewCheckBoxCell checkBox = (row.Cells["IsPrint"] as DataGridViewCheckBoxCell);
                checkBox.Value = headerCheckBox.Checked;
            }
        }

        private void DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //Check to ensure that the row CheckBox is clicked.
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                //Loop to verify whether all row CheckBoxes are checked or not.
                bool isChecked = true;
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (Convert.ToBoolean(row.Cells["IsPrint"].EditedFormattedValue) == false)
                    {
                        isChecked = false;
                        break;
                    }
                }
                headerCheckBox.Checked = isChecked;
            }
        }

        private void QuitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void AboutUsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var about = new AboutUs();
            about.ShowDialog(this);
        }

        void MenuQuit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Home_Load(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //string p = Application.StartupPath.ToString() + @"\LabBarcode.prn";// read prn file
                //string barcodeString = string.Empty;
                //FileStream fs = new FileStream(p, FileMode.Open, FileAccess.Read);
                //StreamReader r = new StreamReader(fs);
                //r.BaseStream.Seek(0, SeekOrigin.Begin);
                //barcodeString = r.ReadToEnd().ToString();
                //r.Close();
                //fs.Close();

                //barcodeString = barcodeString.Replace("<SAMPLENO>", "1506391");
                //barcodeString = barcodeString.Replace("<PATIENTNAME>", "Test Kumar");
                //barcodeString = barcodeString.Replace("<GROUPNAME>", "SERUM");
                //barcodeString = barcodeString.Replace("<BEDNO>", "208");
                //barcodeString = barcodeString.Replace("<COLLECTIONDATE>", "11 Feb 2024");
                //barcodeString = barcodeString.Replace("<PATIENTNO>", "12578");
                //string fn = Application.StartupPath.ToString() + @"\Data\LabBarcode.prn";
                //FileInfo fl = new FileInfo(fn);
                //if (fl.Exists == true)
                //{
                //    fl.Delete();
                //}
                //fs = new FileStream(fn, FileMode.OpenOrCreate, FileAccess.Write);
                //StreamWriter w1 = new StreamWriter(fs);
                //w1.WriteLine(barcodeString);
                //w1.Close();
                //fs.Close();

                ////Print Bracode
                //string prnFile = Application.StartupPath.ToString() + @"\Data\LabBarcode.prn";

                //string batchfilename = "lisprint_" + ".bat";
                //string batchpath = Application.StartupPath + @"\Data\" + batchfilename;
                //FileInfo fi = new FileInfo(batchpath);
                //File.Create(batchpath).Dispose();
                //PrintBarCode(batchpath);
            }
            catch (Exception ex)
            {
                Logger.LogInstance.LogError(ex.Message);
            }
        }

        private void btnPrintBarcode_Click(object sender, EventArgs e)
        {
            var barcodePrintList = new List<BarCode>();
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                bool isSelected = Convert.ToBoolean(row.Cells["IsPrint"].Value);
                if (isSelected)
                {
                    var item = barcodePrintList.Find(p => p.BarcodeNo == row.Cells["BarcodeNo"].Value.ToString());
                    if (item == null)
                    {
                        BarCode br = new BarCode
                        {
                            BarcodeNo = row.Cells["BarcodeNo"].Value.ToString(),
                            PatientName = row.Cells["PatientName"].Value == null ? "" : row.Cells["PatientName"].Value.ToString(),
                            CollectionDate = Convert.ToDateTime(row.Cells["CollectionDate"].Value),
                            TestName = row.Cells["TestName"].Value == null ? "" : row.Cells["TestName"].Value.ToString(),
                            BedNo = row.Cells["BedNo"].Value == null ? "" : row.Cells["BedNo"].Value.ToString(),
                            IPNo = row.Cells["IPNo"].Value == null ? "" : row.Cells["IPNo"].Value.ToString(),
                            LabNo = row.Cells["LabNo"].Value.ToString(),
                            GroupName = Helper.GetGroupName(row.Cells["BarcodeNo"].Value.ToString())
                        };

                        barcodePrintList.Add(br);
                    }
                }
            }

            if (barcodePrintList.Count > 0)
            {
                GenerateBarcodePRN(barcodePrintList);
            }
        }

        private void PrintBarCode(string prnfilename)
        {
            try
            {
                Logger.LogInstance.LogInfo(prnfilename);
                string printerName = ConfigurationManager.AppSettings["PrinterName"];
                string args = $"{prnfilename} {printerName}";
                string commandPath = string.Format(@"{0}\lisprint.bat", Application.StartupPath);

                ProcessStartInfo startinfo = new ProcessStartInfo(commandPath, args);
                startinfo.FileName = commandPath;
                startinfo.CreateNoWindow = true;
                startinfo.UseShellExecute = false;
                startinfo.Arguments = $"{prnfilename} {printerName}";
                startinfo.WorkingDirectory = Application.StartupPath + @"\Data";
                Logger.LogInstance.LogInfo(startinfo.WorkingDirectory);
                Process.Start(startinfo).WaitForExit();

            }
            catch (Exception ex)
            {
                Logger.LogInstance.LogError(ex.Message);
            }
        }


        private void GenerateBarcodePRN(List<BarCode> barcodePrintList)
        {
            try
            { 
                foreach (var item in barcodePrintList)
                {
                    // Read Master PRN File
                    string p = Application.StartupPath.ToString() + @"\LabBarcode.prn";
                    string barcodeString = string.Empty;
                    FileStream fs = new FileStream(p, FileMode.Open, FileAccess.Read);
                    StreamReader r = new StreamReader(fs);
                    r.BaseStream.Seek(0, SeekOrigin.Begin);
                    barcodeString = r.ReadToEnd().ToString();
                    r.Close();
                    fs.Close();

                    barcodeString = barcodeString.Replace("<SAMPLENO>", item.BarcodeNo);
                    barcodeString = barcodeString.Replace("<PATIENTNAME>", item.PatientName);
                    barcodeString = barcodeString.Replace("<GROUPNAME>", item.GroupName);
                    barcodeString = barcodeString.Replace("<BEDNO>", item.BedNo);
                    barcodeString = barcodeString.Replace("<COLLECTIONDATE>", item.CollectionDate.ToString("dd MMM yyyy"));
                    barcodeString = barcodeString.Replace("<PATIENTNO>", item.IPNo);

                    string filename = item.BarcodeNo + DateTime.Now.Ticks + ".prn";
                    string fn = Application.StartupPath.ToString() + @"\Data\" + filename;
                    FileInfo fl = new FileInfo(fn);
                    if (fl.Exists == true)
                    {
                        fl.Delete();
                    }
                    fs = new FileStream(fn, FileMode.OpenOrCreate, FileAccess.Write);

                    StreamWriter w1 = new StreamWriter(fs);
                    w1.WriteLine(barcodeString);
                    w1.Close();
                    fs.Close();

                    PrintBarCode(filename);                    
                }
               
            }
            catch (Exception ex)
            {
                Logger.LogInstance.LogError(ex.Message);
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            SearhData();
        }

        private async Task SearhData()
        {
            if (!string.IsNullOrEmpty(txtSearch.Text))
            {
                string sampleNo = txtSearch.Text.Trim();
                string serverURL = ConfigurationManager.AppSettings["ServerURL"];
                string apiKey = ConfigurationManager.AppSettings["ApiKey"];
                BarcodePrintCommand.LisDOM.InitAPI(serverURL, apiKey);
                var testlist = await BarcodePrintCommand.LisDOM.GetSampleDetails(sampleNo);

                var list = new List<BarCode>();
                foreach (var item in testlist)
                {
                    BarCode br = new BarCode
                    {
                        BarcodeNo = item.SampleNo,
                        PatientName = item.Patient.Name,
                        TestName = item.HISTestName,
                        CollectionDate = item.SampleCollectionDate,
                        BedNo = item.BedNo,
                        IPNo = item.IPNo,
                        LabNo = item.HISRequestNo,
                        GroupName = Helper.GetGroupName(item.SampleNo)
                    };
                    list.Add(br);
                }
                dataGridView1.DataSource = list;
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            string serverURL = ConfigurationManager.AppSettings["ServerURL"];
            string apiKey = ConfigurationManager.AppSettings["ApiKey"];
            BarcodePrintCommand.LisDOM.InitAPI(serverURL, apiKey);
            LoadData();
        }
    }
}
