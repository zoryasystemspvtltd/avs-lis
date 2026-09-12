using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface ITestReportManager
    {
        DiagnosticTestReportDto GetDiagnosticTestReport(string labNo, string invoiceNo, long? testRequestDetailId = null);
        TestReportPrintOptionsDto GetPrintableTestOptions(string labNo, string invoiceNo);
        IEnumerable<TestReportLabNoOption> GetPrintableLabNumbers();
    }
}
