using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DtoModel.Models
{
    public class TestNameItem
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
    }

    public class TestPanelMapping
    {
        public int EquipmentId { get; set; }
        public string HISTestCode { get; set; }
        public string HISTestCodeDescription { get; set; }
        public bool IsActive { get; set; }
        public string DepartmentCode { get; set; }
        public List<TestNameItem> LisTests { get; set; }
    }

    
}
