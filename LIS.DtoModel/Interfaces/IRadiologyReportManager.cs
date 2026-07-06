using LIS.DtoModel.Models;

namespace LIS.DtoModel.Interfaces
{
    public interface IRadiologyReportManager
    {
        ItemList<RadiologyQueueRow> GetPendingQueue(SampleWorkflowSearchOptions options);
        ItemList<RadiologyQueueRow> GetDoctorApprovalQueue(SampleWorkflowSearchOptions options);
        ItemList<RadiologyQueueRow> GetApprovedQueue(SampleWorkflowSearchOptions options);
        RadiologyReportDetailDto GetReport(long radiologyRequestId);
        long CreateRequest(RadiologyRequestDetail request);
        void SaveReport(RadiologyReportSaveRequest request);
        void AuthorizeReport(RadiologyAuthorizeRequest request, bool canAuthorize);
        System.Collections.Generic.List<RadiologyPrintAccessionOption> GetPrintableAccessions();
        DiagnosticRadiologyReportDto GetRadiologyReportForPrint(long radiologyRequestId);
    }
}
