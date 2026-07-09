using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("PatientVisit")]
    public class PatientVisit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PatientVisitId { get; set; }

        [ForeignKey("Patient")]
        public long PatientId { get; set; }

        [JsonIgnore]
        public virtual PatientDetail Patient { get; set; }

        [Required]
        [MaxLength(30)]
        public string VisitId { get; set; }

        public DateTime VisitDateTime { get; set; }

        public long? SaleInvoiceId { get; set; }

        public int VisitStatus { get; set; }

        [MaxLength(80)]
        public string CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        [MaxLength(80)]
        public string ModifiedBy { get; set; }

        public DateTime ModifiedOn { get; set; }

        public bool IsActive { get; set; }
    }

    public class PatientVisitHistoryItem
    {
        public long PatientVisitId { get; set; }
        public string VisitId { get; set; }
        public DateTime VisitDateTime { get; set; }
        public long? SaleInvoiceId { get; set; }
        public string InvoiceNo { get; set; }
        public int VisitStatus { get; set; }
        public string VisitStatusLabel { get; set; }
        public string CreatedBy { get; set; }
    }
}
