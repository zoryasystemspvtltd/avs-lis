using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface ITestReportManager
    {
        DiagnosticTestReportDto GetDiagnosticTestReport(string labNo, string invoiceNo);
        IEnumerable<TestReportLabNoOption> GetPrintableLabNumbers();
    }
}
