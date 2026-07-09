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
    public class PatientVisitManager
    {
        private readonly ModuleRepo<PatientVisit> visitRepo;
        private readonly ModuleRepo<PatientDetail> patientRepo;
        private readonly ModuleRepo<SaleInvoice> invoiceRepo;
        private readonly IModuleIdentity identity;
        private readonly GenericUnitOfWork unitOfWork;

        public PatientVisitManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork unitOfWork)
        {
            this.identity = identity;
            this.unitOfWork = unitOfWork;
            visitRepo = new ModuleRepo<PatientVisit>(logger, identity, unitOfWork);
            patientRepo = new ModuleRepo<PatientDetail>(logger, identity, unitOfWork);
            invoiceRepo = new ModuleRepo<SaleInvoice>(logger, identity, unitOfWork);
        }

        public string GenerateNextVisitId()
        {
            var fromPatients = patientRepo.Get().Select(p => p.VisitId);
            var fromVisits = visitRepo.Get().Select(v => v.VisitId);
            return GenerateNextCode("VIS", 5, fromPatients.Concat(fromVisits));
        }

        public bool ExistsVisitIdForOtherPatients(string visitId, long? excludePatientId)
        {
            if (string.IsNullOrWhiteSpace(visitId))
            {
                return false;
            }

            var normalized = visitId.Trim();
            if (patientRepo.Get().AsEnumerable().Any(p =>
                p.IsActive &&
                (!excludePatientId.HasValue || p.Id != excludePatientId.Value) &&
                !string.IsNullOrWhiteSpace(p.VisitId) &&
                p.VisitId.Trim().Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return visitRepo.Get().AsEnumerable().Any(v =>
                v.IsActive &&
                (!excludePatientId.HasValue || v.PatientId != excludePatientId.Value) &&
                !string.IsNullOrWhiteSpace(v.VisitId) &&
                v.VisitId.Trim().Equals(normalized, StringComparison.OrdinalIgnoreCase));
        }

        public bool ExistsVisitId(string visitId, long? excludePatientVisitId = null)
        {
            if (string.IsNullOrWhiteSpace(visitId))
            {
                return false;
            }

            var normalized = visitId.Trim();
            if (patientRepo.Get().AsEnumerable().Any(p =>
                p.IsActive &&
                !string.IsNullOrWhiteSpace(p.VisitId) &&
                p.VisitId.Trim().Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return visitRepo.Get().AsEnumerable().Any(v =>
                v.IsActive &&
                (!excludePatientVisitId.HasValue || v.PatientVisitId != excludePatientVisitId.Value) &&
                !string.IsNullOrWhiteSpace(v.VisitId) &&
                v.VisitId.Trim().Equals(normalized, StringComparison.OrdinalIgnoreCase));
        }

        public long CreateVisitForPatient(long patientId, string visitId, long? saleInvoiceId, VisitStatusType status)
        {
            if (patientId <= 0)
            {
                throw new ArgumentException("Patient is required.");
            }

            if (string.IsNullOrWhiteSpace(visitId))
            {
                throw new ArgumentException("Visit ID is required.");
            }

            var normalizedVisitId = visitId.Trim();
            if (ExistsVisitIdForOtherPatients(normalizedVisitId, patientId))
            {
                throw new InvalidOperationException("Visit ID already exists.");
            }

            var now = DateTime.Now;
            var member = identity?.ActivityMember ?? "system";
            var visit = new PatientVisit
            {
                PatientId = patientId,
                VisitId = normalizedVisitId,
                VisitDateTime = now,
                SaleInvoiceId = saleInvoiceId,
                VisitStatus = (int)status,
                CreatedBy = member,
                CreatedOn = now,
                ModifiedBy = member,
                ModifiedOn = now,
                IsActive = true
            };

            visitRepo.Add(visit);
            return visit.PatientVisitId;
        }

        public PatientVisit StartVisit(long patientId)
        {
            using (var transaction = unitOfWork.BeginTransaction())
            {
                try
                {
                    var patient = patientRepo.Get(patientId);
                    if (patient == null)
                    {
                        throw new InvalidOperationException("Patient record not found.");
                    }

                    if (!patient.IsActive)
                    {
                        throw new InvalidOperationException("Patient is inactive.");
                    }

                    var visitId = GenerateNextVisitId();
                    for (var attempt = 0; attempt < 3 && ExistsVisitId(visitId); attempt++)
                    {
                        visitId = GenerateNextVisitId();
                    }

                    if (ExistsVisitId(visitId))
                    {
                        throw new InvalidOperationException("Unable to generate a unique Visit ID.");
                    }

                    var now = DateTime.Now;
                    var member = identity?.ActivityMember ?? "system";
                    var visit = new PatientVisit
                    {
                        PatientId = patientId,
                        VisitId = visitId,
                        VisitDateTime = now,
                        VisitStatus = (int)VisitStatusType.New,
                        CreatedBy = member,
                        CreatedOn = now,
                        ModifiedBy = member,
                        ModifiedOn = now,
                        IsActive = true
                    };

                    visitRepo.Add(visit);
                    patient.VisitId = visitId;
                    patientRepo.Update(patient);

                    transaction.Commit();
                    return visit;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public PatientVisit GetCurrentVisit(long patientId)
        {
            if (patientId <= 0)
            {
                return null;
            }

            var patient = patientRepo.Get(patientId);
            if (patient == null)
            {
                return null;
            }

            var visits = visitRepo.Get(v => v.PatientId == patientId && v.IsActive)
                .OrderByDescending(v => v.PatientVisitId)
                .ToList();

            if (!visits.Any())
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(patient.VisitId))
            {
                var current = visits.FirstOrDefault(v =>
                    v.VisitId != null &&
                    v.VisitId.Trim().Equals(patient.VisitId.Trim(), StringComparison.OrdinalIgnoreCase));
                if (current != null)
                {
                    return current;
                }
            }

            return visits.FirstOrDefault();
        }

        public IEnumerable<PatientVisitHistoryItem> GetVisitHistory(long patientId)
        {
            if (patientId <= 0)
            {
                return Enumerable.Empty<PatientVisitHistoryItem>();
            }

            var visits = visitRepo.Get(v => v.PatientId == patientId && v.IsActive)
                .OrderByDescending(v => v.VisitDateTime)
                .ThenByDescending(v => v.PatientVisitId)
                .ToList();

            var invoiceIds = visits
                .Where(v => v.SaleInvoiceId.HasValue && v.SaleInvoiceId > 0)
                .Select(v => v.SaleInvoiceId.Value)
                .Distinct()
                .ToList();

            var invoiceLookup = invoiceIds.Any()
                ? invoiceRepo.Get(i => invoiceIds.Contains(i.Id))
                    .ToDictionary(i => i.Id, i => i.InvoiceNo)
                : new Dictionary<long, string>();

            return visits.Select(v => new PatientVisitHistoryItem
            {
                PatientVisitId = v.PatientVisitId,
                VisitId = v.VisitId,
                VisitDateTime = v.VisitDateTime,
                SaleInvoiceId = v.SaleInvoiceId,
                InvoiceNo = v.SaleInvoiceId.HasValue && invoiceLookup.ContainsKey(v.SaleInvoiceId.Value)
                    ? invoiceLookup[v.SaleInvoiceId.Value]
                    : null,
                VisitStatus = v.VisitStatus,
                VisitStatusLabel = FormatVisitStatus(v.VisitStatus),
                CreatedBy = v.CreatedBy
            });
        }

        public IEnumerable<long> GetPatientIdsMatchingVisitSearch(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return Enumerable.Empty<long>();
            }

            var term = search.Trim();
            return visitRepo.Get(v => v.IsActive).AsEnumerable()
                .Where(v => v.VisitId != null && v.VisitId.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(v => v.PatientId)
                .Distinct()
                .ToList();
        }

        public void LinkVisitToInvoice(long? patientVisitId, long saleInvoiceId, int invoiceStatus)
        {
            if (!patientVisitId.HasValue || patientVisitId <= 0 || saleInvoiceId <= 0)
            {
                return;
            }

            var visit = visitRepo.Get(patientVisitId.Value);
            if (visit == null || !visit.IsActive)
            {
                return;
            }

            visit.SaleInvoiceId = saleInvoiceId;
            visit.VisitStatus = invoiceStatus >= (int)InvoiceStatusType.Confirmed
                ? (int)VisitStatusType.Completed
                : (int)VisitStatusType.InProgress;
            visit.ModifiedOn = DateTime.Now;
            visit.ModifiedBy = identity?.ActivityMember ?? "system";
            visitRepo.Update(visit);
        }

        public void MarkVisitCancelled(long? patientVisitId, long saleInvoiceId)
        {
            if (!patientVisitId.HasValue || patientVisitId <= 0)
            {
                return;
            }

            var visit = visitRepo.Get(patientVisitId.Value);
            if (visit == null || !visit.IsActive)
            {
                return;
            }

            if (visit.SaleInvoiceId.HasValue && visit.SaleInvoiceId.Value != saleInvoiceId)
            {
                return;
            }

            visit.VisitStatus = (int)VisitStatusType.Cancelled;
            visit.ModifiedOn = DateTime.Now;
            visit.ModifiedBy = identity?.ActivityMember ?? "system";
            visitRepo.Update(visit);
        }

        public long? ResolvePatientVisitIdForInvoice(long patientId, long? requestedPatientVisitId)
        {
            if (requestedPatientVisitId.HasValue && requestedPatientVisitId > 0)
            {
                var requested = visitRepo.Get(requestedPatientVisitId.Value);
                if (requested != null && requested.PatientId == patientId && requested.IsActive)
                {
                    return requested.PatientVisitId;
                }
            }

            var current = GetCurrentVisit(patientId);
            return current?.PatientVisitId;
        }

        public static string ResolveVisitId(
            ModuleRepo<PatientVisit> visitRepository,
            ModuleRepo<PatientDetail> patientRepository,
            long? patientVisitId,
            long patientId)
        {
            if (patientVisitId.HasValue && patientVisitId > 0)
            {
                var visit = visitRepository.Get(patientVisitId.Value);
                if (visit != null && !string.IsNullOrWhiteSpace(visit.VisitId))
                {
                    return visit.VisitId;
                }
            }

            var patient = patientRepository.Get(patientId);
            return patient?.VisitId;
        }

        private static string FormatVisitStatus(int status)
        {
            switch ((VisitStatusType)status)
            {
                case VisitStatusType.New: return "New";
                case VisitStatusType.InProgress: return "In Progress";
                case VisitStatusType.Completed: return "Completed";
                case VisitStatusType.Cancelled: return "Cancelled";
                default: return status.ToString(CultureInfo.InvariantCulture);
            }
        }

        private static string GenerateNextCode(string prefix, int digits, IEnumerable<string> codes)
        {
            var max = 0;
            foreach (var code in codes.Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                var trimmed = code.Trim();
                if (trimmed.Length > prefix.Length &&
                    trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(trimmed.Substring(prefix.Length), out var num) &&
                    num > max)
                {
                    max = num;
                }
            }

            return $"{prefix}{(max + 1).ToString($"D{digits}", CultureInfo.InvariantCulture)}";
        }
    }
}
