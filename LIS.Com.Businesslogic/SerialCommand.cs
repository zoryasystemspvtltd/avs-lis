using LIS.DtoModel;
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.IO.Ports;
using System.Threading.Tasks;

namespace LIS.Com.Businesslogic
{
    public class SerialEventArgs : EventArgs
    {
        public string SampleName { get; set; }
    }

    public class SerialCommand
    {
        private PortSettings settings;

        public event EventHandler<SerialEventArgs> OnProcessExecuted;
        protected virtual void ProcessExecuted(SerialEventArgs e)
        {
            OnProcessExecuted?.Invoke(this, e);
        }

        protected SerialPort port;

        protected string[] data = new string[7];// This need to be 7 for equipment XN1000
        protected int index;
        protected string sInputMsg = "";
        public bool IsReady { get; private set; }

        public string Message { get; private set; }

        public bool IsRunning { get; private set; }

        public SerialCommand(PortSettings settings)
        {
            Logger.Logger.LogInstance.LogDebug("LIS.Com.Businesslogic SerialCommand Constructor method started.");
            IsReady = false;
            this.settings = settings;
            if (this.settings.AutoConnect)
            {
                ConnectToCOMPort();
            }
            Logger.Logger.LogInstance.LogDebug("LIS.Com.Businesslogic SerialCommand Constructor method completed.");
        }

        public void ConnectToCOMPort()
        {
            Logger.Logger.LogInstance.LogDebug("SerialCommand ConnectToCOMPort method started.");
            try
            {
                port = new SerialPort(settings.PortName)
                {
                    BaudRate = settings.BaudRate,
                    DataBits = settings.DataBits,
                    StopBits = (StopBits)settings.StopBits,
                    Parity = (Parity)settings.Parity
                };
                port.DataReceived += Port_DataReceived;
                if (!port.IsOpen)
                {
                    port.Open();
                }
                port.DiscardOutBuffer();
                port.DiscardInBuffer();
                IsReady = true;
                Logger.Logger.LogInstance.LogDebug("SerialCommand ConnectToCOMPort method completed.");
            }
            catch (Exception ex)
            {
                this.Message = ex.Message;
                Logger.Logger.LogInstance.LogException(ex);
            }
        }

        public void DisconnectToCOMPort()
        {
            Logger.Logger.LogInstance.LogDebug("SerialCommand DisconnectToCOMPort method started.");
            try
            {
                if (port != null && port.IsOpen)
                {
                    port.Close();
                    port = null;
                }
                IsReady = false;
            }
            catch (Exception ex)
            {
                this.Message = ex.Message;
                Logger.Logger.LogInstance.LogException(ex);
            }
            Logger.Logger.LogInstance.LogDebug("SerialCommand DisconnectToCOMPort method completed.");
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
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private async void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            await DataReceived();

        }

        virtual public async Task DataReceived()
        {
            Logger.Logger.LogInstance.LogDebug("SerialCommand DataReceived method started.");
            var input = port.ReadExisting();
            var InpBuffer = input.ToCharArray();
            Logger.Logger.LogInstance.LogDebug("COM Read: '{0}'", input);
            int failCount = 0;

            try
            {
                switch (InpBuffer[0])
                {
                    case (char)5:        // Check for <ENQ>
                        {
                            WriteToPort("" + (char)6);
                            break;
                        }

                    case (char)6:      // Check for <ACK>
                        {
                            failCount = 0;
                            switch (index)
                            {
                                case 0:
                                    //(char)2 means start of text
                                    WriteToPort((char)2 + Add_CheckSum(data[index + 1]) + Constants.vbCrLf);
                                    index = 1;
                                    break;
                                case 1:
                                    //(char)2 means start of text
                                    WriteToPort((char)2 + Add_CheckSum(data[index + 1]) + Constants.vbCrLf);
                                    index = 2;
                                    break;
                                case 2:
                                    //(char)2 means start of text
                                    WriteToPort((char)2 + Add_CheckSum(data[index + 1]) + Constants.vbCrLf);
                                    index = 3;
                                    break;
                                case 3:
                                    //(char)2 means start of text
                                    WriteToPort((char)2 + Add_CheckSum(data[index + 1]) + Constants.vbCrLf);
                                    index = 4;
                                    break;
                                case 4: 
                                    // This section is required for XN1000, as it needs 5 frames of data to be sent to the equipment.
                                    // For other equipment this section will not be executed as index will be reset to 0 in default section.
                                    //(char)2 means start of text
                                    WriteToPort((char)2 + Add_CheckSum(data[index + 1]) + Constants.vbCrLf);
                                    index = 5;
                                    break;
                                default:
                                    //(char)4 means end of transmission
                                    WriteToPort("" + (char)4);
                                    index = 0;
                                    this.IsRunning = false;
                                    break;
                            }
                            break;
                        }

                    //When the EVOLIS receives a <NAK> for a frame rejected by a host it resends the frame.
                    //Frames are invalidated when:
                    //1. Any character errors are detected(ie.parity error, framing error)
                    //2. The frame checksum does not match the checksum computed on the received frame.
                    //2. The frame number is not the same as the last accepted frame or one number higher.
                    case (char)21:       // Check for <NAK>
                        {
                            if (failCount < 3)
                            {
                                if (index > 0)
                                {
                                    WriteToPort((char)2 + Add_CheckSum(data[index]) + Constants.vbCrLf);
                                }
                                else
                                {
                                    WriteToPort(data[index]);
                                }
                            }
                            else
                            {
                                this.IsRunning = false;
                            }

                            failCount++;

                            break;
                        }

                    case (char)4:   // Check For the <EOT>
                        {
                            Logger.Logger.LogInstance.LogInfo("SerialCommand Read: '{0}'", sInputMsg);
                            await CreateMessage(sInputMsg);
                            break;
                        }

                    default:
                        {
                            for (int i = 0; i <= InpBuffer.Length - 1; i++)
                            {
                                sInputMsg += InpBuffer[i];

                                if (InpBuffer[i] == Strings.Chr(10))
                                {
                                    WriteToPort("" + (char)6);
                                }
                            }

                            break;
                        }
                }
                Logger.Logger.LogInstance.LogDebug("SerialCommand DataReceived method completed.");
            }
            catch (Exception ex)
            {
                Logger.Logger.LogInstance.LogException("SerialCommand DataReceived method exception", ex);
            }

        }
        /// <summary>
        ///Many serial protocols use checksum (additional bytes added at the end of the data string) to check
        ///the data integrity, as errors might occur during data transmission.        
        ///ETX (end of text) or ETB (end transmission block).
        /// The checksum is encoded as two characters sent after the <ETB> or <ETX>
        ///character.The checksum includes the first character after<STX>(the frame
        ///number) up to and including<ETB> or<ETX>.It is computed by adding the
        ///binary values of the characters, keeping the least significant eight bits of the result.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string Add_CheckSum(string input)
        {
            Logger.Logger.LogInstance.LogDebug("SerialCommand Add_CheckSum method started.");
            Logger.Logger.LogInstance.LogDebug("SerialCommand Add_CheckSum: '{0}'", input);
            string output = "";
            if (input != null)
            {
                int chk = 0;
                for (int i = 0; i <= input.Length - 1; i++)
                    chk += Strings.Asc(input[i]);
                if ((chk % 256) < 16)
                    output = input + "0" + Conversion.Hex(chk % 256);
                else
                    output = input + Conversion.Hex(chk % 256);

                Logger.Logger.LogInstance.LogDebug("Add_CheckSum Return: '{0}'", output);
                Logger.Logger.LogInstance.LogDebug("SerialCommand Add_CheckSum method completed.");
            }
            return output;
        }

        public void StartSendToEquipment(SampleItem item)
        {
            Logger.Logger.LogInstance.LogDebug("SerialCommand StartSendToEquipment method started.");
            if (!port.IsOpen)
            {
                port.Open();
            }
            this.IsRunning = true;
            WriteToPort("" + (char)5);
            Logger.Logger.LogInstance.LogDebug("SerialCommand StartSendToEquipment method completed.");
        }

        protected void WriteToPort(string text)
        {
            port.Write(text);
            Logger.Logger.LogInstance.LogInfo("SerialCommand Write: '{0}'", text);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sampleId">Bar-code Number</param>
        /// <returns></returns>
        virtual public Task SendOrderData(string sampleId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sampleId">Bar-code Number</param>
        /// <param name="sampleNo">4 digit sample number received from Analyzer</param>
        /// <returns></returns>
        virtual public Task SendOrderData(string sampleId, string sampleNo)
        {
            throw new NotImplementedException();
        }

        virtual public Task ParseMessage(string message, ArrayList sampleIdLst)
        {
            throw new NotImplementedException();
        }

        virtual public Task CreateMessage(string message)
        {
            throw new NotImplementedException();
        }

        virtual public Task Identify(string message)
        {
            throw new NotImplementedException();
        }

    }
}