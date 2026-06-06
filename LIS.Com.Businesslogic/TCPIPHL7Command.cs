using LIS.DtoModel;
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
    public class TCPIPHL7Command
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
        private volatile bool isDisconnecting = false;

        public TCPIPHL7Command(TCPIPSettings settings)
        {
            Logger.Logger.LogInstance.LogDebug("LIS.Com.Businesslogic TCPIPHL7Command Constructor method started.");
            this._settings = settings;

            // Initialize heartbeat timer (60 seconds)
            timer = new System.Timers.Timer(_settings.HeartbitTimeout * 1000);
            timer.Elapsed += OnHeartbeatTimerElapsed;
            timer.AutoReset = true;

            Logger.Logger.LogInstance.LogDebug("LIS.Com.Businesslogic TCPIPHL7Command Constructor method completed.");
        }

        public void StartListenerAsync()
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
            string messageControlId = "";
            char[] charArray = new char[10240];
            var sInputMsg = new StringBuilder();
            var messageBuffer = new StringBuilder();

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

                    messageBuffer.Append(rawmsg);
                    ProcessBufferedMessages(messageBuffer, ref sInputMsg, ref messageControlId);
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

        private void ProcessBufferedMessages(StringBuilder messageBuffer, ref StringBuilder sInputMsg, ref string messageControlId)
        {
            string bufferContent = messageBuffer.ToString();
            int fsIndex = bufferContent.IndexOf((char)28);

            while (fsIndex >= 0)
            {
                try
                {
                    string completeMsg = bufferContent.Substring(0, fsIndex + 1);
                    messageBuffer.Remove(0, fsIndex + 1);
                    bool isQRC = false;
                    string hl7Content = completeMsg.Length > 2 ? completeMsg.Substring(1, completeMsg.Length - 2) : "";
                    if (string.IsNullOrEmpty(hl7Content)) continue;

                    var blocks = hl7Content.Split((char)13);
                    bool orderRequest = false;

                    foreach (var block in blocks)
                    {
                        if (string.IsNullOrWhiteSpace(block)) continue;

                        var input = block.Split('|');
                        if (input.Length == 0) continue;

                        string segmentType = input[0].TrimStart('', '|');

                        switch (segmentType.Trim())
                        {
                            case "MSH":
                                sInputMsg.Clear();
                                orderRequest = input.Length > 8 && input[8] == "QRY^Q02" || input[8] == "ORM^O01";
                                messageControlId = input.Length > 9 ? input[9] : "";
                                if (!orderRequest)
                                {
                                    sInputMsg.Append(block + (char)13);
                                }
                                break;
                            case "QRD":

                                if (orderRequest && input.Length > 8)
                                {
                                    var sampleId = input[8];
                                    var response = SendOrderData(sampleId, messageControlId).Result;
                                    if (response != null)
                                    {
                                        if (!string.IsNullOrEmpty(response.QRYResponse))
                                            WriteResponseSafe(response.QRYResponse, false);
                                        if (!string.IsNullOrEmpty(response.DSRResponse))
                                            WriteResponseSafe(response.DSRResponse, false);
                                    }
                                }
                                break;
                            case "ORC":
                                isQRC = true;
                                if (orderRequest && input.Length > 2)
                                {
                                    var sampleId = input[3];
                                    var response = SendOrderData(sampleId, messageControlId).Result;
                                    if (response != null)
                                    {
                                        if (!string.IsNullOrEmpty(response.QRYResponse))
                                            WriteResponseSafe(response.QRYResponse, false);
                                    }
                                }
                                break;
                            case "OBR":
                            case "OBX":
                                sInputMsg.Append(block + (char)13);
                                break;
                        }
                    }

                    if (sInputMsg.Length > 150)
                    {
                        ResultProcess(sInputMsg.ToString(), messageControlId).Wait();
                        sInputMsg.Clear();
                        string value = isQRC == true ? "UNICODE" : "ASCII";
                        string ackResponse = $@"MSH|^~\&|LIS||||{DateTime.Now:yyyyMMddHHmmss}||ACK^R01|{messageControlId}|P|2.3.1||||2||{value}{(char)13}MSA|AA|{messageControlId}|Message accepted|||0{(char)13}";
                        
                        WriteResponseSafe(ackResponse, false);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Logger.LogInstance.LogException(ex);
                }

                bufferContent = messageBuffer.ToString();
                fsIndex = bufferContent.IndexOf((char)28);
            }
        }

        private void WriteResponseSafe(string response, bool isHeartBeat)
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
            string heartbeatControlId = "HB" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string heartbeatMsg = $@"MSH|^~\&|LIS|LAB|ANALYZER|RECEIVER|{DateTime.Now:yyyyMMddHHmmss}||ACK|P|2.3.1||||2||ASCII{(char)13}
MSA|AA|{heartbeatControlId}|Analyzer heartbeat check{(char)13}";

            //Logger.Logger.LogInstance.LogInfo("Sending heartbeat ACK (ID: {0})", heartbeatControlId);
            WriteResponseSafe(heartbeatMsg, true);
        }

        private void WriteResponse(string response, StreamWriter sw, bool isHeartBeat)
        {
            var res = AddHeaderAndFooterToHL7Msg(response);
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

        public string AddHeaderAndFooterToHL7Msg(string RawMessage)
        {
            char BeginFormat = (char)11;  // VT
            char EndFormat1 = (char)28;   // FS
            char EndFormat2 = (char)13;   // CR

            string NwkMessage = BeginFormat + RawMessage + EndFormat1 + EndFormat2;
            return NwkMessage;
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

        virtual public Task<OrderHL7Response> SendOrderData(string sampleNo, string messageControlId)
        {
            throw new NotImplementedException();
        }

        virtual public string SendResponse(string sampleNo, string messageControlId)
        {
            throw new NotImplementedException();
        }

        virtual public Task ResultProcess(string message, string messageControlId)
        {
            throw new NotImplementedException();
        }
    }

    public class OrderHL7Response
    {
        public string QRYResponse { get; set; }
        public string DSRResponse { get; set; }
    }
}