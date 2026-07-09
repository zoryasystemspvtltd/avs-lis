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

    public enum RadiologyReportStatus
    {
        Pending = 0,
        Draft = 1,
        UnderReview = 2,
        Authorized = 3,
        Released = 4
    }

    public enum VisitStatusType
    {
        New = 0,
        InProgress = 1,
        Completed = 2,
        Cancelled = 3
    }
}
