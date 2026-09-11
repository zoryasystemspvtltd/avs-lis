using LIS.DtoModel.Models.Reports;

namespace LIS.DtoModel.Interfaces
{
    public interface IReportLayoutConfigurationManager
    {
        ReportLayoutConfigurationDto GetByReportType(string reportType);
        ReportLayoutConfigurationDto GetDefaults(string reportType);
        ReportLayoutConfigurationDto Save(ReportLayoutConfigurationDto dto);
        ReportLayoutConfigurationDto ResetToDefault(string reportType);
    }
}
