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
    public class SampleCollectionManager : ISampleCollectionManager
    {
        private readonly ModuleRepo<TestRequestDetail> requestRepo;
        private readonly ModuleRepo<PatientDetail> patientRepo;
        private readonly ModuleRepo<SampleRejectionReasonMaster> rejectionRepo;
        private readonly ModuleRepo<SaleInvoice> invoiceRepo;
        private readonly ModuleRepo<SaleInvoiceDetail> invoiceDetailRepo;
        private readonly ITestRequestDetailsManager testRequestManager;
        private readonly IModuleIdentity identity;
        private readonly ILogger logger;

        public SampleCollectionManager(
            ILogger logger,
            IModuleIdentity identity,
            GenericUnitOfWork uow,
            ITestRequestDetailsManager testRequestManager)
        {
            this.logger = logger;
            this.identity = identity;
            this.testRequestManager = testRequestManager;
            requestRepo = new ModuleRepo<TestRequestDetail>(logger, identity, uow);
            patientRepo = new ModuleRepo<PatientDetail>(logger, identity, uow);
            rejectionRepo = new ModuleRepo<SampleRejectionReasonMaster>(logger, identity, uow);
            invoiceRepo = new ModuleRepo<SaleInvoice>(logger, identity, uow);
            invoiceDetailRepo = new ModuleRepo<SaleInvoiceDetail>(logger, identity, uow);
        }

        public ItemList<SampleWorkflowQueueRow> GetPendingQueue(SampleWorkflowSearchOptions options)
        {
            options = options ?? new SampleWorkflowSearchOptions();
            var eligibleRequestIds = SampleWorkflowInvoiceEligibility.GetEligibleRequestIds(
                invoiceRepo, invoiceDetailRepo, requestRepo);
            var query = requestRepo.Get(r =>
                r.ReportStatus == ReportStatusType.New &&
                (r.CollectedBy == null || r.CollectedBy == ""))
                .ToList()
                .Where(r => eligibleRequestIds.Contains(r.Id));

            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);
            var rows = ApplySearch(query.ToList(), patients, options)
                .Select(r => MapRow(r, patients, "Pending"))
                .ToList();

            return Paginate(rows, options, "SampleCollectionDate");
        }

        public SampleWorkflowQueueRow GetByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                throw new ArgumentException("Barcode is required.");
            }

            var request = requestRepo.Get(r =>
                r.SampleNo.Equals(barcode.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.Id)
                .FirstOrDefault();

            if (request == null)
            {
                throw new InvalidOperationException("Order not found for the given barcode.");
            }

            if (!SampleWorkflowInvoiceEligibility.IsRequestEligible(
                request.Id, invoiceRepo, invoiceDetailRepo, requestRepo))
            {
                throw new InvalidOperationException(SampleWorkflowInvoiceEligibility.NotConfirmedMessage);
            }

            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);
            var status = string.IsNullOrWhiteSpace(request.CollectedBy) ? "Pending" : "Collected";
            return MapRow(request, patients, status);
        }

        public void CollectSample(SampleCollectionAction action)
        {
            ValidateCollectionAction(action);
            var request = GetRequestOrThrow(action.TestRequestId);
            SampleWorkflowInvoiceEligibility.EnsureRequestEligible(
                request.Id, invoiceRepo, invoiceDetailRepo, requestRepo);

            if (!string.IsNullOrWhiteSpace(request.CollectedBy))
            {
                throw new InvalidOperationException("Duplicate collection is not allowed for this order.");
            }

            if (action.CollectionDateTime > DateTime.Now.AddMinutes(1))
            {
                throw new ArgumentException("Collection time cannot be in the future.");
            }

            var barcode = string.IsNullOrWhiteSpace(action.BarcodeNumber)
                ? EnsureBarcodeInternal(request)
                : action.BarcodeNumber.Trim();

            ValidateBarcodeUnique(barcode, request.Id, request);

            request.SampleNo = barcode;
            request.SampleCollectionDate = action.CollectionDateTime;
            request.CollectedBy = identity?.ActivityMember ?? "system";
            request.CollectedRemarks = action.Remarks?.Trim();
            requestRepo.Update(request);
        }

        public void RejectCollection(SampleRejectionAction action)
        {
            if (action == null || action.TestRequestId <= 0)
            {
                throw new ArgumentException("Test request is required.");
            }

            if (string.IsNullOrWhiteSpace(action.Remarks) && string.IsNullOrWhiteSpace(action.RejectionReasonCode))
            {
                throw new ArgumentException("Rejection reason is mandatory.");
            }

            var request = GetRequestOrThrow(action.TestRequestId);
            SampleWorkflowInvoiceEligibility.EnsureRequestEligible(
                request.Id, invoiceRepo, invoiceDetailRepo, requestRepo);
            var reason = ResolveRejectionReason(action);
            request.ReportStatus = ReportStatusType.FinallyRejected;
            request.CollectedRemarks = string.IsNullOrWhiteSpace(action.Remarks)
                ? reason
                : $"{reason} - {action.Remarks.Trim()}";
            requestRepo.Update(request);
        }

        public void TriggerRecollection(long testRequestId)
        {
            var request = GetRequestOrThrow(testRequestId);
            testRequestManager.TechnicianReview(testRequestId, ReportStatusType.New, "Recollection requested", request.Id);
        }

        public string EnsureBarcode(long testRequestId)
        {
            var request = GetRequestOrThrow(testRequestId);
            return EnsureBarcodeInternal(request);
        }

        private string EnsureBarcodeInternal(TestRequestDetail request)
        {
            if (!string.IsNullOrWhiteSpace(request.SampleNo))
            {
                return request.SampleNo;
            }

            var barcode = $"{request.HISRequestNo}-{request.HISTestCode}";
            ValidateBarcodeUnique(barcode, request.Id, request);
            request.SampleNo = barcode;
            requestRepo.Update(request);
            return barcode;
        }

        private void ValidateCollectionAction(SampleCollectionAction action)
        {
            if (action == null || action.TestRequestId <= 0)
            {
                throw new ArgumentException("Test request is required.");
            }

            if (action.CollectionDateTime == default(DateTime))
            {
                throw new ArgumentException("Collection date and time are mandatory.");
            }
        }

        private void ValidateBarcodeUnique(string barcode, long excludeId, TestRequestDetail request)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                throw new ArgumentException("Barcode is required.");
            }

            var orderNo = request?.HISRequestNo;
            var duplicate = requestRepo.Get(r =>
                r.SampleNo.Equals(barcode, StringComparison.OrdinalIgnoreCase) &&
                r.Id != excludeId &&
                (orderNo == null || r.HISRequestNo != orderNo)).Any();

            if (duplicate)
            {
                throw new InvalidOperationException("Barcode must be unique.");
            }
        }

        private TestRequestDetail GetRequestOrThrow(long id)
        {
            var request = requestRepo.Get(id);
            if (request == null)
            {
                throw new InvalidOperationException("Order does not exist.");
            }

            return request;
        }

        private string ResolveRejectionReason(SampleRejectionAction action)
        {
            if (string.IsNullOrWhiteSpace(action.RejectionReasonCode))
            {
                return action.Remarks?.Trim();
            }

            var master = rejectionRepo.Get(r =>
                r.Code.Equals(action.RejectionReasonCode.Trim(), StringComparison.OrdinalIgnoreCase) &&
                r.IsActive).FirstOrDefault();

            return master != null ? $"{master.Code} {master.Name}" : action.RejectionReasonCode.Trim();
        }

        private static IEnumerable<TestRequestDetail> ApplySearch(
            List<TestRequestDetail> source,
            Dictionary<long, PatientDetail> patients,
            SampleWorkflowSearchOptions options)
        {
            IEnumerable<TestRequestDetail> query = source;

            if (options.PatientId.HasValue && options.PatientId.Value > 0)
            {
                query = query.Where(r => r.PatientId == options.PatientId.Value);
            }

            if (!string.IsNullOrWhiteSpace(options.Uhid))
            {
                var uhid = options.Uhid.Trim();
                query = query.Where(r =>
                    patients.TryGetValue(r.PatientId, out var p) &&
                    !string.IsNullOrWhiteSpace(p.HisPatientId) &&
                    p.HisPatientId.IndexOf(uhid, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (!string.IsNullOrWhiteSpace(options.BarcodeNumber))
            {
                var barcode = options.BarcodeNumber.Trim();
                query = query.Where(r =>
                    !string.IsNullOrWhiteSpace(r.SampleNo) &&
                    r.SampleNo.IndexOf(barcode, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (!string.IsNullOrWhiteSpace(options.OrderNumber))
            {
                var order = options.OrderNumber.Trim();
                query = query.Where(r =>
                    !string.IsNullOrWhiteSpace(r.HISRequestNo) &&
                    r.HISRequestNo.IndexOf(order, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (!string.IsNullOrWhiteSpace(options.PatientName))
            {
                var name = options.PatientName.Trim();
                query = query.Where(r =>
                    patients.TryGetValue(r.PatientId, out var p) &&
                    !string.IsNullOrWhiteSpace(p.Name) &&
                    p.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (options.CollectionDate.HasValue)
            {
                var day = options.CollectionDate.Value.Date;
                query = query.Where(r => r.SampleCollectionDate.Date == day);
            }

            if (options.OrderDate.HasValue)
            {
                var day = options.OrderDate.Value.Date;
                query = query.Where(r => r.CreatedOn.Date == day);
            }

            if (!string.IsNullOrWhiteSpace(options.SearchText))
            {
                var text = options.SearchText.Trim();
                query = query.Where(r =>
                    (!string.IsNullOrWhiteSpace(r.SampleNo) && r.SampleNo.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrWhiteSpace(r.HISRequestNo) && r.HISRequestNo.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (patients.TryGetValue(r.PatientId, out var p) &&
                        ((!string.IsNullOrWhiteSpace(p.Name) && p.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                         (!string.IsNullOrWhiteSpace(p.HisPatientId) && p.HisPatientId.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0))));
            }

            return query;
        }

        private static SampleWorkflowQueueRow MapRow(
            TestRequestDetail request,
            Dictionary<long, PatientDetail> patients,
            string status)
        {
            patients.TryGetValue(request.PatientId, out var patient);
            return new SampleWorkflowQueueRow
            {
                Id = request.Id,
                SampleNo = request.SampleNo,
                HisRequestNo = request.HISRequestNo,
                HisPatientId = patient?.HisPatientId,
                PatientName = patient?.Name,
                TestName = request.HISTestName,
                Department = request.Department,
                Specimen = !string.IsNullOrWhiteSpace(request.SpecimenName) ? request.SpecimenName : request.SpecimenCode,
                SampleCollectionDate = request.SampleCollectionDate,
                SampleReceivedDate = request.SampleReceivedDate,
                CollectedBy = request.CollectedBy,
                ReceivedBy = request.ReceivedBy,
                Status = status,
                CollectedRemarks = request.CollectedRemarks,
                ReceivedRemarks = request.ReceivedRemarks
            };
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
