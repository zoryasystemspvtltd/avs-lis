using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DtoModel.Models
{
    public enum ReportStatusType
    {
        New = 0,
        SentToEquipment = 1,
        ReportGenerated = 2,
        TechnicianApproved = 3,
        TechnicianRejected = 4,
        DoctorApproved = 5,
        DoctorRejected = 6,
        FinallyRejected = 7
    }
}
