using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models.Notification
{
    [Table("SecureLinkToken")]
    public class SecureLinkToken
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [MaxLength(64)]
        public string Token { get; set; }

        [MaxLength(50)]
        public string InvoiceNo { get; set; }

        public long? PatientId { get; set; }

        public DateTime ExpiresOn { get; set; }

        public bool IsUsed { get; set; }

        public DateTime CreatedOn { get; set; }

        [MaxLength(80)]
        public string CreatedBy { get; set; }
    }
}
