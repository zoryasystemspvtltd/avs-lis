using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.Businesslogic
{
    public class RadiologyReportManager : IRadiologyReportManager
    {
        private readonly ModuleRepo<RadiologyRequestDetail> requestRepo;
        private readonly ModuleRepo<RadiologyResultDetail> resultRepo;
        private readonly ModuleRepo<PatientDetail> patientRepo;
        private readonly IModuleIdentity identity;

        public RadiologyReportManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
        {
            this.identity = identity;
            requestRepo = new ModuleRepo<RadiologyRequestDetail>(logger, identity, uow);
            resultRepo = new ModuleRepo<RadiologyResultDetail>(logger, identity, uow);
            patientRepo = new ModuleRepo<PatientDetail>(logger, identity, uow);
        }

        public ItemList<RadiologyQueueRow> GetPendingQueue(SampleWorkflowSearchOptions options)
        {
            options = options ?? new SampleWorkflowSearchOptions();
            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);
            var requests = requestRepo.Get(r =>
                r.ReportStatus == RadiologyReportStatus.Pending ||
                r.ReportStatus == RadiologyReportStatus.Draft ||
                r.ReportStatus == RadiologyReportStatus.UnderReview).ToList();

            if (!string.IsNullOrWhiteSpace(options.Modality))
            {
                var modality = options.Modality.Trim();
                requests = requests.Where(r =>
                    !string.IsNullOrWhiteSpace(r.Modality) &&
                    r.Modality.IndexOf(modality, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            if (!string.IsNullOrWhiteSpace(options.PatientName))
            {
                var name = options.PatientName.Trim();
                requests = requests.Where(r =>
                    patients.TryGetValue(r.PatientId, out var p) &&
                    !string.IsNullOrWhiteSpace(p.Name) &&
                    p.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            if (!string.IsNullOrWhiteSpace(options.SearchText))
            {
                var text = options.SearchText.Trim();
                requests = requests.Where(r =>
                    (!string.IsNullOrWhiteSpace(r.AccessionNo) && r.AccessionNo.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrWhiteSpace(r.HISTestName) && r.HISTestName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (patients.TryGetValue(r.PatientId, out var p) &&
                        !string.IsNullOrWhiteSpace(p.Name) &&
                        p.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            }

            var rows = requests.Select(r =>
            {
                patients.TryGetValue(r.PatientId, out var patient);
                return new RadiologyQueueRow
                {
                    Id = r.Id,
                    HisRequestNo = r.HISRequestNo,
                    AccessionNo = r.AccessionNo,
                    HisPatientId = patient?.HisPatientId,
                    PatientName = patient?.Name,
                    TestName = r.HISTestName,
                    Modality = r.Modality,
                    Department = r.Department,
                    Status = FormatStatus(r.ReportStatus),
                    CreatedOn = r.CreatedOn,
                    CreatedBy = r.CreatedBy
                };
            }).ToList();

            return Paginate(rows, options, "CreatedOn");
        }

        public RadiologyReportDetailDto GetReport(long radiologyRequestId)
        {
            var request = requestRepo.Get(radiologyRequestId);
            if (request == null)
            {
                throw new InvalidOperationException("Radiology request not found.");
            }

            var result = resultRepo.Get(r => r.RadiologyRequestId == request.Id).FirstOrDefault();
            var patient = patientRepo.Get(request.PatientId);
            var editable = request.ReportStatus != RadiologyReportStatus.Authorized &&
                           request.ReportStatus != RadiologyReportStatus.Released;

            return new RadiologyReportDetailDto
            {
                Id = request.Id,
                PatientId = request.PatientId,
                HisRequestNo = request.HISRequestNo,
                AccessionNo = request.AccessionNo,
                HisPatientId = patient?.HisPatientId,
                PatientName = patient?.Name,
                TestName = request.HISTestName,
                Modality = request.Modality,
                Department = request.Department,
                Status = FormatStatus(request.ReportStatus),
                ReportStatus = request.ReportStatus,
                ClinicalHistory = result?.ClinicalHistory,
                Findings = result?.Findings,
                Impression = result?.Impression,
                Recommendation = result?.Recommendation,
                AuthorizedBy = result?.AuthorizedBy,
                AuthorizedOn = result?.AuthorizedOn,
                CanEdit = editable,
                CanAuthorize = request.ReportStatus == RadiologyReportStatus.UnderReview
            };
        }

        public long CreateRequest(RadiologyRequestDetail request)
        {
            if (request == null || request.PatientId <= 0)
            {
                throw new ArgumentException("Patient is required.");
            }

            var now = DateTime.Now;
            request.ReportStatus = RadiologyReportStatus.Pending;
            request.CreatedOn = now;
            request.ModifiedOn = now;
            request.CreatedBy = identity?.ActivityMember ?? "system";
            request.ModifiedBy = request.CreatedBy;

            if (string.IsNullOrWhiteSpace(request.AccessionNo))
            {
                request.AccessionNo = $"RAD-{DateTime.Now:yyyyMMdd}-{request.PatientId}";
            }

            return requestRepo.Add(request);
        }

        public void SaveReport(RadiologyReportSaveRequest request)
        {
            if (request == null || request.RadiologyRequestId <= 0)
            {
                throw new ArgumentException("Radiology request is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Findings))
            {
                throw new ArgumentException("Findings are mandatory.");
            }

            if (string.IsNullOrWhiteSpace(request.Impression))
            {
                throw new ArgumentException("Impression is mandatory.");
            }

            var header = requestRepo.Get(request.RadiologyRequestId);
            if (header == null)
            {
                throw new InvalidOperationException("Radiology request not found.");
            }

            if (header.ReportStatus == RadiologyReportStatus.Authorized ||
                header.ReportStatus == RadiologyReportStatus.Released)
            {
                throw new InvalidOperationException("Authorized reports cannot be edited.");
            }

            var now = DateTime.Now;
            var user = identity?.ActivityMember ?? "system";
            var result = resultRepo.Get(r => r.RadiologyRequestId == header.Id).FirstOrDefault();
            if (result == null)
            {
                result = new RadiologyResultDetail
                {
                    RadiologyRequestId = header.Id,
                    CreatedOn = now,
                    CreatedBy = user
                };
                result.ClinicalHistory = request.ClinicalHistory?.Trim();
                result.Findings = request.Findings.Trim();
                result.Impression = request.Impression.Trim();
                result.Recommendation = request.Recommendation?.Trim();
                result.ModifiedOn = now;
                result.ModifiedBy = user;
                resultRepo.Add(result);
            }
            else
            {
                result.ClinicalHistory = request.ClinicalHistory?.Trim();
                result.Findings = request.Findings.Trim();
                result.Impression = request.Impression.Trim();
                result.Recommendation = request.Recommendation?.Trim();
                result.ModifiedOn = now;
                result.ModifiedBy = user;
                resultRepo.Update(result);
            }

            header.ReportStatus = request.SubmitForReview
                ? RadiologyReportStatus.UnderReview
                : RadiologyReportStatus.Draft;
            header.ModifiedOn = now;
            header.ModifiedBy = user;
            requestRepo.Update(header);
        }

        public void AuthorizeReport(RadiologyAuthorizeRequest request, bool canAuthorize)
        {
            if (!canAuthorize)
            {
                throw new UnauthorizedAccessException("Only authorized users can approve radiology reports.");
            }

            if (request == null || request.RadiologyRequestId <= 0)
            {
                throw new ArgumentException("Radiology request is required.");
            }

            if (string.IsNullOrWhiteSpace(request.DigitalSignature))
            {
                throw new ArgumentException("Digital signature is required.");
            }

            var header = requestRepo.Get(request.RadiologyRequestId);
            if (header == null)
            {
                throw new InvalidOperationException("Radiology request not found.");
            }

            if (header.ReportStatus != RadiologyReportStatus.UnderReview &&
                header.ReportStatus != RadiologyReportStatus.Draft)
            {
                throw new InvalidOperationException("Report is not in a reviewable state.");
            }

            var result = resultRepo.Get(r => r.RadiologyRequestId == header.Id).FirstOrDefault();
            if (result == null || string.IsNullOrWhiteSpace(result.Findings) || string.IsNullOrWhiteSpace(result.Impression))
            {
                throw new ArgumentException("Findings and impression are required before authorization.");
            }

            var now = DateTime.Now;
            var user = identity?.ActivityMember ?? "system";
            result.AuthorizedBy = user;
            result.AuthorizedOn = now;
            result.DigitalSignature = request.DigitalSignature.Trim();
            result.ModifiedOn = now;
            result.ModifiedBy = user;
            resultRepo.Update(result);

            header.ReportStatus = request.Release
                ? RadiologyReportStatus.Released
                : RadiologyReportStatus.Authorized;
            header.ModifiedOn = now;
            header.ModifiedBy = user;
            requestRepo.Update(header);
        }

        private static string FormatStatus(RadiologyReportStatus status)
        {
            switch (status)
            {
                case RadiologyReportStatus.Pending: return "Pending";
                case RadiologyReportStatus.Draft: return "Draft";
                case RadiologyReportStatus.UnderReview: return "Under Review";
                case RadiologyReportStatus.Authorized: return "Authorized";
                case RadiologyReportStatus.Released: return "Released";
                default: return status.ToString();
            }
        }

        private static ItemList<T> Paginate<T>(List<T> rows, SampleWorkflowSearchOptions options, string defaultSort) where T : class
        {
            var result = new ItemList<T>();
            var sortColumn = string.IsNullOrWhiteSpace(options.SortColumnName) ? defaultSort : options.SortColumnName;
            var pageSize = options.RecordPerPage <= 0 ? 25 : options.RecordPerPage;
            var page = options.CurrentPage <= 0 ? 1 : options.CurrentPage;
            var exportAll = options.RecordPerPage <= 0;

            IEnumerable<T> sorted;
            try
            {
                sorted = rows.OrderBy(sortColumn, options.SortDirection);
            }
            catch
            {
                sorted = rows.OrderBy(defaultSort, false);
            }

            var list = sorted.ToList();
            result.TotalRecord = list.Count;
            if (exportAll)
            {
                result.Items = list;
                return result;
            }

            var minRow = (page - 1) * pageSize;
            result.Items = list.Skip(minRow).Take(pageSize).ToList();
            return result;
        }
    }
}
