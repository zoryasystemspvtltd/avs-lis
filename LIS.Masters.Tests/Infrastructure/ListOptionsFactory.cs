using LIS.DtoModel.Models;

namespace LIS.Masters.Tests.Infrastructure
{
    internal static class ListOptionsFactory
    {
        public static ListOptions Create(string sortColumn = "Name", int page = 1, int pageSize = 50, string search = null)
        {
            return new ListOptions
            {
                CurrentPage = page,
                RecordPerPage = pageSize,
                SortColumnName = sortColumn,
                SortDirection = true,
                SearchText = search
            };
        }

        public static ListOptions ForHisTest() => Create("HISTestCode", 1, 100);

        public static ListOptions ForTestRate() => Create("EffectiveStart", 1, 100);

        public static ListOptions ForSaleInvoice() => Create("InvoiceDate", 1, 100);
    }
}
