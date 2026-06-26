namespace LIS.DtoModel.Models
{
    public class ProfileDiagnosticAuditRow
    {
        public int ProfileId { get; set; }
        public string ProfileCode { get; set; }
        public string ProfileName { get; set; }
        public int TestId { get; set; }
        public string TestCode { get; set; }
        public string TestName { get; set; }
        public string DepartmentCode { get; set; }
        public string ProcessingCategory { get; set; }
    }
}
