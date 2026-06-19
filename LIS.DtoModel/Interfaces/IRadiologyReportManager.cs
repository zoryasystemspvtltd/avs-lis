using LIS.DtoModel.Models;

namespace LIS.DtoModel.Interfaces
{
    public interface IRadiologyReportManager
    {
        ItemList<RadiologyQueueRow> GetPendingQueue(SampleWorkflowSearchOptions options);
        RadiologyReportDetailDto GetReport(long radiologyRequestId);
        long CreateRequest(RadiologyRequestDetail request);
        void SaveReport(RadiologyReportSaveRequest request);
        void AuthorizeReport(RadiologyAuthorizeRequest request, bool canAuthorize);
    }
}
