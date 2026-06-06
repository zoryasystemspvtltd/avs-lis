namespace LisTCPIPConsole
{
    partial class PortSetting
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PortSetting));
            this.bSave = new System.Windows.Forms.Button();
            this.bCancle = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.cmbProtocol = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.ddlEquipmentType = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.cbRunOnStartup = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.Label6 = new System.Windows.Forms.Label();
            this.cbAutoConnect = new System.Windows.Forms.CheckBox();
            this.pnlTCPIP = new System.Windows.Forms.Panel();
            this.txtServerPort = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.txtServerIP = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnValidate = new System.Windows.Forms.Button();
            this.txtKey = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtServer = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtHeartbit = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.pnlTCPIP.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            // 
            // bSave
            // 
            this.bSave.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bSave.Location = new System.Drawing.Point(83, 188);
            this.bSave.Margin = new System.Windows.Forms.Padding(4);
            this.bSave.Name = "bSave";
            this.bSave.Size = new System.Drawing.Size(112, 28);
            this.bSave.TabIndex = 22;
            this.bSave.Text = "SAVE";
            this.bSave.UseVisualStyleBackColor = true;
            this.bSave.Click += new System.EventHandler(this.bSave_Click);
            // 
            // bCancle
            // 
            this.bCancle.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bCancle.Location = new System.Drawing.Point(228, 188);
            this.bCancle.Margin = new System.Windows.Forms.Padding(4);
            this.bCancle.Name = "bCancle";
            this.bCancle.Size = new System.Drawing.Size(112, 28);
            this.bCancle.TabIndex = 21;
            this.bCancle.Text = "CANCEL";
            this.bCancle.UseVisualStyleBackColor = true;
            this.bCancle.Click += new System.EventHandler(this.bCancle_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(358, 169);
            this.tabControl1.TabIndex = 23;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage1.Controls.Add(this.cmbProtocol);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.ddlEquipmentType);
            this.tabPage1.Controls.Add(this.label10);
            this.tabPage1.Controls.Add(this.cbRunOnStartup);
            this.tabPage1.Controls.Add(this.label7);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(350, 143);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "General Settings";
            // 
            // cmbProtocol
            // 
            this.cmbProtocol.FormattingEnabled = true;
            this.cmbProtocol.Items.AddRange(new object[] {
            "ASTM",
            "HL7"});
            this.cmbProtocol.Location = new System.Drawing.Point(163, 84);
            this.cmbProtocol.Name = "cmbProtocol";
            this.cmbProtocol.Size = new System.Drawing.Size(121, 21);
            this.cmbProtocol.TabIndex = 29;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(23, 90);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(103, 13);
            this.label1.TabIndex = 28;
            this.label1.Text = "PROTOCOL NAME:";
            // 
            // ddlEquipmentType
            // 
            this.ddlEquipmentType.FormattingEnabled = true;
            this.ddlEquipmentType.Location = new System.Drawing.Point(164, 36);
            this.ddlEquipmentType.Name = "ddlEquipmentType";
            this.ddlEquipmentType.Size = new System.Drawing.Size(121, 21);
            this.ddlEquipmentType.TabIndex = 27;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(51, 36);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(74, 13);
            this.label10.TabIndex = 26;
            this.label10.Text = "EQUIPMENT:";
            // 
            // cbRunOnStartup
            // 
            this.cbRunOnStartup.AutoSize = true;
            this.cbRunOnStartup.Location = new System.Drawing.Point(164, 66);
            this.cbRunOnStartup.Margin = new System.Windows.Forms.Padding(4);
            this.cbRunOnStartup.Name = "cbRunOnStartup";
            this.cbRunOnStartup.Size = new System.Drawing.Size(15, 14);
            this.cbRunOnStartup.TabIndex = 25;
            this.cbRunOnStartup.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(20, 66);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(107, 13);
            this.label7.TabIndex = 24;
            this.label7.Text = "RUN ON STARTUP:";
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage2.Controls.Add(this.Label6);
            this.tabPage2.Controls.Add(this.cbAutoConnect);
            this.tabPage2.Controls.Add(this.pnlTCPIP);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(350, 143);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "PORT Settings";
            // 
            // Label6
            // 
            this.Label6.AutoSize = true;
            this.Label6.Location = new System.Drawing.Point(47, 108);
            this.Label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label6.Name = "Label6";
            this.Label6.Size = new System.Drawing.Size(95, 13);
            this.Label6.TabIndex = 58;
            this.Label6.Text = "AUTO CONNECT:";
            // 
            // cbAutoConnect
            // 
            this.cbAutoConnect.AutoSize = true;
            this.cbAutoConnect.Location = new System.Drawing.Point(150, 108);
            this.cbAutoConnect.Margin = new System.Windows.Forms.Padding(4);
            this.cbAutoConnect.Name = "cbAutoConnect";
            this.cbAutoConnect.Size = new System.Drawing.Size(15, 14);
            this.cbAutoConnect.TabIndex = 56;
            this.cbAutoConnect.UseVisualStyleBackColor = true;
            // 
            // pnlTCPIP
            // 
            this.pnlTCPIP.Controls.Add(this.txtHeartbit);
            this.pnlTCPIP.Controls.Add(this.label2);
            this.pnlTCPIP.Controls.Add(this.txtServerPort);
            this.pnlTCPIP.Controls.Add(this.label13);
            this.pnlTCPIP.Controls.Add(this.txtServerIP);
            this.pnlTCPIP.Controls.Add(this.label12);
            this.pnlTCPIP.Location = new System.Drawing.Point(8, 6);
            this.pnlTCPIP.Name = "pnlTCPIP";
            this.pnlTCPIP.Size = new System.Drawing.Size(316, 95);
            this.pnlTCPIP.TabIndex = 50;
            // 
            // txtServerPort
            // 
            this.txtServerPort.Location = new System.Drawing.Point(141, 36);
            this.txtServerPort.Margin = new System.Windows.Forms.Padding(4);
            this.txtServerPort.Name = "txtServerPort";
            this.txtServerPort.Size = new System.Drawing.Size(165, 20);
            this.txtServerPort.TabIndex = 49;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(30, 37);
            this.label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(106, 13);
            this.label13.TabIndex = 48;
            this.label13.Text = "SERVER PORT NO:";
            // 
            // txtServerIP
            // 
            this.txtServerIP.Location = new System.Drawing.Point(141, 4);
            this.txtServerIP.Margin = new System.Windows.Forms.Padding(4);
            this.txtServerIP.Name = "txtServerIP";
            this.txtServerIP.Size = new System.Drawing.Size(165, 20);
            this.txtServerIP.TabIndex = 47;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(16, 5);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(122, 13);
            this.label12.TabIndex = 46;
            this.label12.Text = "SERVER IP ADDRESS:";
            // 
            // tabPage3
            // 
            this.tabPage3.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage3.Controls.Add(this.lblStatus);
            this.tabPage3.Controls.Add(this.btnValidate);
            this.tabPage3.Controls.Add(this.txtKey);
            this.tabPage3.Controls.Add(this.label8);
            this.tabPage3.Controls.Add(this.txtServer);
            this.tabPage3.Controls.Add(this.label9);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(350, 143);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Server Settings";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(64, 111);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 13);
            this.lblStatus.TabIndex = 43;
            // 
            // btnValidate
            // 
            this.btnValidate.Location = new System.Drawing.Point(156, 106);
            this.btnValidate.Name = "btnValidate";
            this.btnValidate.Size = new System.Drawing.Size(75, 23);
            this.btnValidate.TabIndex = 42;
            this.btnValidate.Text = "Validate";
            this.btnValidate.UseVisualStyleBackColor = true;
            this.btnValidate.Click += new System.EventHandler(this.btnValidate_Click);
            // 
            // txtKey
            // 
            this.txtKey.Location = new System.Drawing.Point(156, 66);
            this.txtKey.Margin = new System.Windows.Forms.Padding(4);
            this.txtKey.Name = "txtKey";
            this.txtKey.Size = new System.Drawing.Size(165, 20);
            this.txtKey.TabIndex = 41;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(61, 70);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(48, 13);
            this.label8.TabIndex = 40;
            this.label8.Text = "API Key:";
            // 
            // txtServer
            // 
            this.txtServer.Location = new System.Drawing.Point(156, 21);
            this.txtServer.Margin = new System.Windows.Forms.Padding(4);
            this.txtServer.Name = "txtServer";
            this.txtServer.Size = new System.Drawing.Size(165, 20);
            this.txtServer.TabIndex = 39;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(48, 25);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(66, 13);
            this.label9.TabIndex = 38;
            this.label9.Text = "Server URL:";
            // 
            // txtHeartbit
            // 
            this.txtHeartbit.Location = new System.Drawing.Point(141, 67);
            this.txtHeartbit.Margin = new System.Windows.Forms.Padding(4);
            this.txtHeartbit.Name = "txtHeartbit";
            this.txtHeartbit.Size = new System.Drawing.Size(165, 20);
            this.txtHeartbit.TabIndex = 51;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(34, 68);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(101, 13);
            this.label2.TabIndex = 50;
            this.label2.Text = "Heartbit (Seconds) :";
            // 
            // PortSetting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(416, 240);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.bSave);
            this.Controls.Add(this.bCancle);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PortSetting";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Port Setting";
            this.Load += new System.EventHandler(this.LISSettings_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.pnlTCPIP.ResumeLayout(false);
            this.pnlTCPIP.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.Button bSave;
        internal System.Windows.Forms.Button bCancle;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        internal System.Windows.Forms.CheckBox cbRunOnStartup;
        internal System.Windows.Forms.Label label7;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        internal System.Windows.Forms.TextBox txtKey;
        internal System.Windows.Forms.Label label8;
        internal System.Windows.Forms.TextBox txtServer;
        internal System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button btnValidate;
        private System.Windows.Forms.ComboBox ddlEquipmentType;
        internal System.Windows.Forms.Label label10;
        private System.Windows.Forms.Panel pnlTCPIP;
        internal System.Windows.Forms.TextBox txtServerPort;
        internal System.Windows.Forms.Label label13;
        internal System.Windows.Forms.TextBox txtServerIP;
        internal System.Windows.Forms.Label label12;
        internal System.Windows.Forms.CheckBox cbAutoConnect;
        internal System.Windows.Forms.Label Label6;
        internal System.Windows.Forms.Label lblStatus;
        internal System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbProtocol;
        internal System.Windows.Forms.TextBox txtHeartbit;
        internal System.Windows.Forms.Label label2;
    }
}