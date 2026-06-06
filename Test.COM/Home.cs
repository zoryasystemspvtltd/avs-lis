using LIS.Logger;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Test.COM
{
    public partial class Home : Form
    {
        private SerialPort port;
        private TcpClient client;
        private Thread clientThread;
        private readonly Random _random = new Random(1024);
        public Home()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (rdbSerialPort.Checked)
                {
                    port = new SerialPort(comboBox1.Text)
                    {
                        BaudRate = 9600,
                        DataBits = 8,
                        StopBits = (StopBits)1,
                        Parity = (Parity)0
                    };
                    port.Open();
                    port.DataReceived += Port_DataReceived;
                    port.DiscardOutBuffer();
                    port.DiscardInBuffer();
                    this.Invoke((MethodInvoker)delegate
                    {
                        Logger.LogInstance.LogInfo("PORT Connected.");
                    });
                }
                else
                {
                    client = new TcpClient();
                    client.ConnectAsync(txtIPAddress.Text, Convert.ToInt32(txtPortNo.Text));
                    Logger.LogInstance.LogInfo("TCP/IP Connected.");
                }
                btnSend.Enabled = true;
            }
            catch (Exception ex)
            {
                client.Close();
                throw ex;
            }
        }
        private async Task TCP_HL7SendData()
        {
            try
            {
                if (client == null || !client.Connected)
                {
                    client = new TcpClient();
                    await client.ConnectAsync(txtIPAddress.Text, Convert.ToInt32(txtPortNo.Text));
                }

                NetworkStream stream = client.GetStream();
                string msg = textBox1.Text;
                string writemsg = ReplaceSpecialCharecter(msg, false);
                byte[] writeData = Encoding.ASCII.GetBytes(writemsg);
                await stream.WriteAsync(writeData, 0, writeData.Length);
                Logger.LogInstance.LogInfo("Write :" + msg);
                while (true)
                {
                    var data = new byte[10240];
                    int bytesRead = await stream.ReadAsync(data, 0, data.Length);

                    if (bytesRead > 0)
                    {
                        string response = Encoding.ASCII.GetString(data, 0, bytesRead);
                        Logger.LogInstance.LogInfo("Read :" + response);
                    }
                }
            }

            catch (ArgumentNullException ane)
            {
                Logger.LogInstance.LogError("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                Logger.LogInstance.LogError("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Logger.LogInstance.LogError("Unexpected exception : {0}", e.ToString());
            }
            finally
            {
                if (client?.Connected == true)
                {
                    client.Close();
                }
            }

        }

        private async Task TCP_ASTMSendData()
        {
            try
            {
                if (client == null || !client.Connected)
                {
                    client = new TcpClient();
                    await client.ConnectAsync(txtIPAddress.Text, Convert.ToInt32(txtPortNo.Text));
                }

                NetworkStream stream = client.GetStream();
                string msg = textBox1.Text;
                string writemsg = ReplaceSpecialCharecter(msg, false);
                byte[] writeData = Encoding.ASCII.GetBytes(writemsg);
                await stream.WriteAsync(writeData, 0, writeData.Length);
                //string rawmsg = ReplaceSpecialCharecter(writemsg, true);
                Logger.LogInstance.LogInfo("Write :" + msg);
                while (true)
                {
                    var data = new byte[1024];
                    int bytesRead = await stream.ReadAsync(data, 0, data.Length);

                    if (bytesRead > 0)
                    {
                        string response = Encoding.ASCII.GetString(data, 0, bytesRead);
                        Logger.LogInstance.LogInfo("Read :" + response);
                    }
                }
            }

            catch (ArgumentNullException ane)
            {
                Logger.LogInstance.LogError("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                Logger.LogInstance.LogError("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Logger.LogInstance.LogError("Unexpected exception : {0}", e.ToString());
            }
            finally
            {
                if (client?.Connected == true)
                {
                    client.Close();
                }
            }

        }
        /// <summary>
        /// ENQ or (char)5 -enquiry
        /// ACK or (char)6 -acknowledge
        /// STX or (char)2- start of text
        /// ETX or (char)3- end of text
        /// ETB or (char)17- end of text
        /// EOT or (char)4 - end of transmission
        /// NAK or (char)21 - negative acknowledge
        /// DLE or (char)10 - data link escape 
        /// CR	carriage return
        /// </summary>
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var text = port.ReadExisting().ToString();

            if (text == "" + (char)4)
            {
                text = "<EOT>";
            }
            else if (text == "" + (char)5)
            {
                text = "<ENQ>";
                if (rdbTCPIP.Checked)
                {
                    port.Write("" + (char)6);

                    this.Invoke((MethodInvoker)delegate
                    {
                        Logger.LogInstance.LogInfo("Write :<ACK>");
                    });
                }
            }
            else if (text == "" + (char)6)
            {
                text = "<ACK>";
            }
            else if (text == "" + (char)21)
            {
                text = "<NAK>";
            }
            else
            {
                if (rdbTCPIP.Checked)
                {
                    port.Write("" + (char)6);

                    this.Invoke((MethodInvoker)delegate
                    {
                        Logger.LogInstance.LogInfo("Write :<ACK>");
                    });
                }
            }

            this.Invoke((MethodInvoker)delegate
            {
                Logger.LogInstance.LogInfo("Read :" + text);
            });

        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (rdbSerialPort.Checked)
            {
                var response = ReplaceSpecialCharecter(textBox1.Text, false);
                port.Write(response);
                var text = ReplaceSpecialCharecter(response, false);
                Logger.LogInstance.LogInfo("Write :" + text);
            }
            else
            {
                //test test HL7 uncomment the below method
                //clientThread = new Thread(new ThreadStart(TCP_HL7SendData));
                clientThread = new Thread(new ThreadStart(RunTCPAsyncMethod));
                clientThread.Start();
            }
        }

        private void RunTCPAsyncMethod()
        {
            TCP_HL7SendData().GetAwaiter().GetResult();  // Blocking wait, not recommended
        }
        private string ReplaceSpecialCharecter(string text, bool reverse)
        {
            /*
                <ACK> acknowledge (ASCII Decimal 6).
                {Constants.vbCr} carriage return (ASCII decimal 13).
                <ENQ> enquiry (ASCII Decimal 5).
                <EOT> end of transmission (ASCII decimal 4).
                <ETB> end of transmission block (ASCII Decimal 23). For use only
                <ETX> end of text (ASCII Decimal 3). Required at the end of eachrecord.
                <LF> line feed (ASCII Decimal 10).
                <NAK> negative acknowledge (ASCII Decimal 21).
                <STX> start of text (ASCII Decimal 2).
             */

            Dictionary<string, char> specialCharecters = new Dictionary<string, char>();
            specialCharecters.Add("<ACK>", (char)6);
            specialCharecters.Add("<VT>", (char)11);
            specialCharecters.Add("<FS>", (char)28);
            specialCharecters.Add("<CR>", (char)13);
            specialCharecters.Add("<ENQ>", (char)5);
            specialCharecters.Add("<EOT>", (char)4);
            specialCharecters.Add("<ETB>", (char)23);
            specialCharecters.Add("<ETX>", (char)3);
            specialCharecters.Add("<LF>", (char)10);
            specialCharecters.Add("<NAK>", (char)21);
            specialCharecters.Add("<STX>", (char)2);

            if (!reverse)
            {
                foreach (var key in specialCharecters.Keys)
                {
                    string oldValue = key;
                    string newValue = "" + specialCharecters[key];

                    text = text.Replace(oldValue, newValue);
                }
            }
            else
            {
                foreach (var key in specialCharecters.Keys)
                {
                    string oldValue = "" + specialCharecters[key];
                    string newValue = key;

                    text = text.Replace(oldValue, newValue);
                }
            }
            return text;
        }

        private void Home_Load(object sender, EventArgs e)
        {
            Logger.LogInstance.LogInfo("COM Simulator Started.");
        }

        private void rdbSerialPort_CheckedChanged(object sender, EventArgs e)
        {
            lblIPAddress.Visible = false;
            lblPortNo.Visible = false;
            txtIPAddress.Visible = false;
            txtPortNo.Visible = false;
            label1.Visible = true;
            comboBox1.Visible = true;
           
        }

        private void rdbTCPIP_CheckedChanged(object sender, EventArgs e)
        {
            lblIPAddress.Visible = true;
            lblPortNo.Visible = true;
            txtIPAddress.Visible = true;
            txtPortNo.Visible = true;
            label1.Visible = false;
            comboBox1.Visible = false;
        }

        private void Home_FormClosed(object sender, FormClosedEventArgs e)
        {
            clientThread?.Abort();
            client?.Close();
        }
    }
}
