using LisTCPIPConsole.Properties;
using LIS.Com.Businesslogic;
using LIS.DtoModel;
using Microsoft.Win32;
using System;
using System.Windows.Forms;

namespace LisTCPIPConsole
{
    public partial class PortSetting : Form
    {
        // The path to the key where Windows looks for startup applications
        RegistryKey startupApp = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        bool isValidAPI = false;
        public PortSetting()
        {
            InitializeComponent();
        }

        private void LISSettings_Load(object sender, EventArgs e)
        {
            cbAutoConnect.Checked = Settings.Default.AUTO_CONNECT;
            cbRunOnStartup.Checked = Settings.Default.RUN_ON_STARTUP;
            txtServer.Text = Settings.Default.SERVER_URL;
            txtKey.Text = Settings.Default.API_KEY;
            txtServerIP.Text = Settings.Default.IP_ADDRESS;
            txtServerPort.Text = Settings.Default.PORT_NO.ToString();
            txtHeartbit.Text = Settings.Default.HEARTBIT_INTERVAL.ToString();
            cmbProtocol.SelectedItem = Settings.Default.PROTOCOL_NAME;
            ddlEquipmentType.DataSource = Enum.GetNames(typeof(EquipmentType));
            var selectedEquipment = (EquipmentType)Enum.Parse(typeof(EquipmentType), Settings.Default.EQUIPMENT_TYPE);
            foreach (string item in ddlEquipmentType.Items)
            {
                var enumItem = (EquipmentType)Enum.Parse(typeof(EquipmentType), item);
                if (enumItem == selectedEquipment)
                {
                    ddlEquipmentType.SelectedItem = item;
                    break;
                }
            }
        }

        private void bCancle_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void bSave_Click(object sender, EventArgs e)
        {
            //For local testing comment this code
            
            if (string.IsNullOrWhiteSpace(txtServer.Text))
            {
                MessageBox.Show(this, "Invalid value in Server URL", "Error !", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtKey.Text))
            {
                MessageBox.Show(this, "Invalid value in API Key", "Error !", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtServerIP.Text))
            {
                MessageBox.Show(this, "Invalid value in My AP address", "Error !", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtServerPort.Text))
            {
                MessageBox.Show(this, "Invalid value in My Port No", "Error !", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Settings.Default.IP_ADDRESS = txtServerIP.Text;
            Settings.Default.PORT_NO = Convert.ToInt32(txtServerPort.Text);
            Settings.Default.HEARTBIT_INTERVAL = Convert.ToInt32(txtHeartbit.Text);
            Settings.Default.PROTOCOL_NAME = (string)cmbProtocol.SelectedItem;
            Settings.Default.AUTO_CONNECT = cbAutoConnect.Checked;
            Settings.Default.SERVER_URL = txtServer.Text;
            Settings.Default.API_KEY = txtKey.Text;
            Settings.Default.EQUIPMENT_TYPE = (string)ddlEquipmentType.SelectedValue;
            if (cbRunOnStartup.Checked)
            {
                // Add the value in the registry so that the application runs at startup
                startupApp.SetValue("LisTCPIPConsole", Application.ExecutablePath);
                Settings.Default.RUN_ON_STARTUP = true;
            }
            else
            {
                // Remove the value from the registry so that the application doesn't start
                startupApp.DeleteValue("LisTCPIPConsole", false);
                Settings.Default.RUN_ON_STARTUP = false;
            }

            var settings = new TCPIPSettings();
            settings.AutoConnect = Settings.Default.AUTO_CONNECT;
            settings.IPAddress = Settings.Default.IP_ADDRESS;
            settings.PortNo = Settings.Default.PORT_NO;
            settings.HeartbitTimeout = Settings.Default.HEARTBIT_INTERVAL;
            settings.ProtocolName = Settings.Default.PROTOCOL_NAME;
            Settings.Default.Save();
            this.Close();
        }

        private void btnValidate_Click(object sender, EventArgs e)
        {
            LisContext.LisDOM.InitAPI(txtServer.Text, txtKey.Text);
            //isValidAPI = await LisContext.LisDOM.PingAPI();

            //if (!isValidAPI)
            //{
            //    MessageBox.Show(this, "Invalid API Details", "Error !", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return;
            //}

            lblStatus.Text = "Success...";
        }
    }
}