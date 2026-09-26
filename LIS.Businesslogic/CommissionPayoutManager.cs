using LIS.BusinessLogic.Helper;
using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;

namespace LIS.BusinessLogic
{
    public class CommissionPayoutManager
    {
        private readonly ILogger logger;
        private readonly IModuleIdentity identity;
        private readonly GenericUnitOfWork unitOfWork;
        private readonly CommissionCalculationService calculation;
        private readonly ModuleRepo<CommissionPayout> payoutRepo;
        private readonly ModuleRepo<CommissionPayoutDetail> detailRepo;
        private readonly ModuleRepo<ReferralDoctorMaster> doctorRepo;
        private readonly ModuleRepo<SampleCollectorMaster> collectorRepo;

        public CommissionPayoutManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
        {
            this.logger = logger;
            this.identity = identity;
            unitOfWork = uow;
            calculation = new CommissionCalculationService(logger, identity, uow);
            payoutRepo = new ModuleRepo<CommissionPayout>(logger, identity, uow);
            detailRepo = new ModuleRepo<CommissionPayoutDetail>(logger, identity, uow);
            doctorRepo = new ModuleRepo<ReferralDoctorMaster>(logger, identity, uow);
            collectorRepo = new ModuleRepo<SampleCollectorMaster>(logger, identity, uow);
        }

        public CommissionPayoutSchedule GetSchedule(int recipientType, int recipientId)
        {
            EnsureRecipient(recipientType, recipientId);
            var required = RequiredFrom(recipientType, recipientId, out var first);
            return new CommissionPayoutSchedule
            {
                FromDate = required,
                IsFirstPayout = first,
                ImplementationMonth = CommissionCalculationService.ReadImplementationMonthStart()
            };
        }

        public CommissionPayoutPreview Preview(CommissionPayoutRequest request)
        {
            var period = Resolve(request);
            var lines = EligibleLines(period.Type, period.RecipientId, period.From, period.To);
            return new CommissionPayoutPreview
            {
                RecipientType = period.Type,
                RecipientId = period.RecipientId,
                RecipientName = RecipientName(period.Type, period.RecipientId),
                FromDate = period.From,
                ToDate = period.To,
                IsFirstPayout = period.IsFirst,
                Lines = lines,
                TotalAmount = lines.Sum(l => l.Amount)
            };
        }

        public long Confirm(CommissionPayoutRequest request)
        {
            using (var transaction = unitOfWork.BeginTransaction())
            {
                try
                {
                    var period = Resolve(request);
                    var lines = EligibleLines(period.Type, period.RecipientId, period.From, period.To);
                    if (lines.Count == 0)
                    {
                        throw new InvalidOperationException("No eligible invoices for this payout.");
                    }

                    var already = PaidInvoiceIds(period.Type, period.RecipientId);
                    var duplicate = lines.FirstOrDefault(l => already.Contains(l.SaleInvoiceId));
                    if (duplicate != null)
                    {
                        throw new InvalidOperationException(
                            "Invoice " + duplicate.InvoiceNo + " is already in a payout for this recipient.");
                    }

                    var payout = new CommissionPayout
                    {
                        PayoutNo = NextPayoutNo(),
                        RecipientType = period.Type,
                        RecipientId = period.RecipientId,
                        FromDate = period.From,
                        ToDate = period.To,
                        TotalAmount = lines.Sum(l => l.Amount),
                        CreatedOn = DateTime.Now,
                        CreatedBy = identity?.ActivityMember
                    };
                    var id = payoutRepo.Add(payout);

                    foreach (var line in lines)
                    {
                        detailRepo.Add(new CommissionPayoutDetail
                        {
                            CommissionPayoutId = id,
                            RecipientType = period.Type,
                            RecipientId = period.RecipientId,
                            SaleInvoiceId = line.SaleInvoiceId,
                            InvoiceNo = line.InvoiceNo,
                            InvoiceDate = line.InvoiceDate.Date,
                            CommissionBase = line.CommissionBase,
                            CommissionPercent = line.CommissionPercent,
                            Amount = line.Amount
                        });
                    }

                    if (period.Type == (int)CommissionRecipientType.ReferralDoctor)
                    {
                        new ReferralDoctorCommissionRateManager(logger, identity, unitOfWork)
                            .ApplyClosedBoundary(period.RecipientId, period.To);
                    }
                    else
                    {
                        new SampleCollectorCommissionRateManager(logger, identity, unitOfWork)
                            .ApplyClosedBoundary(period.RecipientId, period.To);
                    }

                    transaction.Commit();
                    return id;
                }
                catch (DbUpdateException)
                {
                    transaction.Rollback();
                    throw new InvalidOperationException("This payout period was already confirmed.");
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public List<CommissionPayout> List(int? recipientType, int? recipientId)
        {
            var rows = payoutRepo.Get().ToList();
            if (recipientType.HasValue)
            {
                rows = rows.Where(p => p.RecipientType == recipientType.Value).ToList();
            }

            if (recipientId.HasValue && recipientId.Value > 0)
            {
                rows = rows.Where(p => p.RecipientId == recipientId.Value).ToList();
            }

            foreach (var row in rows)
            {
                row.RecipientName = RecipientName(row.RecipientType, row.RecipientId);
            }

            return rows.OrderByDescending(p => p.FromDate).ThenByDescending(p => p.Id).ToList();
        }

        /// <summary>Stored header and details only. Does not recalculate.</summary>
        public CommissionPayoutMemo GetMemo(long id)
        {
            var payout = payoutRepo.Get(id);
            if (payout == null)
            {
                return null;
            }

            payout.RecipientName = RecipientName(payout.RecipientType, payout.RecipientId);
            var details = detailRepo.Get(d => d.CommissionPayoutId == id)
                .OrderBy(d => d.InvoiceDate)
                .ThenBy(d => d.InvoiceNo)
                .ToList();
            return new CommissionPayoutMemo
            {
                Payout = payout,
                Details = details
            };
        }

        private List<CommissionCalculationLine> EligibleLines(int recipientType, int recipientId, DateTime from, DateTime to)
        {
            var type = (CommissionRecipientType)recipientType;
            var monthStart = CommissionCalculationService.ReadImplementationMonthStart();
            var current = calculation.Calculate(new CommissionCalculationRequest
            {
                FromDate = from,
                ToDate = to,
                RecipientType = type,
                RecipientId = recipientId
            }).Lines ?? new List<CommissionCalculationLine>();

            var carry = new List<CommissionCalculationLine>();
            var carryTo = from.AddDays(-1);
            if (carryTo >= monthStart)
            {
                carry = calculation.Calculate(new CommissionCalculationRequest
                {
                    FromDate = monthStart,
                    ToDate = carryTo,
                    RecipientType = type,
                    RecipientId = recipientId
                }).Lines ?? carry;
            }

            var used = PaidInvoiceIds(recipientType, recipientId);
            return carry.Concat(current)
                .Where(l => !used.Contains(l.SaleInvoiceId))
                .GroupBy(l => l.SaleInvoiceId)
                .Select(g => g.First())
                .OrderBy(l => l.InvoiceDate)
                .ThenBy(l => l.InvoiceNo)
                .ToList();
        }

        private HashSet<long> PaidInvoiceIds(int recipientType, int recipientId)
        {
            return new HashSet<long>(detailRepo
                .Get(d => d.RecipientType == recipientType && d.RecipientId == recipientId)
                .Select(d => d.SaleInvoiceId));
        }

        private DateTime RequiredFrom(int recipientType, int recipientId, out bool first)
        {
            var monthStart = CommissionCalculationService.ReadImplementationMonthStart();
            var last = payoutRepo.Get(p => p.RecipientType == recipientType && p.RecipientId == recipientId)
                .OrderByDescending(p => p.ToDate)
                .FirstOrDefault();
            if (last == null)
            {
                first = true;
                return monthStart;
            }

            first = false;
            return last.ToDate.Date.AddDays(1);
        }

        private Period Resolve(CommissionPayoutRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            EnsureRecipient(request.RecipientType, request.RecipientId);
            if (request.ToDate == default(DateTime))
            {
                throw new ArgumentException("To date is required.");
            }

            var required = RequiredFrom(request.RecipientType, request.RecipientId, out var first);
            if (request.FromDate.HasValue && request.FromDate.Value.Date != required)
            {
                throw new ArgumentException(first
                    ? "The first payout From date is " + required.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ", the first day of the commission implementation month."
                    : "The From date must be " + required.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ", the day after the previous payout To date.");
            }

            var to = request.ToDate.Date;
            if (required > to)
            {
                throw new ArgumentException("From date cannot be after To date.");
            }

            return new Period
            {
                Type = request.RecipientType,
                RecipientId = request.RecipientId,
                From = required,
                To = to,
                IsFirst = first
            };
        }

        private void EnsureRecipient(int recipientType, int recipientId)
        {
            if (recipientId <= 0)
            {
                throw new ArgumentException("Recipient was not found.");
            }

            if (recipientType == (int)CommissionRecipientType.ReferralDoctor)
            {
                if (doctorRepo.Get(recipientId) == null)
                {
                    throw new ArgumentException("Selected referral doctor was not found.");
                }

                return;
            }

            if (recipientType == (int)CommissionRecipientType.SampleCollector)
            {
                if (collectorRepo.Get(recipientId) == null)
                {
                    throw new ArgumentException("Selected sample collector was not found.");
                }

                return;
            }

            throw new ArgumentException("Recipient type is required.");
        }

        private string RecipientName(int recipientType, int recipientId)
        {
            if (recipientType == (int)CommissionRecipientType.ReferralDoctor)
            {
                return doctorRepo.Get(recipientId)?.Name ?? string.Empty;
            }

            if (recipientType == (int)CommissionRecipientType.SampleCollector)
            {
                return collectorRepo.Get(recipientId)?.Name ?? string.Empty;
            }

            return string.Empty;
        }

        private string NextPayoutNo()
        {
            var prefix = "CP-" + OperationalDateTime.GetFacilityNow().ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "-";
            var last = payoutRepo.Get()
                .Where(p => p.PayoutNo != null && p.PayoutNo.StartsWith(prefix))
                .OrderByDescending(p => p.PayoutNo)
                .FirstOrDefault();
            var seq = 1;
            if (last != null)
            {
                var parts = last.PayoutNo.Split('-');
                if (parts.Length >= 3)
                {
                    int.TryParse(parts[parts.Length - 1], out seq);
                    seq++;
                }
            }

            return prefix + seq.ToString("D4", CultureInfo.InvariantCulture);
        }

        private sealed class Period
        {
            public int Type { get; set; }
            public int RecipientId { get; set; }
            public DateTime From { get; set; }
            public DateTime To { get; set; }
            public bool IsFirst { get; set; }
        }
    }
}
