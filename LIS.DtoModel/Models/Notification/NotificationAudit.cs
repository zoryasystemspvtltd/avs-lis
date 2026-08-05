using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models.Notification
{
    [Table("NotificationAudit")]
    public class NotificationAudit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? PatientId { get; set; }

        [MaxLength(50)]
        public string PatientName { get; set; }

        public long? InvoiceId { get; set; }

        [MaxLength(50)]
        public string InvoiceNo { get; set; }

        [Required]
        [MaxLength(50)]
        public string EventCode { get; set; }

        public int Channel { get; set; }

        public int? TemplateId { get; set; }

        public int? TemplateVersion { get; set; }

        [MaxLength(50)]
        public string ProviderName { get; set; }

        [MaxLength(20)]
        public string RecipientPhone { get; set; }

        public string MessageBody { get; set; }

        public int Status { get; set; }

        public int RetryCount { get; set; }

        public int Priority { get; set; }

        [MaxLength(500)]
        public string ProviderResponse { get; set; }

        [MaxLength(500)]
        public string ErrorMessage { get; set; }

        [MaxLength(100)]
        public string SecureLinkToken { get; set; }

        [MaxLength(64)]
        public string CorrelationId { get; set; }

        public int? ElapsedTimeMs { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? SentOn { get; set; }

        public DateTime? NextRetryOn { get; set; }

        [MaxLength(80)]
        public string CreatedBy { get; set; }
    }
}
