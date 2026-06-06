using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using LIS.Com.Businesslogic;
using LIS.DtoModel;

namespace LisTCPIPConsole.controlls
{
    public partial class ExecuteCOM : UserControl
    {
        List<SampleItem> results;
        SampleItem processingItem;

        public ExecuteCOM()
        {
            InitializeComponent();
            
            InitProcessing();
        }

        private void InitProcessing()
        {
            results = new List<SampleItem>();

            if (LisContext.LisDOM.IsCommandReady)
            {
                LisContext.LisDOM.Command.OnProcessExecuted += Command_OnProcessCompleted;
            }
        }

        private void Command_OnProcessCompleted(object sender, SerialEventArgs e)
        {
            var item = results.First(p => p.SAMPLE.Equals(e.SampleName));
            item.STATUS = "SENT";

            processingItem = results.FirstOrDefault(p => p.STATUS.Equals("NEW"));
            if (processingItem != null)
            {
                LisContext.LisDOM.Command.StartSendToEquipment(processingItem);
            }

        }

        private void ExecuteCOM_Load(object sender, EventArgs e)
        {
           
        }
    }
}
