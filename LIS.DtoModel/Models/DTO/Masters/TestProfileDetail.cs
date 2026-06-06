using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS.DtoModel.Models
{
    [Table("TestProfileDetail")]
    public class TestProfileDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("TestProfileMaster")]
        public int TestProfileId { get; set; }

        [JsonIgnore]
        public virtual TestProfileMaster TestProfileMaster { get; set; }

        [ForeignKey("HisTestMaster")]
        public int TestId { get; set; }

        [JsonIgnore]
        public virtual HisTestMaster HisTestMaster { get; set; }

        public int Quantity { get; set; }
    }
}
