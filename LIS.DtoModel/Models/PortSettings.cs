namespace LIS.DtoModel
{
    public class PortSettings
    {
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public int StopBits { get; set; }
        public int Parity { get; set; }
        public bool AutoConnect { get; set; }
        public string TestName { get; set; }
        public bool RunOnStartup { get; set; }
    }
}
