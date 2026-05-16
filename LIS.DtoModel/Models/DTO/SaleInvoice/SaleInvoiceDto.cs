using System.Collections.Generic;

namespace LIS.DtoModel.Models
{
    public class SaleInvoiceDto
    {
        public SaleInvoice Invoice { get; set; }

        public List<SaleInvoiceDetail> Details { get; set; }
    }
}
