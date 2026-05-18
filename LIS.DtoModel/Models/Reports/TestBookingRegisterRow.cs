using System;

namespace LIS.DtoModel.Models
{
    public class TestBookingRegisterRow
    {
        public long Id { get; set; }
        public DateTime BookingDate { get; set; }
        public string RequestNumber { get; set; }
        public string InvoiceNumber { get; set; }
        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public string TestName { get; set; }
        public string Department { get; set; }
        public string Specimen { get; set; }
        public string ReferralDoctor { get; set; }
        public string Status { get; set; }
        public string CreatedBy { get; set; }
        public string SampleNo { get; set; }
    }
}
