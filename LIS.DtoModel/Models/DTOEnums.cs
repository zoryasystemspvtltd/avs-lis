namespace LIS.DtoModel.Models
{
    public enum ReportStatusType
    {
        New = 0,
        SentToEquipment = 1,
        ReportGenerated = 2,
        TechnicianApproved = 3,
        TechnicianRejected = 4,
        DoctorApproved = 5,
        DoctorRejected = 6,
        FinallyRejected = 7
    }

    public enum InvoiceStatusType
    {
        Draft = 0,
        Confirmed = 1,
        Paid = 2,
        Cancelled = 3
    }

    public enum PaymentStatusType
    {
        Unpaid = 0,
        Partial = 1,
        Paid = 2
    }

    public enum RateType
    {
        Standard = 0,
        Corporate = 1,
        ReferralDoctor = 2,
        Profile = 3,
        Emergency = 4
    }
}
