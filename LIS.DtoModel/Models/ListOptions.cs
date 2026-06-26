using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DtoModel.Models
{
    public class ListOptions
    {
        public int RecordPerPage { get; set; }

        public int CurrentPage { get; set; }

        public string SortColumnName { get; set; }

        public bool SortDirection { get; set; }

        public ReportStatusType Status { get; set; }

        public string SearchText { get; set; }

        public bool ReceivedOnly { get; set; }

        /// <summary>Department code filter for billable test search (sale invoice UI).</summary>
        public string DepartmentCode { get; set; }

        /// <summary>Billable lookup mode: test | profile.</summary>
        public string BillableItemType { get; set; }
    }
}
