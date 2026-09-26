using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LIS.BusinessLogic
{
    public class ReferralDoctorCommissionRateManager : MasterCrudManager<ReferralDoctorCommissionRate>, IMasterCrudManager<ReferralDoctorCommissionRate>
    {
        private readonly ModuleRepo<ReferralDoctorMaster> doctorRepo;

        public ReferralDoctorCommissionRateManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow,
                x => x.CommissionPercent.ToString(CultureInfo.InvariantCulture),
                x => x.RecipientName ?? string.Empty,
                x => x.IsActive,
                "EffectiveFrom")
        {
            doctorRepo = new ModuleRepo<ReferralDoctorMaster>(logger, identity, uow);
        }

        public override long Add(ReferralDoctorCommissionRate item)
        {
            Prepare(item, true);
            EnsureDoctor(item.ReferralDoctorId);
            item.IsLocked = false;
            RejectOverlap(item, null);
            Stamp(item, true);
            return base.Add(item);
        }

        public override void Update(ReferralDoctorCommissionRate item)
        {
            if (item == null || item.Id <= 0)
            {
                throw new ArgumentException("Invalid commission rate.");
            }

            var existing = Repo.Get(item.Id);
            if (existing == null)
            {
                throw new InvalidOperationException("Commission rate not found.");
            }

            if (existing.IsLocked)
            {
                throw new InvalidOperationException("This commission rate period is locked and cannot be changed.");
            }

            existing.ReferralDoctorId = item.ReferralDoctorId;
            existing.CommissionPercent = item.CommissionPercent;
            existing.EffectiveFrom = item.EffectiveFrom;
            existing.EffectiveTo = item.EffectiveTo;
            existing.IsActive = item.IsActive;
            Prepare(existing, false);
            EnsureDoctor(existing.ReferralDoctorId);
            RejectOverlap(existing, existing.Id);
            Stamp(existing, false);
            base.Update(existing);
        }

        public override void Delete(ReferralDoctorCommissionRate item)
        {
            var existing = item == null ? null : Repo.Get(item.Id);
            if (existing == null)
            {
                return;
            }

            if (existing.IsLocked)
            {
                throw new InvalidOperationException("A locked commission rate cannot be deactivated.");
            }

            existing.IsActive = false;
            Stamp(existing, false);
            base.Update(existing);
        }

        public override ReferralDoctorCommissionRate GetById(int id)
        {
            var row = Repo.Get(id);
            if (row == null)
            {
                return null;
            }

            Enrich(new List<ReferralDoctorCommissionRate> { row });
            return row;
        }

        public override IEnumerable<ReferralDoctorCommissionRate> GetAllActive()
        {
            var rows = Repo.Get(r => r.IsActive).OrderBy(r => r.EffectiveFrom).ToList();
            Enrich(rows);
            return rows;
        }

        public override ItemList<ReferralDoctorCommissionRate> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            var rows = Repo.Get().ToList();
            Enrich(rows);
            if (!string.IsNullOrWhiteSpace(option.SearchText))
            {
                var search = option.SearchText.Trim();
                rows = rows.Where(r =>
                    (r.RecipientName != null && r.RecipientName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    r.CommissionPercent.ToString(CultureInfo.InvariantCulture).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            var sortDesc = option.SortDirection == false;
            IEnumerable<ReferralDoctorCommissionRate> sorted = rows.OrderBy(r => r.EffectiveFrom);
            var column = option.SortColumnName ?? string.Empty;
            if (column.Equals("CommissionPercent", StringComparison.OrdinalIgnoreCase))
            {
                sorted = sortDesc ? rows.OrderByDescending(r => r.CommissionPercent) : rows.OrderBy(r => r.CommissionPercent);
            }
            else if (column.Equals("RecipientName", StringComparison.OrdinalIgnoreCase))
            {
                sorted = sortDesc ? rows.OrderByDescending(r => r.RecipientName) : rows.OrderBy(r => r.RecipientName);
            }
            else if (sortDesc)
            {
                sorted = rows.OrderByDescending(r => r.EffectiveFrom);
            }

            var list = sorted.ToList();
            var pageSize = option.RecordPerPage <= 0 ? list.Count : option.RecordPerPage;
            var page = option.CurrentPage <= 0 ? 1 : option.CurrentPage;
            return new ItemList<ReferralDoctorCommissionRate>
            {
                TotalRecord = list.Count,
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            };
        }

        /// <summary>
        /// Splits rates at a closed payout end date. Called by payout confirmation in a later stage.
        /// </summary>
        public void ApplyClosedBoundary(int referralDoctorId, DateTime closedTo)
        {
            closedTo = closedTo.Date;
            var rates = Repo.Get(r => r.IsActive && r.ReferralDoctorId == referralDoctorId).ToList();
            foreach (var rate in rates.ToList())
            {
                var from = rate.EffectiveFrom.Date;
                var to = CommissionRateRules.NormalizeEnd(rate.EffectiveTo);
                if (from > closedTo)
                {
                    continue;
                }

                if (rate.IsLocked)
                {
                    if (!to.HasValue || to.Value > closedTo)
                    {
                        throw new InvalidOperationException("A locked commission rate extends past the closed period.");
                    }

                    continue;
                }

                if (to.HasValue && to.Value <= closedTo)
                {
                    rate.IsLocked = true;
                    Stamp(rate, false);
                    base.Update(rate);
                    continue;
                }

                var oldTo = rate.EffectiveTo;
                rate.EffectiveTo = closedTo;
                rate.IsLocked = true;
                Stamp(rate, false);
                base.Update(rate);

                var open = new ReferralDoctorCommissionRate
                {
                    ReferralDoctorId = rate.ReferralDoctorId,
                    CommissionPercent = rate.CommissionPercent,
                    EffectiveFrom = closedTo.AddDays(1),
                    EffectiveTo = oldTo,
                    IsActive = true,
                    IsLocked = false
                };
                Stamp(open, true);
                base.Add(open);
            }
        }

        private void Prepare(ReferralDoctorCommissionRate item, bool isNew)
        {
            if (item == null)
            {
                throw new ArgumentException("Commission rate is required.");
            }

            item.EffectiveFrom = item.EffectiveFrom.Date;
            item.EffectiveTo = CommissionRateRules.NormalizeEnd(item.EffectiveTo);
            CommissionRateRules.ValidatePercent(item.CommissionPercent);
            CommissionRateRules.ValidatePeriod(item.EffectiveFrom, item.EffectiveTo);
            if (isNew)
            {
                item.IsActive = true;
            }
        }

        private void EnsureDoctor(int doctorId)
        {
            if (doctorId <= 0 || doctorRepo.Get(doctorId) == null)
            {
                throw new ArgumentException("Selected referral doctor was not found.");
            }
        }

        private void RejectOverlap(ReferralDoctorCommissionRate item, int? excludeId)
        {
            if (!item.IsActive)
            {
                return;
            }

            var overlap = Repo.Get(r => r.IsActive && r.ReferralDoctorId == item.ReferralDoctorId
                    && (!excludeId.HasValue || r.Id != excludeId.Value))
                .AsEnumerable()
                .Any(r => CommissionRateRules.Overlaps(item.EffectiveFrom, item.EffectiveTo, r.EffectiveFrom, r.EffectiveTo));
            if (overlap)
            {
                throw new InvalidOperationException("An active commission rate already covers this period for the referral doctor.");
            }
        }

        private void Enrich(List<ReferralDoctorCommissionRate> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            var names = doctorRepo.Get().ToDictionary(d => d.Id, d => d.Name);
            foreach (var row in rows)
            {
                names.TryGetValue(row.ReferralDoctorId, out var name);
                row.RecipientName = name;
            }
        }

        private void Stamp(ReferralDoctorCommissionRate item, bool isNew)
        {
            var now = DateTime.Now;
            if (isNew)
            {
                item.CreatedOn = now;
                item.CreatedBy = Identity.ActivityMember;
            }

            item.ModifiedOn = now;
            item.ModifiedBy = Identity.ActivityMember;
        }
    }

    public class SampleCollectorCommissionRateManager : MasterCrudManager<SampleCollectorCommissionRate>, IMasterCrudManager<SampleCollectorCommissionRate>
    {
        private readonly ModuleRepo<SampleCollectorMaster> collectorRepo;

        public SampleCollectorCommissionRateManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow,
                x => x.CommissionPercent.ToString(CultureInfo.InvariantCulture),
                x => x.RecipientName ?? string.Empty,
                x => x.IsActive,
                "EffectiveFrom")
        {
            collectorRepo = new ModuleRepo<SampleCollectorMaster>(logger, identity, uow);
        }

        public override long Add(SampleCollectorCommissionRate item)
        {
            Prepare(item, true);
            EnsureCollector(item.CollectorId);
            item.IsLocked = false;
            RejectOverlap(item, null);
            Stamp(item, true);
            return base.Add(item);
        }

        public override void Update(SampleCollectorCommissionRate item)
        {
            if (item == null || item.Id <= 0)
            {
                throw new ArgumentException("Invalid commission rate.");
            }

            var existing = Repo.Get(item.Id);
            if (existing == null)
            {
                throw new InvalidOperationException("Commission rate not found.");
            }

            if (existing.IsLocked)
            {
                throw new InvalidOperationException("This commission rate period is locked and cannot be changed.");
            }

            existing.CollectorId = item.CollectorId;
            existing.CommissionPercent = item.CommissionPercent;
            existing.EffectiveFrom = item.EffectiveFrom;
            existing.EffectiveTo = item.EffectiveTo;
            existing.IsActive = item.IsActive;
            Prepare(existing, false);
            EnsureCollector(existing.CollectorId);
            RejectOverlap(existing, existing.Id);
            Stamp(existing, false);
            base.Update(existing);
        }

        public override void Delete(SampleCollectorCommissionRate item)
        {
            var existing = item == null ? null : Repo.Get(item.Id);
            if (existing == null)
            {
                return;
            }

            if (existing.IsLocked)
            {
                throw new InvalidOperationException("A locked commission rate cannot be deactivated.");
            }

            existing.IsActive = false;
            Stamp(existing, false);
            base.Update(existing);
        }

        public override SampleCollectorCommissionRate GetById(int id)
        {
            var row = Repo.Get(id);
            if (row == null)
            {
                return null;
            }

            Enrich(new List<SampleCollectorCommissionRate> { row });
            return row;
        }

        public override IEnumerable<SampleCollectorCommissionRate> GetAllActive()
        {
            var rows = Repo.Get(r => r.IsActive).OrderBy(r => r.EffectiveFrom).ToList();
            Enrich(rows);
            return rows;
        }

        public override ItemList<SampleCollectorCommissionRate> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            var rows = Repo.Get().ToList();
            Enrich(rows);
            if (!string.IsNullOrWhiteSpace(option.SearchText))
            {
                var search = option.SearchText.Trim();
                rows = rows.Where(r =>
                    (r.RecipientName != null && r.RecipientName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    r.CommissionPercent.ToString(CultureInfo.InvariantCulture).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            var sortDesc = option.SortDirection == false;
            IEnumerable<SampleCollectorCommissionRate> sorted = rows.OrderBy(r => r.EffectiveFrom);
            var column = option.SortColumnName ?? string.Empty;
            if (column.Equals("CommissionPercent", StringComparison.OrdinalIgnoreCase))
            {
                sorted = sortDesc ? rows.OrderByDescending(r => r.CommissionPercent) : rows.OrderBy(r => r.CommissionPercent);
            }
            else if (column.Equals("RecipientName", StringComparison.OrdinalIgnoreCase))
            {
                sorted = sortDesc ? rows.OrderByDescending(r => r.RecipientName) : rows.OrderBy(r => r.RecipientName);
            }
            else if (sortDesc)
            {
                sorted = rows.OrderByDescending(r => r.EffectiveFrom);
            }

            var list = sorted.ToList();
            var pageSize = option.RecordPerPage <= 0 ? list.Count : option.RecordPerPage;
            var page = option.CurrentPage <= 0 ? 1 : option.CurrentPage;
            return new ItemList<SampleCollectorCommissionRate>
            {
                TotalRecord = list.Count,
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            };
        }

        public void ApplyClosedBoundary(int collectorId, DateTime closedTo)
        {
            closedTo = closedTo.Date;
            var rates = Repo.Get(r => r.IsActive && r.CollectorId == collectorId).ToList();
            foreach (var rate in rates.ToList())
            {
                var from = rate.EffectiveFrom.Date;
                var to = CommissionRateRules.NormalizeEnd(rate.EffectiveTo);
                if (from > closedTo)
                {
                    continue;
                }

                if (rate.IsLocked)
                {
                    if (!to.HasValue || to.Value > closedTo)
                    {
                        throw new InvalidOperationException("A locked commission rate extends past the closed period.");
                    }

                    continue;
                }

                if (to.HasValue && to.Value <= closedTo)
                {
                    rate.IsLocked = true;
                    Stamp(rate, false);
                    base.Update(rate);
                    continue;
                }

                var oldTo = rate.EffectiveTo;
                rate.EffectiveTo = closedTo;
                rate.IsLocked = true;
                Stamp(rate, false);
                base.Update(rate);

                var open = new SampleCollectorCommissionRate
                {
                    CollectorId = rate.CollectorId,
                    CommissionPercent = rate.CommissionPercent,
                    EffectiveFrom = closedTo.AddDays(1),
                    EffectiveTo = oldTo,
                    IsActive = true,
                    IsLocked = false
                };
                Stamp(open, true);
                base.Add(open);
            }
        }

        private void Prepare(SampleCollectorCommissionRate item, bool isNew)
        {
            if (item == null)
            {
                throw new ArgumentException("Commission rate is required.");
            }

            item.EffectiveFrom = item.EffectiveFrom.Date;
            item.EffectiveTo = CommissionRateRules.NormalizeEnd(item.EffectiveTo);
            CommissionRateRules.ValidatePercent(item.CommissionPercent);
            CommissionRateRules.ValidatePeriod(item.EffectiveFrom, item.EffectiveTo);
            if (isNew)
            {
                item.IsActive = true;
            }
        }

        private void EnsureCollector(int collectorId)
        {
            if (collectorId <= 0 || collectorRepo.Get(collectorId) == null)
            {
                throw new ArgumentException("Selected sample collector was not found.");
            }
        }

        private void RejectOverlap(SampleCollectorCommissionRate item, int? excludeId)
        {
            if (!item.IsActive)
            {
                return;
            }

            var overlap = Repo.Get(r => r.IsActive && r.CollectorId == item.CollectorId
                    && (!excludeId.HasValue || r.Id != excludeId.Value))
                .AsEnumerable()
                .Any(r => CommissionRateRules.Overlaps(item.EffectiveFrom, item.EffectiveTo, r.EffectiveFrom, r.EffectiveTo));
            if (overlap)
            {
                throw new InvalidOperationException("An active commission rate already covers this period for the sample collector.");
            }
        }

        private void Enrich(List<SampleCollectorCommissionRate> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            var names = collectorRepo.Get().ToDictionary(d => d.Id, d => d.Name);
            foreach (var row in rows)
            {
                names.TryGetValue(row.CollectorId, out var name);
                row.RecipientName = name;
            }
        }

        private void Stamp(SampleCollectorCommissionRate item, bool isNew)
        {
            var now = DateTime.Now;
            if (isNew)
            {
                item.CreatedOn = now;
                item.CreatedBy = Identity.ActivityMember;
            }

            item.ModifiedOn = now;
            item.ModifiedBy = Identity.ActivityMember;
        }
    }
}
