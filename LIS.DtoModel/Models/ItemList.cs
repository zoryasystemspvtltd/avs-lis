using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DtoModel.Models
{
    public class ItemList<T>
       where T : class
    {
        public int TotalRecord { get; set; }

        public IEnumerable<T> Items { get; set; }
    }
}
