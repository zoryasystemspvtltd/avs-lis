using LIS.DtoModel.Models;

namespace LIS.DtoModel.Interfaces
{
    public interface IReportManager
    {
        ItemList<SaleInvoiceRegisterRow> GetSaleInvoiceRegister(ReportFilterOptions options);
        ItemList<TestBookingRegisterRow> GetTestBookingRegister(ReportFilterOptions options);
    }
}
