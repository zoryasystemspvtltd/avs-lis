using LIS.DtoModel.Models;

namespace LIS.DtoModel.Interfaces
{
    public interface IReportManager
    {
        ItemList<SaleInvoiceRegisterRow> GetSaleInvoiceRegister(ReportFilterOptions options);
        ItemList<TestBookingRegisterRow> GetTestBookingRegister(ReportFilterOptions options);
        ItemList<CollectionSummaryRow> GetCollectionSummary(ReportFilterOptions options);
        ItemList<CollectorWiseRow> GetCollectorWiseReport(ReportFilterOptions options);
        ItemList<PendingCollectionRow> GetPendingCollectionReport(ReportFilterOptions options);
        ItemList<RecollectionRow> GetRecollectionReport(ReportFilterOptions options);
        ItemList<ReceivedSampleRow> GetReceivedSamplesReport(ReportFilterOptions options);
        ItemList<RejectedSampleRow> GetRejectedSamplesReport(ReportFilterOptions options);
        ItemList<SampleTurnaroundRow> GetSampleTurnaroundReport(ReportFilterOptions options);
        ItemList<PendingRadiologyRow> GetPendingRadiologyReport(ReportFilterOptions options);
        ItemList<AuthorizedRadiologyRow> GetAuthorizedRadiologyReport(ReportFilterOptions options);
        ItemList<ModalityStatisticsRow> GetModalityStatisticsReport(ReportFilterOptions options);
        ItemList<RadiologistProductivityRow> GetRadiologistProductivityReport(ReportFilterOptions options);
    }
}
