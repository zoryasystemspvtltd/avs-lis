namespace LIS.DtoModel.Models
{
    /// <summary>Server-side sale-invoice item lookup (test or profile).</summary>
    public class BillableItemLookup
    {
        public string Key { get; set; }
        public string Label { get; set; }
        /// <summary>test | profile</summary>
        public string ItemType { get; set; }
        public int? TestId { get; set; }
        public int? TestProfileId { get; set; }
        public string DepartmentCode { get; set; }
    }
}
