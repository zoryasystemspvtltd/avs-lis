using System;

namespace LIS.DtoModel.Models
{
    public class CollectionSummaryRow
    {
        public long Id { get; set; }
        public DateTime CollectionDate { get; set; }
        public string SampleNo { get; set; }
        public string OrderNumber { get; set; }
        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public string TestName { get; set; }
        public string CollectedBy { get; set; }
        public string Status { get; set; }
    }

    public class CollectorWiseRow
    {
        public string CollectorName { get; set; }
        public int TotalCollected { get; set; }
        public int TotalRejected { get; set; }
        public int TotalRecollection { get; set; }
    }

    public class PendingCollectionRow
    {
        public long Id { get; set; }
        public string SampleNo { get; set; }
        public string OrderNumber { get; set; }
        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public string TestName { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class RecollectionRow
    {
        public long Id { get; set; }
        public string SampleNo { get; set; }
        public string OrderNumber { get; set; }
        public string PatientName { get; set; }
        public string TestName { get; set; }
        public string CollectedBy { get; set; }
        public DateTime CollectionDate { get; set; }
        public string Remarks { get; set; }
    }

    public class ReceivedSampleRow
    {
        public long Id { get; set; }
        public string SampleNo { get; set; }
        public string PatientName { get; set; }
        public string TestName { get; set; }
        public DateTime CollectionDate { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string ReceivedBy { get; set; }
        public int TurnaroundMinutes { get; set; }
    }

    public class RejectedSampleRow
    {
        public long Id { get; set; }
        public string SampleNo { get; set; }
        public string PatientName { get; set; }
        public string TestName { get; set; }
        public string RejectionReason { get; set; }
        public string RejectedBy { get; set; }
        public DateTime RejectedOn { get; set; }
        public string Stage { get; set; }
    }

    public class SampleTurnaroundRow
    {
        public string SampleNo { get; set; }
        public string PatientName { get; set; }
        public string TestName { get; set; }
        public DateTime CollectionDate { get; set; }
        public DateTime ReceivedDate { get; set; }
        public int TurnaroundMinutes { get; set; }
    }

    public class PendingRadiologyRow
    {
        public long Id { get; set; }
        public string AccessionNo { get; set; }
        public string PatientName { get; set; }
        public string TestName { get; set; }
        public string Modality { get; set; }
        public string Status { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class AuthorizedRadiologyRow
    {
        public long Id { get; set; }
        public string AccessionNo { get; set; }
        public string PatientName { get; set; }
        public string TestName { get; set; }
        public string Modality { get; set; }
        public string AuthorizedBy { get; set; }
        public DateTime? AuthorizedOn { get; set; }
        public string Status { get; set; }
    }

    public class ModalityStatisticsRow
    {
        public string Modality { get; set; }
        public int TotalCases { get; set; }
        public int AuthorizedCases { get; set; }
        public int PendingCases { get; set; }
    }

    public class RadiologistProductivityRow
    {
        public string RadiologistName { get; set; }
        public int AuthorizedCount { get; set; }
        public int ReleasedCount { get; set; }
    }
}
