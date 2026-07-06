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
    public class RadiologyReportManager : IRadiologyReportManager
    {
        private readonly ModuleRepo<RadiologyRequestDetail> requestRepo;
        private readonly ModuleRepo<RadiologyResultDetail> resultRepo;
        private readonly ModuleRepo<PatientDetail> patientRepo;
        private readonly ModuleRepo<SaleInvoice> invoiceRepo;
        private readonly IModuleIdentity identity;

        public RadiologyReportManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
        {
            this.identity = identity;
            requestRepo = new ModuleRepo<RadiologyRequestDetail>(logger, identity, uow);
            resultRepo = new ModuleRepo<RadiologyResultDetail>(logger, identity, uow);
            patientRepo = new ModuleRepo<PatientDetail>(logger, identity, uow);
            invoiceRepo = new ModuleRepo<SaleInvoice>(logger, identity, uow);
        }

        public ItemList<RadiologyQueueRow> GetPendingQueue(SampleWorkflowSearchOptions options)
        {
            return BuildQueue(options, RadiologyReportStatus.Pending, RadiologyReportStatus.Draft);
        }

        public ItemList<RadiologyQueueRow> GetDoctorApprovalQueue(SampleWorkflowSearchOptions options)
        {
            return BuildQueue(options, RadiologyReportStatus.UnderReview);
        }

        public ItemList<RadiologyQueueRow> GetApprovedQueue(SampleWorkflowSearchOptions options)
        {
            return BuildQueue(options, RadiologyReportStatus.Authorized, RadiologyReportStatus.Released);
        }

        private ItemList<RadiologyQueueRow> BuildQueue(SampleWorkflowSearchOptions options, params RadiologyReportStatus[] statuses)
        {
            options = options ?? new SampleWorkflowSearchOptions();
            var statusSet = new HashSet<RadiologyReportStatus>(statuses);
            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);
            var requests = requestRepo.Get().AsEnumerable()
                .Where(r => statusSet.Contains(r.ReportStatus))
                .ToList();

            requests = ApplySearchFilters(requests, options, patients);

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

        private static List<RadiologyRequestDetail> ApplySearchFilters(
            List<RadiologyRequestDetail> requests,
            SampleWorkflowSearchOptions options,
            Dictionary<long, PatientDetail> patients)
        {
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

            return requests;
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

            if (header.ReportStatus != RadiologyReportStatus.UnderReview)
            {
                throw new InvalidOperationException("Report is not pending doctor approval.");
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

        public List<RadiologyPrintAccessionOption> GetPrintableAccessions()
        {
            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);
            var invoices = invoiceRepo.Get().ToDictionary(i => i.InvoiceNo, i => i, StringComparer.OrdinalIgnoreCase);

            return requestRepo.Get().AsEnumerable()
                .Where(r => r.ReportStatus == RadiologyReportStatus.Authorized ||
                            r.ReportStatus == RadiologyReportStatus.Released)
                .Where(r =>
                {
                    if (string.IsNullOrWhiteSpace(r.HISRequestNo)) { return false; }
                    if (!invoices.TryGetValue(r.HISRequestNo.Trim(), out var invoice)) { return false; }
                    return invoice.InvoiceStatus != (int)InvoiceStatusType.Cancelled &&
                           invoice.PaymentStatus == (int)PaymentStatusType.Paid;
                })
                .Select(r =>
                {
                    patients.TryGetValue(r.PatientId, out var patient);
                    var label = $"{r.AccessionNo} — {patient?.Name} — {r.HISTestName}";
                    return new RadiologyPrintAccessionOption
                    {
                        RadiologyRequestId = r.Id,
                        AccessionNo = r.AccessionNo,
                        InvoiceNo = r.HISRequestNo,
                        PatientName = patient?.Name,
                        TestName = r.HISTestName,
                        DisplayLabel = label
                    };
                })
                .OrderBy(o => o.AccessionNo)
                .ToList();
        }

        public DiagnosticRadiologyReportDto GetRadiologyReportForPrint(long radiologyRequestId)
        {
            if (radiologyRequestId <= 0)
            {
                throw new TestReportValidationException("Radiology request is required.");
            }

            var request = requestRepo.Get(radiologyRequestId);
            if (request == null)
            {
                throw new TestReportValidationException("Radiology request not found.");
            }

            if (request.ReportStatus != RadiologyReportStatus.Authorized &&
                request.ReportStatus != RadiologyReportStatus.Released)
            {
                throw new TestReportValidationException("Radiology report must be authorized or released before printing.");
            }

            var invoice = string.IsNullOrWhiteSpace(request.HISRequestNo)
                ? null
                : invoiceRepo.Get(i => i.InvoiceNo == request.HISRequestNo).FirstOrDefault();

            if (invoice == null)
            {
                throw new TestReportValidationException("Invoice not found for this radiology request.");
            }

            if (invoice.InvoiceStatus == (int)InvoiceStatusType.Cancelled)
            {
                throw new TestReportValidationException("Invoice is cancelled. Radiology report cannot be printed.");
            }

            if (invoice.PaymentStatus != (int)PaymentStatusType.Paid)
            {
                throw new TestReportValidationException("Payment pending. Radiology report can only be printed after full payment.");
            }

            var result = resultRepo.Get(r => r.RadiologyRequestId == request.Id).FirstOrDefault();
            if (result == null ||
                string.IsNullOrWhiteSpace(result.Findings) ||
                string.IsNullOrWhiteSpace(result.Impression))
            {
                throw new TestReportValidationException("Report findings and impression are required for printing.");
            }

            var patient = patientRepo.Get(request.PatientId);
            if (patient == null)
            {
                throw new TestReportValidationException("Patient record not found.");
            }

            return new DiagnosticRadiologyReportDto
            {
                Header = new DiagnosticRadiologyReportHeader
                {
                    AccessionNo = request.AccessionNo,
                    InvoiceNo = request.HISRequestNo,
                    PatientName = patient.Name,
                    PatientId = patient.HisPatientId,
                    Age = patient.Age,
                    Gender = patient.Gender,
                    TestName = request.HISTestName,
                    Modality = request.Modality,
                    Department = request.Department,
                    ReportStatus = FormatStatus(request.ReportStatus),
                    ReportDate = result.AuthorizedOn ?? result.ModifiedOn,
                    AuthorizedBy = result.AuthorizedBy,
                    AuthorizedOn = result.AuthorizedOn,
                    DigitalSignature = result.DigitalSignature
                },
                ClinicalHistory = result.ClinicalHistory,
                Findings = result.Findings,
                Impression = result.Impression,
                Recommendation = result.Recommendation
            };
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
