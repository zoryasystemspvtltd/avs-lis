using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models.Reports
{
    /// <summary>
    /// Per report-type runtime mode: SystemDefault (factory) vs Custom (activated customs may apply).
    /// Does not delete custom templates when switching modes.
    /// </summary>
    [Table("ReportTemplateModeSetting")]
    public class ReportTemplateModeSetting
    {
        [Key]
        [MaxLength(40)]
        public string ReportType { get; set; }

        /// <summary>SystemDefault | Custom</summary>
        [Required]
        [MaxLength(20)]
        public string Mode { get; set; }

        [MaxLength(80)]
        public string ModifiedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }
    }
}
