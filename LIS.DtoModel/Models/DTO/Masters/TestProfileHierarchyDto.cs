using System.Collections.Generic;

namespace LIS.DtoModel.Models
{
    public class TestProfileHierarchyDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal PackageRate { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public System.DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public System.DateTime ModifiedOn { get; set; }
        public ICollection<TestProfileDetail> ProfileDetails { get; set; }
        public List<TestProfileTestNodeDto> Tests { get; set; }
    }

    public class TestProfileTestNodeDto
    {
        public int TestId { get; set; }
        public int Quantity { get; set; }
        public string TestCode { get; set; }
        public string TestName { get; set; }
        public List<TestProfileParameterNodeDto> Parameters { get; set; }
    }

    public class TestProfileParameterNodeDto
    {
        public string ParamCode { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public string Method { get; set; }
        public List<TestProfileRangeNodeDto> Ranges { get; set; }
    }

    public class TestProfileRangeNodeDto
    {
        public string RangeCode { get; set; }
        public string RangeValue { get; set; }
        public string Gender { get; set; }
        public decimal MinValue { get; set; }
        public decimal MaxValue { get; set; }
    }
}
