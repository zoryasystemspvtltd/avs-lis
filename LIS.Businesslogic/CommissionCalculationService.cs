using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;

namespace LIS.BusinessLogic
{
    /// <summary>
    /// Calculates commission for eligible invoices. Does not persist amounts and is not called by invoice save.
    /// </summary>
    public class CommissionCalculationService
    {
        public const string ImplementationMonthKey = "Commission:ImplementationMonth";

        private readonly ModuleRepo<SaleInvoice> invoiceRepo;
        private readonly ModuleRepo<ReferralDoctorCommissionRate> doctorRateRepo;
        private readonly ModuleRepo<SampleCollectorCommissionRate> collectorRateRepo;
        private readonly ModuleRepo<ReferralDoctorMaster> doctorRepo;
        private readonly ModuleRepo<SampleCollectorMaster> collectorRepo;

        public CommissionCalculationService(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
        {
            invoiceRepo = new ModuleRepo<SaleInvoice>(logger, identity, uow);
            doctorRateRepo = new ModuleRepo<ReferralDoctorCommissionRate>(logger, identity, uow);
            collectorRateRepo = new ModuleRepo<SampleCollectorCommissionRate>(logger, identity, uow);
            doctorRepo = new ModuleRepo<ReferralDoctorMaster>(logger, identity, uow);
            collectorRepo = new ModuleRepo<SampleCollectorMaster>(logger, identity, uow);
        }

        public static DateTime ReadImplementationMonthStart()
        {
            var raw = ConfigurationManager.AppSettings[ImplementationMonthKey];
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new InvalidOperationException(
                    "Commission:ImplementationMonth is not configured. Set it to yyyy-MM before calculating commission.");
            }

            if (!DateTime.TryParseExact(raw.Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                throw new InvalidOperationException(
                    "Commission:ImplementationMonth must be yyyy-MM.");
            }

            return new DateTime(parsed.Year, parsed.Month, 1);
        }

        public CommissionCalculationResult Calculate(CommissionCalculationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var from = request.FromDate.Date;
            var to = request.ToDate.Date;
            if (from > to)
            {
                throw new ArgumentException("From date cannot be after To date.");
            }

            if (request.RecipientId.HasValue && !request.RecipientType.HasValue)
            {
                throw new ArgumentException("Recipient type is required when a recipient is selected.");
            }

            if (request.RecipientId.HasValue && request.RecipientId.Value <= 0)
            {
                throw new ArgumentException("Recipient was not found.");
            }

            var monthStart = ReadImplementationMonthStart();
            var rangeStart = from < monthStart ? monthStart : from;
            if (to < monthStart)
            {
                return Empty();
            }

            var invoices = LoadEligible(rangeStart, to.AddDays(1), request.RecipientType, request.RecipientId);
            var lines = new List<CommissionCalculationLine>();
            var exclusions = new List<CommissionRateExclusion>();
            if (invoices.Count == 0)
            {
                return Empty();
            }

            var doctorIds = invoices
                .Where(HasDoctor)
                .Select(i => i.ReferralDoctorId.Value)
                .Distinct()
                .ToList();
            var collectorIds = invoices
                .Where(i => !HasDoctor(i) && HasCollector(i))
                .Select(i => i.CollectorId.Value)
                .Distinct()
                .ToList();

            var doctorRates = doctorIds.Count == 0
                ? new List<ReferralDoctorCommissionRate>()
                : doctorRateRepo.Get(r => r.IsActive && doctorIds.Contains(r.ReferralDoctorId)).ToList();
            var collectorRates = collectorIds.Count == 0
                ? new List<SampleCollectorCommissionRate>()
                : collectorRateRepo.Get(r => r.IsActive && collectorIds.Contains(r.CollectorId)).ToList();
            var doctorNames = doctorIds.Count == 0
                ? new Dictionary<int, string>()
                : doctorRepo.Get(d => doctorIds.Contains(d.Id)).ToDictionary(d => d.Id, d => d.Name ?? string.Empty);
            var collectorNames = collectorIds.Count == 0
                ? new Dictionary<int, string>()
                : collectorRepo.Get(c => collectorIds.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Name ?? string.Empty);

            foreach (var invoice in invoices)
            {
                var day = invoice.InvoiceDate.Date;
                if (HasDoctor(invoice))
                {
                    if (request.RecipientType == CommissionRecipientType.SampleCollector)
                    {
                        continue;
                    }

                    var doctorId = invoice.ReferralDoctorId.Value;
                    if (request.RecipientType == CommissionRecipientType.ReferralDoctor &&
                        request.RecipientId.HasValue &&
                        request.RecipientId.Value != doctorId)
                    {
                        continue;
                    }

                    var doctorMatches = doctorRates.Where(r =>
                        r.ReferralDoctorId == doctorId &&
                        r.EffectiveFrom.Date <= day &&
                        (r.EffectiveTo == null || r.EffectiveTo.Value.Date >= day)).ToList();
                    AddRatedOrExclude(
                        request,
                        lines,
                        exclusions,
                        doctorMatches.Count,
                        doctorMatches.Count == 1 ? doctorMatches[0].CommissionPercent : 0m,
                        invoice,
                        CommissionRecipientType.ReferralDoctor,
                        doctorId,
                        doctorNames.ContainsKey(doctorId) ? doctorNames[doctorId] : string.Empty,
                        "referral doctor");
                    continue;
                }

                if (!HasCollector(invoice))
                {
                    continue;
                }

                if (request.RecipientType == CommissionRecipientType.ReferralDoctor)
                {
                    continue;
                }

                var collectorId = invoice.CollectorId.Value;
                if (request.RecipientId.HasValue && request.RecipientId.Value != collectorId)
                {
                    continue;
                }

                var collectorMatches = collectorRates.Where(r =>
                    r.CollectorId == collectorId &&
                    r.EffectiveFrom.Date <= day &&
                    (r.EffectiveTo == null || r.EffectiveTo.Value.Date >= day)).ToList();
                AddRatedOrExclude(
                    request,
                    lines,
                    exclusions,
                    collectorMatches.Count,
                    collectorMatches.Count == 1 ? collectorMatches[0].CommissionPercent : 0m,
                    invoice,
                    CommissionRecipientType.SampleCollector,
                    collectorId,
                    collectorNames.ContainsKey(collectorId) ? collectorNames[collectorId] : string.Empty,
                    "sample collector");
            }

            var ordered = lines
                .OrderBy(l => l.InvoiceDate)
                .ThenBy(l => l.InvoiceNo)
                .ThenBy(l => l.RecipientType)
                .ThenBy(l => l.RecipientId)
                .ToList();

            var orderedExclusions = exclusions
                .OrderBy(l => l.InvoiceDate)
                .ThenBy(l => l.InvoiceNo)
                .ThenBy(l => l.RecipientType)
                .ThenBy(l => l.RecipientId)
                .ToList();

            return new CommissionCalculationResult
            {
                Lines = ordered,
                TotalAmount = ordered.Sum(l => l.Amount),
                Exclusions = orderedExclusions
            };
        }

        private List<SaleInvoice> LoadEligible(
            DateTime rangeStart,
            DateTime rangeEndExclusive,
            CommissionRecipientType? recipientType,
            int? recipientId)
        {
            var paid = (int)PaymentStatusType.Paid;
            var confirmed = (int)InvoiceStatusType.Confirmed;
            var invoicePaid = (int)InvoiceStatusType.Paid;

            if (recipientType == CommissionRecipientType.ReferralDoctor && recipientId.HasValue)
            {
                var doctorId = recipientId.Value;
                return invoiceRepo.Get(i =>
                    i.PaymentStatus == paid &&
                    (i.InvoiceStatus == confirmed || i.InvoiceStatus == invoicePaid) &&
                    i.InvoiceDate >= rangeStart &&
                    i.InvoiceDate < rangeEndExclusive &&
                    i.ReferralDoctorId == doctorId).ToList();
            }

            if (recipientType == CommissionRecipientType.SampleCollector && recipientId.HasValue)
            {
                var collectorId = recipientId.Value;
                return invoiceRepo.Get(i =>
                    i.PaymentStatus == paid &&
                    (i.InvoiceStatus == confirmed || i.InvoiceStatus == invoicePaid) &&
                    i.InvoiceDate >= rangeStart &&
                    i.InvoiceDate < rangeEndExclusive &&
                    i.CollectorId == collectorId &&
                    (i.ReferralDoctorId == null || i.ReferralDoctorId <= 0)).ToList();
            }

            return invoiceRepo.Get(i =>
                i.PaymentStatus == paid &&
                (i.InvoiceStatus == confirmed || i.InvoiceStatus == invoicePaid) &&
                i.InvoiceDate >= rangeStart &&
                i.InvoiceDate < rangeEndExclusive &&
                ((i.ReferralDoctorId != null && i.ReferralDoctorId > 0) ||
                 (i.CollectorId != null && i.CollectorId > 0))).ToList();
        }

        private static CommissionCalculationLine BuildLine(
            SaleInvoice invoice,
            CommissionRecipientType recipientType,
            int recipientId,
            string recipientName,
            decimal percent)
        {
            var commissionBase = invoice.NetAmount - invoice.TaxAmount;
            var amount = Math.Round(commissionBase * percent / 100m, 2);
            return new CommissionCalculationLine
            {
                SaleInvoiceId = invoice.Id,
                InvoiceNo = invoice.InvoiceNo,
                InvoiceDate = invoice.InvoiceDate.Date,
                RecipientType = recipientType,
                RecipientId = recipientId,
                RecipientName = recipientName ?? string.Empty,
                CommissionBase = commissionBase,
                CommissionPercent = percent,
                Amount = amount
            };
        }

        private static void AddRatedOrExclude(
            CommissionCalculationRequest request,
            List<CommissionCalculationLine> lines,
            List<CommissionRateExclusion> exclusions,
            int matchCount,
            decimal percent,
            SaleInvoice invoice,
            CommissionRecipientType recipientType,
            int recipientId,
            string recipientName,
            string recipientLabel)
        {
            if (matchCount > 1)
            {
                throw new InvalidOperationException(
                    "More than one commission rate covers invoice " + invoice.InvoiceNo + ".");
            }

            if (matchCount == 0)
            {
                if (request.ReportMissingRatesAsExclusions)
                {
                    exclusions.Add(new CommissionRateExclusion
                    {
                        SaleInvoiceId = invoice.Id,
                        InvoiceNo = invoice.InvoiceNo,
                        InvoiceDate = invoice.InvoiceDate.Date,
                        RecipientType = recipientType,
                        RecipientId = recipientId,
                        RecipientName = recipientName ?? string.Empty,
                        Reason = "No commission rate is defined for the selected " + recipientLabel +
                            " on this invoice date."
                    });
                    return;
                }

                throw new InvalidOperationException(
                    "No commission rate is defined for the selected " + recipientLabel +
                    " on invoice " + invoice.InvoiceNo + " (" + invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ").");
            }

            lines.Add(BuildLine(invoice, recipientType, recipientId, recipientName, percent));
        }

        private static bool HasDoctor(SaleInvoice invoice)
        {
            return invoice.ReferralDoctorId.HasValue && invoice.ReferralDoctorId.Value > 0;
        }

        private static bool HasCollector(SaleInvoice invoice)
        {
            return invoice.CollectorId.HasValue && invoice.CollectorId.Value > 0;
        }

        private static CommissionCalculationResult Empty()
        {
            return new CommissionCalculationResult
            {
                Lines = new List<CommissionCalculationLine>(),
                TotalAmount = 0m,
                Exclusions = new List<CommissionRateExclusion>()
            };
        }
    }
}
