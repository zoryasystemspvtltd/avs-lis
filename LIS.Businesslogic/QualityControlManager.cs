using LIS.Businesslogic;
using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.BusinessLogic
{
    public class QualityControlManager : IQualityControlManager
    {
        private ILogger logger;
        private ModuleRepo<ControlResult> controlResultRepo;
        private ModuleRepo<ControlResultDetails> controlResultDetailsRepo;
        private ModuleRepo<EquipmentMaster> equpmentRepo;
        private IModuleIdentity identity;
        private GenericUnitOfWork genericUnitOfWork;
        private IFileHandler file;

        public QualityControlManager(ILogger Logger, IModuleIdentity identity, GenericUnitOfWork genericUnitOfWork, IFileHandler file)
        {
            this.identity = identity;
            logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
            controlResultRepo = new ModuleRepo<ControlResult>(logger, this.identity, this.genericUnitOfWork);
            controlResultDetailsRepo = new ModuleRepo<ControlResultDetails>(logger, this.identity, this.genericUnitOfWork);
            equpmentRepo = new ModuleRepo<EquipmentMaster>(logger, this.identity, this.genericUnitOfWork);
            this.file = file;
        }

        public dynamic Get(string controlId)
        {
            var temp = controlId.Split('~');
            var year = Convert.ToInt32(temp[0]);
            var month = Convert.ToInt32(temp[1]);
            var equipmentId = Convert.ToInt32(temp[2]);
            var sampleNo = temp[3];

            var equipment = equpmentRepo.Get(p => p.Id.Equals(equipmentId)).FirstOrDefault();

            var equipmentName = equipment?.Name;
            var days = GetDays(year, month);

            var code = controlResultRepo.Get(r => r.EquipmentId == equipmentId && r.SampleNo.Equals(sampleNo, StringComparison.CurrentCultureIgnoreCase))
                .Join(controlResultDetailsRepo.Get(d => d.CreatedOn.Year == year && d.CreatedOn.Month == month),
                    result => result.Id,
                    detail => detail.ControlResultId,
                     (result, detail) => new { detail.LISParamCode,result.EquipmentId }
                )
                .Join(equpmentRepo.Get(d => d.IsActive),
                    result => result.EquipmentId,
                    eq => eq.Id,
                     (result, eq) => new {  result.LISParamCode, eq.Model}
                )
                .Distinct()
                .ToList();
            

            var details = new List<dynamic>();
            var model = code.FirstOrDefault().Model;
            var availableTest = file.GetJsonMappings(model);

            foreach (var item in code)
            {
               var data = controlResultDetailsRepo.Get(d => d.CreatedOn.Year == year
                        && d.CreatedOn.Month == month
                        && d.LISParamCode.Equals(item.LISParamCode, StringComparison.OrdinalIgnoreCase))

                    .Select(detail => new { detail.LISParamValue, day = detail.CreatedOn.Day })
                    .Distinct()
                    .ToList();

                var testName = availableTest
                        .FirstOrDefault(p => p.Code.Equals(item.LISParamCode, StringComparison.OrdinalIgnoreCase))?.Description;
                if (!string.IsNullOrEmpty(testName))
                {
                    details.Add(new
                    {
                        LISParamCode = item.LISParamCode,
                        LISParamValues = data,
                        TestName = testName
                    });
                }
            }

            var control = new
            {
                Id = controlId,
                SampleNo = sampleNo,
                EquipmentName = equipmentName,
                MonthName = GetMonthName(year + ":" + month),
                details = details,
                days = days
            };
            
            return control;

            /*
            var controlResult = controlResultRepo.Get(p => p.Id.Equals(ResultId)).FirstOrDefault();
            var equipment = equpmentRepo.Get(p => p.Id.Equals(controlResult.EquipmentId)).FirstOrDefault();

            controlResult.EquipmentName = equipment.Name;

            var controlResultDetails = controlResultDetailsRepo.Get(p => p.ControlResultId.Equals(ResultId)).ToList();

            var result = new QualityControlResult
            {
                ControlResult = controlResult,
                ControlResultDetails = controlResultDetails
            };

            return result;
            */
        }

        public static List<int> GetDays(int year, int month)
        {
            var dates = new List<int>();

            // Loop from the first day of the month until we hit the next month, moving forward a day at a time
            for (var date = new DateTime(year, month, 1); date.Month == month; date = date.AddDays(1))
            {
                dates.Add(date.Day);
            }

            return dates;
        }
        private string GetMonthName(string name)
        {
            var temp = name.Split(':');
            var month = temp[1];
            switch (month)
            {
                case "1":
                    return temp[0] + ": January";
                case "2":
                    return temp[0] + ":February";
                case "3":
                    return temp[0] + ":March";
                case "4":
                    return temp[0] + ":April";
                case "5":
                    return temp[0] + ":May";
                case "6":
                    return temp[0] + ":June";
                case "7":
                    return temp[0] + ":July";
                case "8":
                    return temp[0] + ":August";
                case "9":
                    return temp[0] + ":September";
                case "10":
                    return temp[0] + ":October";
                case "11":
                    return temp[0] + ":November";
                case "12":
                    return temp[0] + ":December";

                default:
                    return "";
            }
        }
        public ItemList<dynamic> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            var results = new ItemList<dynamic>();

            var controls = controlResultRepo.Get(r => r.Id > 0)
                .Join(controlResultDetailsRepo.Get(d => d.Id > 0),
                    result => result.Id,
                    detail => detail.ControlResultId,
                     (result, detail) => result
                )
                .Join(equpmentRepo.Get(d => d.IsActive),
                    result => result.EquipmentId,
                    eq => eq.Id,
                     (result, eq) => new { Id = result.CreatedOn.Year + "~" + result.CreatedOn.Month + "~" + eq.Id + "~" + result.SampleNo, result.SampleNo, EquipmentName = eq.Name, MonthName = result.CreatedOn.Year + ":" + result.CreatedOn.Month }
                )
                .Distinct()
                .ToList();

            results.Items = controls
                .Select(p => new { p.Id, p.SampleNo, p.EquipmentName, MonthName = GetMonthName(p.MonthName) });

            results.TotalRecord = results.Items.Count();
            return results;
            /*
            ItemList<ControlResult> result = new ItemList<ControlResult>();
            var query = controlResultRepo.Get();
            foreach (var item in query)
            {
                var equipment = equpmentRepo.Get(p => p.Id.Equals(item.EquipmentId)).FirstOrDefault();
                item.EquipmentName = equipment.Name;
            }


            if (!string.IsNullOrEmpty(option.SearchText))
            {
                DateTime searchDate;
                bool isValidSearchDate = DateTime.TryParse(option.SearchText, out searchDate);
                if (option.SearchText.Contains("/") && isValidSearchDate)
                {
                    query = query.Where(p => p.ResultDate.Year == searchDate.Year
                                                && p.ResultDate.Month == searchDate.Month
                                                && p.ResultDate.Day == searchDate.Day);
                }
                else
                {
                    query = query.Where(p => p.SampleNo.Contains(option.SearchText)
                                || p.EquipmentName.Contains(option.SearchText));
                }
            }

            result.TotalRecord = query.Count();

            int minRow = (option.CurrentPage - 1) * option.RecordPerPage;
            int maxRow = (option.CurrentPage) * option.RecordPerPage;

            int totalRecordToBeSelected = ((result.TotalRecord - minRow) > option.RecordPerPage)
                ? option.RecordPerPage : (result.TotalRecord - minRow);

            option.SortColumnName = string.IsNullOrEmpty(option.SortColumnName)
                ? "ResultDate" : option.SortColumnName;

            //TODO SQL View
            if (!option.SortColumnName.Equals("SampleNo", StringComparison.OrdinalIgnoreCase)
                && !option.SortColumnName.Equals("EquipmentName", StringComparison.OrdinalIgnoreCase)
                && !option.SortColumnName.Equals("ResultDate", StringComparison.OrdinalIgnoreCase))
            {
                option.SortColumnName = "ResultDate";
                option.SortDirection = false;
            }

            if (option.RecordPerPage == 0)
            {
                totalRecordToBeSelected = result.TotalRecord;
            }

            result.Items = query
                    .OrderBy(option.SortColumnName, option.SortDirection)
                    .Skip(minRow)
                    .Take(totalRecordToBeSelected)
                    .ToList();

            return result;
            */
        }

        public List<ControlResultDetails> GetQualityMonthwiseData(string pramCode)
        {
            int month = 0, year = 0;
            DateTime dt = new DateTime();
            month = dt.Month;
            year = dt.Year;
            var controlResultDetails = controlResultDetailsRepo.Get(p =>
            p.LISParamCode.Equals(pramCode, StringComparison.OrdinalIgnoreCase)
            && p.CreatedOn.Month.Equals(month)
            && p.CreatedOn.Year.Equals(year)).ToList();

            return controlResultDetails;
        }
    }
}
