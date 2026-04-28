using LIS.Com.Businesslogic;
using LIS.DtoModel;
using LIS.Logger;
using LisTCPIPConsole.Properties;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace LisTCPIPConsole
{
    public partial class Home : Form
    {
        string comPortName = ":: Status - Equipment not connected.";
        EquipmentType selectedEquipment;
        ToolStripMenuItem connectItem;
        HeartBeatProxy heartBeatProxy;
        bool IsReady = false;
        CancellationToken cancellationToken;
        public Home()
        {
            Logger.LogInstance.LogDebug("Lis Console Home method started");
            InitializeComponent();

            var settings = new TCPIPSettings
            {
                AutoConnect = Settings.Default.AUTO_CONNECT,
                IPAddress = Settings.Default.IP_ADDRESS,
                PortNo = Settings.Default.PORT_NO,
                ProtocolName = Settings.Default.PROTOCOL_NAME,
                HeartbitTimeout = Settings.Default.HEARTBIT_INTERVAL
            };
            selectedEquipment = (EquipmentType)Enum.Parse(typeof(EquipmentType), Settings.Default.EQUIPMENT_TYPE);
            LisContext.LisDOM.InitTCPIPCommand(settings, selectedEquipment);
            LisContext.LisDOM.InitAPI(Settings.Default.SERVER_URL, Settings.Default.API_KEY);
            TrayMenuContext();
            Logger.LogInstance.LogDebug($"Home Autoconnect {settings.AutoConnect}.");
            heartBeatProxy = new HeartBeatProxy();
            InitLIS();

            Logger.LogInstance.LogDebug("Lis Console Home method completed");
        }

        private void TrayMenuContext()
        {
            Logger.LogInstance.LogDebug("Lis Console TrayMenuContext method started");
            connectItem = new ToolStripMenuItem("Connect", null, this.MenuConnect_Click);
            Logger.LogInstance.LogDebug("Lis Console TrayMenuContext method completed");
        }

        private void InitLIS()
        {
            Logger.LogInstance.LogDebug("Lis Console InitLIS method started");
            var context = LisContext.LisDOM;
            var isReady = IsReady;

            var statusText = isReady ? "Disconnect" : "Connect";
            var statusMessage = isReady ? " :: Status - Equipment connected." : " :: Status - Equipment not connected.";

            ConnectToolStripMenuItem.Text = statusText;
            connectItem.Text = statusText;
            comPortName = statusMessage;

            heartBeatProxy.WatchHeartBeat();
            this.Text = $"{selectedEquipment} {comPortName}";
            Logger.LogInstance.LogDebug("Lis Console InitLIS method completed");
        }

        private void ConnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ConnectToolStripMenuItem.Text == "Connect")
            {
                ConnectTCPIP();
            }
            else
            {
                IsReady = false;
                InitLIS();
                DisconnectIP();
            }
        }

        private void ConnectTCPIP()
        {
            try
            {
                var context = LisContext.LisDOM;
                Logger.LogInstance.LogDebug("LisConsole ConnectTCPIP started.");
                if (Settings.Default.PROTOCOL_NAME == "HL7")
                {
                    IsReady = true;
                    this.InitLIS();
                    Logger.LogInstance.LogInfo($"{Settings.Default.IP_ADDRESS} IP Address connected.");
                    context.TcpIpHL7Command.StartListenerAsync();

                }
                else if (Settings.Default.PROTOCOL_NAME == "ASTM")
                {
                    IsReady = true;
                    this.InitLIS();
                    Logger.LogInstance.LogInfo($"{Settings.Default.IP_ADDRESS} IP Address connected.");
                    context.TcpIpASTMCommand.StartListenerAsync(cancellationToken);
                }

                Logger.LogInstance.LogDebug("LisConsole ConnectTCPIP completed.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error !", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void QuitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DisconnectIP();
            Application.Exit();
        }

        private void DisconnectIP()
        {
            if (Settings.Default.PROTOCOL_NAME == "ASTM")
            {
                IsReady = false;
                LisContext.LisDOM.TcpIpASTMCommand.DisconnectToTCPIPAsync();
            }
            else
            {
                IsReady = false;
                LisContext.LisDOM.TcpIpHL7Command.DisconnectToTCPIPAsync();
            }
        }

        private void AboutUsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var about = new AboutUs();
            about.ShowDialog(this);
        }

        private void PortSettingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var portsetting = new PortSetting();

            if (portsetting.ShowDialog(this) == DialogResult.OK)
            {
                //this.ConnectCOM();
            }
        }
        private void Home_SizeChanged(object sender, EventArgs e)
        {

        }

        private void AddUserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(Settings.Default.SERVER_URL);
        }

        private void Home_Resize(object sender, EventArgs e)
        {
            //if (FormWindowState.Minimized == this.WindowState)
            //{
            //    //lisNotifyIcon.Visible = true;
            //    //lisNotifyIcon.ShowBalloonTip(500);
            //    //this.executeCOM1.txtLog.Text = "";
            //    //this.Hide();
            //}
            //else if (FormWindowState.Normal == this.WindowState)
            //{
            //    lisNotifyIcon.Visible = false;
            //}
        }

        void MenuShow_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        void MenuConnect_Click(object sender, EventArgs e)
        {
            ConnectTCPIP();
        }

        void MenuQuit_Click(object sender, EventArgs e)
        {
            DisconnectIP();
            Application.Exit();
        }

        private void Home_Load(object sender, EventArgs e)
        {
            Logger.LogInstance.LogInfo(selectedEquipment + " Started.");
        }

        private void Home_FormClosed(object sender, FormClosedEventArgs e)
        {
            DisconnectIP();
            Application.Exit();
        }
    }
}