using LIS.DtoModel.Models;
using System;

namespace LIS.DtoModel.Interfaces
{
    public interface ISaleInvoiceManager
    {
        SaleInvoiceDto GetById(long id);
        ItemList<SaleInvoice> Get(ListOptions option);
        ItemList<BillableItemLookup> GetBillableItems(ListOptions option, DateTime? invoiceDate = null);
        long Save(SaleInvoiceDto dto);
        void UpdateStatus(long id, int invoiceStatus, int paymentStatus, decimal? paidAmount = null);
        void Cancel(long id);
        string GenerateInvoiceNo();
    }
}
