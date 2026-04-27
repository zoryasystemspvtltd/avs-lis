using LIS.Logger;

namespace Test.COM
{
    partial class Home
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
            Logger.LogInstance.LogInfo("COM Simulator Stopped.");
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
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.rdbSerialPort = new System.Windows.Forms.RadioButton();
            this.rdbTCPIP = new System.Windows.Forms.RadioButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.txtPortNo = new System.Windows.Forms.TextBox();
            this.txtIPAddress = new System.Windows.Forms.TextBox();
            this.lblPortNo = new System.Windows.Forms.Label();
            this.lblIPAddress = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9",
            "COM10"});
            this.comboBox1.Location = new System.Drawing.Point(3, 41);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(121, 21);
            this.comboBox1.TabIndex = 0;
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(607, 28);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(75, 23);
            this.btnConnect.TabIndex = 1;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(3, 68);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(785, 58);
            this.textBox1.TabIndex = 2;
            // 
            // btnSend
            // 
            this.btnSend.Enabled = false;
            this.btnSend.Location = new System.Drawing.Point(688, 26);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(75, 23);
            this.btnSend.TabIndex = 3;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // txtLog
            // 
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Location = new System.Drawing.Point(0, 0);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(800, 317);
            this.txtLog.TabIndex = 4;
            // 
            // rdbSerialPort
            // 
            this.rdbSerialPort.AutoSize = true;
            this.rdbSerialPort.Checked = true;
            this.rdbSerialPort.Location = new System.Drawing.Point(12, 5);
            this.rdbSerialPort.Name = "rdbSerialPort";
            this.rdbSerialPort.Size = new System.Drawing.Size(73, 17);
            this.rdbSerialPort.TabIndex = 7;
            this.rdbSerialPort.TabStop = true;
            this.rdbSerialPort.Text = "Serial Port";
            this.rdbSerialPort.UseVisualStyleBackColor = true;
            this.rdbSerialPort.CheckedChanged += new System.EventHandler(this.rdbSerialPort_CheckedChanged);
            // 
            // rdbTCPIP
            // 
            this.rdbTCPIP.AutoSize = true;
            this.rdbTCPIP.Location = new System.Drawing.Point(91, 5);
            this.rdbTCPIP.Name = "rdbTCPIP";
            this.rdbTCPIP.Size = new System.Drawing.Size(61, 17);
            this.rdbTCPIP.TabIndex = 8;
            this.rdbTCPIP.TabStop = true;
            this.rdbTCPIP.Text = "TCP/IP";
            this.rdbTCPIP.UseVisualStyleBackColor = true;
            this.rdbTCPIP.CheckedChanged += new System.EventHandler(this.rdbTCPIP_CheckedChanged);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.txtPortNo);
            this.splitContainer1.Panel1.Controls.Add(this.txtIPAddress);
            this.splitContainer1.Panel1.Controls.Add(this.lblPortNo);
            this.splitContainer1.Panel1.Controls.Add(this.lblIPAddress);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            this.splitContainer1.Panel1.Controls.Add(this.comboBox1);
            this.splitContainer1.Panel1.Controls.Add(this.btnConnect);
            this.splitContainer1.Panel1.Controls.Add(this.textBox1);
            this.splitContainer1.Panel1.Controls.Add(this.rdbTCPIP);
            this.splitContainer1.Panel1.Controls.Add(this.btnSend);
            this.splitContainer1.Panel1.Controls.Add(this.rdbSerialPort);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.txtLog);
            this.splitContainer1.Size = new System.Drawing.Size(800, 450);
            this.splitContainer1.SplitterDistance = 129;
            this.splitContainer1.TabIndex = 11;
            // 
            // txtPortNo
            // 
            this.txtPortNo.Location = new System.Drawing.Point(369, 42);
            this.txtPortNo.Name = "txtPortNo";
            this.txtPortNo.Size = new System.Drawing.Size(100, 20);
            this.txtPortNo.TabIndex = 13;
            this.txtPortNo.Text = "1234";
            // 
            // txtIPAddress
            // 
            this.txtIPAddress.Location = new System.Drawing.Point(155, 41);
            this.txtIPAddress.Name = "txtIPAddress";
            this.txtIPAddress.Size = new System.Drawing.Size(143, 20);
            this.txtIPAddress.TabIndex = 12;
            this.txtIPAddress.Text = "10.228.175.184";
            // 
            // lblPortNo
            // 
            this.lblPortNo.AutoSize = true;
            this.lblPortNo.Location = new System.Drawing.Point(366, 26);
            this.lblPortNo.Name = "lblPortNo";
            this.lblPortNo.Size = new System.Drawing.Size(43, 13);
            this.lblPortNo.TabIndex = 11;
            this.lblPortNo.Text = "Port No";
            // 
            // lblIPAddress
            // 
            this.lblIPAddress.AutoSize = true;
            this.lblIPAddress.Location = new System.Drawing.Point(152, 25);
            this.lblIPAddress.Name = "lblIPAddress";
            this.lblIPAddress.Size = new System.Drawing.Size(58, 13);
            this.lblIPAddress.TabIndex = 10;
            this.lblIPAddress.Text = "IP Address";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Com Port";
            // 
            // Home
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.splitContainer1);
            this.Name = "Home";
            this.Text = "COM Simulator";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Home_FormClosed);
            this.Load += new System.EventHandler(this.Home_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.RadioButton rdbSerialPort;
        private System.Windows.Forms.RadioButton rdbTCPIP;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TextBox txtPortNo;
        private System.Windows.Forms.TextBox txtIPAddress;
        private System.Windows.Forms.Label lblPortNo;
        private System.Windows.Forms.Label lblIPAddress;
        private System.Windows.Forms.Label label1;
    }
}

