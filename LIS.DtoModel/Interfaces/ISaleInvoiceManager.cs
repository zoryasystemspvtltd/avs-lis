using LIS.DtoModel.Models;

namespace LIS.DtoModel.Interfaces
{
    public interface ISaleInvoiceManager
    {
        SaleInvoiceDto GetById(long id);
        ItemList<SaleInvoice> Get(ListOptions option);
        long Save(SaleInvoiceDto dto);
        void UpdateStatus(long id, int invoiceStatus, int paymentStatus);
        void Cancel(long id);
        string GenerateInvoiceNo();
    }
}
