using System;

namespace LIS.DtoModel.Models
{
    public class SampleWorkflowSearchOptions : ListOptions
    {
        public long? PatientId { get; set; }
        public string Uhid { get; set; }
        public string BarcodeNumber { get; set; }
        public string OrderNumber { get; set; }
        public string PatientName { get; set; }
        public DateTime? CollectionDate { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string CollectorName { get; set; }
        public string Modality { get; set; }
        public string QueueType { get; set; }
    }

    public class SampleCollectionAction
    {
        public long TestRequestId { get; set; }
        public DateTime CollectionDateTime { get; set; }
        public string Remarks { get; set; }
        public string BarcodeNumber { get; set; }
    }

    public class SampleRejectionAction
    {
        public long TestRequestId { get; set; }
        public string RejectionReasonCode { get; set; }
        public string Remarks { get; set; }
        public bool IsReceiving { get; set; }
    }

    public class SampleReceivingAction
    {
        public long TestRequestId { get; set; }
        public DateTime ReceivedDateTime { get; set; }
        public string Remarks { get; set; }
        public string BarcodeNumber { get; set; }
    }

    public class SampleWorkflowQueueRow
    {
        public long Id { get; set; }
        public string SampleNo { get; set; }
        public string HisRequestNo { get; set; }
        public string HisPatientId { get; set; }
        public string PatientName { get; set; }
        public string TestName { get; set; }
        public string Department { get; set; }
        public string Specimen { get; set; }
        public DateTime? SampleCollectionDate { get; set; }
        public DateTime? SampleReceivedDate { get; set; }
        public string CollectedBy { get; set; }
        public string ReceivedBy { get; set; }
        public string Status { get; set; }
        public string CollectedRemarks { get; set; }
        public string ReceivedRemarks { get; set; }
    }

    public class RadiologyReportSaveRequest
    {
        public long RadiologyRequestId { get; set; }
        public string ClinicalHistory { get; set; }
        public string Findings { get; set; }
        public string Impression { get; set; }
        public string Recommendation { get; set; }
        public bool SubmitForReview { get; set; }
    }

    public class RadiologyAuthorizeRequest
    {
        public long RadiologyRequestId { get; set; }
        public string DigitalSignature { get; set; }
        public bool Release { get; set; }
    }

    public class RadiologyQueueRow
    {
        public long Id { get; set; }
        public string HisRequestNo { get; set; }
        public string AccessionNo { get; set; }
        public string HisPatientId { get; set; }
        public string PatientName { get; set; }
        public string TestName { get; set; }
        public string Modality { get; set; }
        public string Department { get; set; }
        public string Status { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
    }

    public class RadiologyReportDetailDto
    {
        public long Id { get; set; }
        public long PatientId { get; set; }
        public string HisRequestNo { get; set; }
        public string AccessionNo { get; set; }
        public string HisPatientId { get; set; }
        public string PatientName { get; set; }
        public string InvoiceNo { get; set; }
        public decimal Age { get; set; }
        public string Gender { get; set; }
        public string TestName { get; set; }
        public string Modality { get; set; }
        public string Department { get; set; }
        public string Status { get; set; }
        public RadiologyReportStatus ReportStatus { get; set; }
        public string ClinicalHistory { get; set; }
        public string Findings { get; set; }
        public string Impression { get; set; }
        public string Recommendation { get; set; }
        public string AuthorizedBy { get; set; }
        public DateTime? AuthorizedOn { get; set; }
        public DateTime? ResultDate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanAuthorize { get; set; }
    }
}
