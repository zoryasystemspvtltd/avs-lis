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
    public class CommissionReportService
    {
        public const string SummaryFormat = "Summary";
        public const string DetailFormat = "Detail";

        private readonly CommissionCalculationService calculation;

        public CommissionReportService(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
        {
            calculation = new CommissionCalculationService(logger, identity, uow);
        }

        public CommissionReportPage Get(CommissionRecipientType recipientType, ReportFilterOptions options)
        {
            if (options == null || !options.FromDate.HasValue || !options.ToDate.HasValue)
            {
                throw new ArgumentException("From Date and To Date are required.");
            }

            var format = (options.CommissionFormat ?? string.Empty).Trim();
            var summary = string.Equals(format, SummaryFormat, StringComparison.OrdinalIgnoreCase);
            var detail = string.Equals(format, DetailFormat, StringComparison.OrdinalIgnoreCase);
            if (summary == detail)
            {
                throw new ArgumentException("Choose Summary or Detail.");
            }

            int? recipientId = null;
            if (recipientType == CommissionRecipientType.ReferralDoctor && options.ReferralDoctorId.HasValue && options.ReferralDoctorId.Value > 0)
            {
                recipientId = options.ReferralDoctorId;
            }
            else if (recipientType == CommissionRecipientType.SampleCollector && options.CollectorId.HasValue && options.CollectorId.Value > 0)
            {
                recipientId = options.CollectorId;
            }

            var calculated = calculation.Calculate(new CommissionCalculationRequest
            {
                FromDate = options.FromDate.Value,
                ToDate = options.ToDate.Value,
                RecipientType = recipientType,
                RecipientId = recipientId
            });

            var lines = calculated.Lines ?? new List<CommissionCalculationLine>();
            if (summary)
            {
                var groups = lines
                    .GroupBy(l => l.RecipientId)
                    .Select(g => new CommissionSummaryRow
                    {
                        RecipientType = recipientType,
                        RecipientId = g.Key,
                        RecipientName = g.First().RecipientName,
                        InvoiceCount = g.Count(),
                        Amount = g.Sum(x => x.Amount)
                    })
                    .OrderBy(r => r.RecipientName)
                    .ToList();
                var page = Page(groups, options.CurrentPage, options.RecordPerPage);
                return new CommissionReportPage
                {
                    Format = SummaryFormat,
                    TotalRecord = groups.Count,
                    TotalAmount = groups.Sum(g => g.Amount),
                    Summary = page,
                    Detail = null
                };
            }

            var detailPage = Page(lines, options.CurrentPage, options.RecordPerPage);
            return new CommissionReportPage
            {
                Format = DetailFormat,
                TotalRecord = lines.Count,
                TotalAmount = lines.Sum(l => l.Amount),
                Summary = null,
                Detail = detailPage
            };
        }

        private static List<T> Page<T>(List<T> rows, int currentPage, int recordPerPage)
        {
            if (recordPerPage <= 0)
            {
                return rows;
            }

            var page = currentPage <= 0 ? 1 : currentPage;
            return rows.Skip((page - 1) * recordPerPage).Take(recordPerPage).ToList();
        }
    }
}
