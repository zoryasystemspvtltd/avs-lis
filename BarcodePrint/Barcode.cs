using System;
namespace BarcodePrint
{
    public class BarCode
    {
        public bool IsPrint { get; set; }
       
        public string BarcodeNo { get; set; }
        public string PatientName { get; set; }
        public DateTime CollectionDate { get; set; }
        public string TestName { get; set; }
        public string BedNo { get; set; }
        public string IPNo { get; set; }
        public string LabNo { get; set; }
        public string GroupName { get; set; }
    }
}
