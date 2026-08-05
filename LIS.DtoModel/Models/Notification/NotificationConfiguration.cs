using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models.Notification
{
    [Table("NotificationConfiguration")]
    public class NotificationConfiguration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public bool IsEnabled { get; set; }

        public int ChannelMode { get; set; }

        public int RetryCount { get; set; }

        public int RetryIntervalSeconds { get; set; }

        public int DefaultChannel { get; set; }

        public bool SmsEnabled { get; set; }

        public bool WhatsAppEnabled { get; set; }

        public DateTime CreatedOn { get; set; }

        [MaxLength(80)]
        public string CreatedBy { get; set; }

        public DateTime ModifiedOn { get; set; }

        [MaxLength(80)]
        public string ModifiedBy { get; set; }
    }
}
