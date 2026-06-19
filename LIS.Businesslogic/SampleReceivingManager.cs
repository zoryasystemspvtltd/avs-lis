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
    public class SampleReceivingManager : ISampleReceivingManager
    {
        private readonly ModuleRepo<TestRequestDetail> requestRepo;
        private readonly ModuleRepo<PatientDetail> patientRepo;
        private readonly ModuleRepo<SampleRejectionReasonMaster> rejectionRepo;
        private readonly ITestRequestDetailsManager testRequestManager;
        private readonly IModuleIdentity identity;

        public SampleReceivingManager(
            ILogger logger,
            IModuleIdentity identity,
            GenericUnitOfWork uow,
            ITestRequestDetailsManager testRequestManager)
        {
            this.identity = identity;
            this.testRequestManager = testRequestManager;
            requestRepo = new ModuleRepo<TestRequestDetail>(logger, identity, uow);
            patientRepo = new ModuleRepo<PatientDetail>(logger, identity, uow);
            rejectionRepo = new ModuleRepo<SampleRejectionReasonMaster>(logger, identity, uow);
        }

        public ItemList<SampleWorkflowQueueRow> GetReceivingQueue(SampleWorkflowSearchOptions options)
        {
            options = options ?? new SampleWorkflowSearchOptions();
            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);

            var query = requestRepo.Get(r =>
                !string.IsNullOrEmpty(r.CollectedBy) &&
                (r.ReceivedBy == null || r.ReceivedBy == "") &&
                r.ReportStatus == ReportStatusType.New);

            var rows = ApplySearch(query.ToList(), patients, options)
                .Select(r => MapRow(r, patients, "Awaiting Receiving"))
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
                throw new InvalidOperationException("Barcode does not exist.");
            }

            if (string.IsNullOrWhiteSpace(request.CollectedBy))
            {
                throw new InvalidOperationException("Sample must be collected before receiving.");
            }

            var patients = patientRepo.Get().ToDictionary(p => p.Id, p => p);
            var status = string.IsNullOrWhiteSpace(request.ReceivedBy) ? "Awaiting Receiving" : "Received";
            return MapRow(request, patients, status);
        }

        public void ReceiveSample(SampleReceivingAction action)
        {
            if (action == null || action.TestRequestId <= 0)
            {
                throw new ArgumentException("Test request is required.");
            }

            if (action.ReceivedDateTime == default(DateTime))
            {
                throw new ArgumentException("Receiving date and time are mandatory.");
            }

            var request = requestRepo.Get(action.TestRequestId);
            if (request == null)
            {
                throw new InvalidOperationException("Barcode does not exist.");
            }

            if (!string.IsNullOrWhiteSpace(action.BarcodeNumber) &&
                !request.SampleNo.Equals(action.BarcodeNumber.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Barcode does not match the selected order.");
            }

            if (string.IsNullOrWhiteSpace(request.CollectedBy))
            {
                throw new InvalidOperationException("Sample must be collected before receiving.");
            }

            if (!string.IsNullOrWhiteSpace(request.ReceivedBy))
            {
                throw new InvalidOperationException("Duplicate receiving is not allowed for this sample.");
            }

            if (action.ReceivedDateTime < request.SampleCollectionDate)
            {
                throw new ArgumentException("Receiving time cannot be before collection time.");
            }

            request.SampleReceivedDate = action.ReceivedDateTime;
            request.ReceivedBy = identity?.ActivityMember ?? "system";
            request.ReceivedRemarks = action.Remarks?.Trim();
            requestRepo.Update(request);
        }

        public void RejectSample(SampleRejectionAction action)
        {
            if (action == null || action.TestRequestId <= 0)
            {
                throw new ArgumentException("Test request is required.");
            }

            if (string.IsNullOrWhiteSpace(action.RejectionReasonCode))
            {
                throw new ArgumentException("Rejection reason is mandatory.");
            }

            var request = requestRepo.Get(action.TestRequestId);
            if (request == null)
            {
                throw new InvalidOperationException("Barcode does not exist.");
            }

            if (string.IsNullOrWhiteSpace(request.CollectedBy))
            {
                throw new InvalidOperationException("Sample must be collected before receiving.");
            }

            var master = rejectionRepo.Get(r =>
                r.Code.Equals(action.RejectionReasonCode.Trim(), StringComparison.OrdinalIgnoreCase) &&
                r.IsActive).FirstOrDefault();

            var reason = master != null ? $"{master.Code} {master.Name}" : action.RejectionReasonCode.Trim();
            request.ReportStatus = ReportStatusType.FinallyRejected;
            request.ReceivedRemarks = string.IsNullOrWhiteSpace(action.Remarks)
                ? reason
                : $"{reason} - {action.Remarks.Trim()}";
            requestRepo.Update(request);
        }

        public void TriggerRecollection(long testRequestId)
        {
            var request = requestRepo.Get(testRequestId);
            if (request == null)
            {
                throw new InvalidOperationException("Order does not exist.");
            }

            testRequestManager.TechnicianReview(testRequestId, ReportStatusType.New, "Recollection from receiving", request.Id);
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

            if (!string.IsNullOrWhiteSpace(options.SearchText))
            {
                var text = options.SearchText.Trim();
                query = query.Where(r =>
                    (!string.IsNullOrWhiteSpace(r.SampleNo) && r.SampleNo.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrWhiteSpace(r.HISRequestNo) && r.HISRequestNo.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0));
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
