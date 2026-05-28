using LIS.DtoModel;
using Microsoft.VisualBasic;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace LIS.Com.Businesslogic
{
    public class TCPIPASTMCommand
    {
        private TCPIPSettings _settings;
        protected Thread reportingThread;
        protected Socket soc;
        protected Stream sm;
        protected StreamWriter sw;
        protected StreamReader sr;
        protected TcpListener server;
        public bool IsReady { get; private set; }
        public bool AnalyzerActive { get; private set; } = false;
        public string FullMessage { get; private set; }
        protected System.Timers.Timer timer;
        private CancellationTokenSource disconnectTokenSource;
        private readonly object _lockObject = new object();
        private volatile bool _connectionEstablished = false;
        protected string[] output = new string[5];
        protected int index;
        private volatile bool isDisconnecting = false;
        protected NetworkStream stream;
        //StringBuilder sInputMsg = new StringBuilder();
        public TCPIPASTMCommand(TCPIPSettings settings)
        {
            Logger.Logger.LogInstance.LogDebug("LIS.Com.Businesslogic TCPIPASTMCommand Constructor method started.");
            _settings = settings;

            // Initialize heartbeat timer (60 seconds)
            timer = new System.Timers.Timer(_settings.HeartbitTimeout * 1000);
            timer.Elapsed += OnHeartbeatTimerElapsed;
            timer.AutoReset = true;
            Logger.Logger.LogInstance.LogDebug("LIS.Com.Businesslogic TCPIPASTMCommand Constructor method completed.");
        }

        // New async listener modeled after TCPIPHL7Command
        public void StartListenerAsync(CancellationToken externalToken)
        {
            Logger.Logger.LogInstance.LogDebug("TCPIPCommand ConnectToTCPIP method started.");
            try
            {
                if (string.IsNullOrWhiteSpace(_settings?.IPAddress) || _settings.PortNo <= 0)
                    throw new ArgumentException("Invalid TCP settings");

                var ipAddress = IPAddress.Parse(_settings.IPAddress);
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, _settings.PortNo);
                server = new TcpListener(localEndPoint);
                server.Start();
                disconnectTokenSource = new CancellationTokenSource();

                reportingThread = new Thread(() => TCP_ListenLoop(disconnectTokenSource.Token));
                reportingThread.IsBackground = true;
                reportingThread.Start();
                IsReady = true;
                Logger.Logger.LogInstance.LogDebug("TCPIPCommand ConnectToTCPIP method completed.");
            }
            catch (Exception ex)
            {
                this.FullMessage = ex.Message;
                Logger.Logger.LogInstance.LogException(ex);
            }
        }

        /// <summary>
        /// Main accept loop: accepts clients repeatedly until cancellation requested.
        /// For each accepted client it processes incoming messages until client disconnects,
        /// then cleans up and waits for the next client.
        /// </summary>
        private void TCP_ListenLoop(CancellationToken token)
        {
            Logger.Logger.LogInstance.LogDebug("TCPIPCommand TCP_ListenLoop started.");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // AcceptTcpClient blocks until a client connects or the listener is stopped
                    TcpClient tcpClient = null;
                    try
                    {
                        tcpClient = server.AcceptTcpClient();
                    }
                    catch (SocketException sockEx)
                    {
                        // If the listener was stopped, break the loop
                        if (token.IsCancellationRequested) break;
                        Logger.Logger.LogInstance.LogException(sockEx);
                        Thread.Sleep(100);
                        continue;
                    }

                    if (tcpClient == null) continue;

                    // Create socket/streams for this client
                    lock (_lockObject)
                    {
                        // Cleanup any previous connection (defensive)
                        CleanupConnection();

                        soc = tcpClient.Client;
                        sm = tcpClient.GetStream();
                        sr = new StreamReader(sm, Encoding.ASCII);
                        sw = new StreamWriter(sm, Encoding.ASCII) { AutoFlush = true };

                        _connectionEstablished = true;
                        AnalyzerActive = true;
                        Logger.Logger.LogInstance.LogInfo("TCP connection established successfully from {0}", tcpClient.Client.RemoteEndPoint);

                        // Start heartbeat timer
                        timer.Start();
                    }

                    // Process this client's incoming data until it disconnects
                    ProcessIncomingMessages(token);

                    // When processing returns, ensure cleanup for this client and continue to accept next
                    CleanupConnection();
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Logger.Logger.LogInstance.LogException(ex);
                    }
                    Thread.Sleep(100);
                }
            }

            Logger.Logger.LogInstance.LogDebug("TCPIPCommand TCP_ListenLoop exiting.");
        }

        private void ProcessIncomingMessages(CancellationToken token)
        {
            char[] charArray = new char[10240];
            var sInputMsg = new StringBuilder();
            while (!token.IsCancellationRequested && _connectionEstablished)
            {
                try
                {
                    // Defensive checks
                    if (sr == null || sm == null || soc == null || !soc.Connected)
                    {
                        Logger.Logger.LogInstance.LogInfo("Socket disconnected or streams null - breaking read loop.");
                        break;
                    }

                    int readByteCount = sr.Read(charArray, 0, charArray.Length);

                    // If 0 bytes read -> remote closed the connection gracefully
                    if (readByteCount == 0)
                    {
                        Logger.Logger.LogInstance.LogInfo("Client closed the connection (read returned 0).");
                        break;
                    }

                    string rawmsg = new string(charArray, 0, readByteCount);
                    Logger.Logger.LogInstance.LogInfo("COM Read: '{0}'", rawmsg);

                    //messageBuffer.Append(rawmsg);
                    ProcessBufferedMessages(rawmsg, ref sInputMsg);                    
                }
                catch (IOException ioex)
                {
                    Logger.Logger.LogInstance.LogWarning("IO exception while reading: {0}", ioex.Message);
                    break; // break the loop so we cleanup and accept a new client
                }
                catch (ObjectDisposedException odex)
                {
                    Logger.Logger.LogInstance.LogWarning("Stream was disposed while reading: {0}", odex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Logger.Logger.LogInstance.LogException(ex);
                    }

                    // Small pause to avoid tight error loop
                    Thread.Sleep(100);
                }
            }

            _connectionEstablished = false;
            timer.Stop();
            Logger.Logger.LogInstance.LogInfo("Exiting ProcessIncomingMessages for current client.");
        }

        private void ProcessBufferedMessages(string bufferContent, ref StringBuilder sInputMsg)
        {
            //sInputMsg.Append(bufferContent);
            try
            {
                var InpBuffer = bufferContent.ToCharArray();
                int failCount = 0;
                for (int i = 0; i < InpBuffer.Length; i++)
                {
                    char ch = InpBuffer[i];

                    switch (ch)
                    {
                        case (char)5:        // Check for <ENQ>
                            {
                                WriteResponseSafe(((char)6).ToString(), false);
                                break;
                            }

                        case (char)6:      // Check for <ACK>
                            {
                                failCount = 0;
                                switch (index)
                                {
                                    case 0:
                                        //(char)2 means start of text

                                        var payload1 = ((char)2) + Add_CheckSum(output[index + 1]) + (char)13;
                                        WriteResponseSafe(payload1, false);
                                        index = 1;
                                        break;
                                    case 1:
                                        //(char)2 means start of text
                                        var payload2 = ((char)2) + Add_CheckSum(output[index + 1]) + (char)13;
                                        WriteResponseSafe(payload2, false);
                                        index = 2;
                                        break;
                                    case 2:
                                        //(char)2 means start of text
                                        var payload3 = ((char)2) + Add_CheckSum(output[index + 1]) + (char)13;
                                        WriteResponseSafe(payload3, false);
                                        index = 3;
                                        break;
                                    case 3:
                                        //(char)2 means start of text
                                        var payload4 = ((char)2) + Add_CheckSum(output[index + 1]) + (char)13;
                                        WriteResponseSafe(payload4, false);
                                        index = 4;
                                        break;

                                    default:
                                        //(char)4 means end of transmission
                                        WriteResponseSafe("" + (char)4, false);
                                        index = 0;
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
                                        var payload = ((char)2) + Add_CheckSum(output[index]) + (char)13;
                                        WriteResponseSafe(payload, false);
                                    }
                                    else
                                    {
                                        WriteResponseSafe(output[index], false);
                                    }
                                }

                                failCount++;
                                break;
                            }

                        case (char)4:   // Check For the <EOT>
                            {
                                Logger.Logger.LogInstance.LogInfo("SerialCommand Read: '{0}'", sInputMsg);
                                CreateMessageAsync(sInputMsg.ToString());
                                sInputMsg.Clear();
                                break;
                            }

                        default:
                            {
                                sInputMsg.Append(InpBuffer[i]);

                                if (InpBuffer[i] == Strings.Chr(10))
                                {
                                    WriteResponseSafe("" + (char)6,false);
                                }

                                break;
                            }
                    }
                    Logger.Logger.LogInstance.LogDebug("SerialCommand DataReceived method completed.");
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.LogInstance.LogException(ex);
            }
        }

        public void WriteResponseSafe(string response,bool isHeartBeat)
        {
            lock (_lockObject)
            {
                if (_connectionEstablished && sw != null && soc != null && soc.Connected)
                {
                    try
                    {
                        WriteResponse(response, sw, isHeartBeat);
                    }
                    catch (ObjectDisposedException)
                    {
                        Logger.Logger.LogInstance.LogWarning("Attempted to write to a closed writer.");
                        // mark connection as dead so it will be cleaned up
                        _connectionEstablished = false;
                    }
                    catch (IOException ioex)
                    {
                        Logger.Logger.LogInstance.LogWarning("IOException while writing response: {0}", ioex.Message);
                        _connectionEstablished = false;
                    }
                    catch (Exception ex)
                    {
                        Logger.Logger.LogInstance.LogException(ex);
                    }
                }
                else
                {
                    Logger.Logger.LogInstance.LogWarning("Write ignored: connection not established or writer is null/closed.");
                }
            }
        }

        // Heartbeat method - sends HL7 ACK every 60 seconds to check analyzer
        private void OnHeartbeatTimerElapsed(object sender, ElapsedEventArgs e)
        {
            lock (_lockObject)
            {
                if (!_connectionEstablished || sw == null || soc == null || !soc.Connected)
                {
                    Logger.Logger.LogInstance.LogInfo("Analyzer disconnected (heartbeat check). Will cleanup and wait for new client.");
                    _connectionEstablished = false;
                    try
                    {
                        CleanupConnection();
                    }
                    catch (Exception ex)
                    {
                        Logger.Logger.LogInstance.LogException(ex);
                    }
                    timer.Stop();
                    return;
                }
            }

            try
            {
                SendHeartbit();
            }
            catch (Exception ex)
            {
                AnalyzerActive = false;
                Logger.Logger.LogInstance.LogError("Heartbeat failed: {0}", ex.Message);
            }
        }
        private void SendHeartbit()
        {
            string heartbeatMsg = "<ACK>";
            WriteResponseSafe(heartbeatMsg,true);
        }
        private void WriteResponse(string res, StreamWriter sw, bool isHeartBeat)
        {
            if (isHeartBeat)
                Logger.Logger.LogInstance.LogInfo("HeartBeat: '{0}'", res);
            else
                Logger.Logger.LogInstance.LogInfo("COM Write: '{0}'", res);

            try
            {
                char[] datachar = res.ToCharArray();
                sw.Write(datachar, 0, datachar.Length);
                sw.Flush();
            }
            catch (IOException)
            {
                throw;
            }
        }
        private void CleanupConnection()
        {
            lock (_lockObject)
            {
                try
                {
                    timer?.Stop();

                    _connectionEstablished = false;

                    try { sw?.Close(); } catch { }
                    try { sr?.Close(); } catch { }
                    try { sm?.Close(); } catch { }

                    if (soc != null)
                    {
                        try { soc.Shutdown(SocketShutdown.Both); } catch { }
                        try { soc.Close(); } catch { }
                    }

                    sw = null;
                    sr = null;
                    sm = null;
                    soc = null;
                }
                catch (Exception ex)
                {
                    Logger.Logger.LogInstance.LogException(ex);
                }
            }
        }


        public void DisconnectToTCPIPAsync()
        {
            try
            {
                isDisconnecting = true;
                disconnectTokenSource?.Cancel();

                try { server?.Stop(); } catch { }

                CleanupConnection();

                server = null;

                if (reportingThread != null)
                {
                    if (!reportingThread.Join(500))
                    {
                        try { reportingThread.Interrupt(); } catch { }
                    }
                }

                reportingThread = null;
                IsReady = false;
                AnalyzerActive = false;

                try { Task.Run(() => Logger.Logger.LogInstance.LogInfo("LIS disconnected. Analyzer: {0}", AnalyzerActive)); } catch { }
            }
            catch (Exception ex)
            {
                Logger.Logger.LogInstance.LogException(ex);
            }
            finally
            {
                isDisconnecting = false;
            }
        }
        // Compute checksum same semantics as original VB implementation:
        // sum of bytes from block-number (first char after STX) through ETX/ETB,
        // keep low-order 8 bits and present hex as uppercase with leading zero if needed.
        public string Add_CheckSum(string input)
        {
            Logger.Logger.LogInstance.LogDebug($"TCPIPASTMCommand Add_CheckSum: '{input}'");
            if (input == null) return string.Empty;

            int chk = 0;
            for (int i = 0; i < input.Length; i++)
            {
                chk += (byte)input[i];
            }

            int low = chk % 256;
            string hex = low.ToString("X");
            if (hex.Length == 1) hex = "0" + hex;

            var result = input + hex;
            Logger.Logger.LogInstance.LogDebug($"Add_CheckSum Return: '{result}'");
            return result;
        }
        virtual public Task CreateMessageAsync(string message)
        {
            throw new NotImplementedException();
        }

    }
}