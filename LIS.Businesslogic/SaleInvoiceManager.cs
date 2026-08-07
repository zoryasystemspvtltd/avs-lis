using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using LIS.BusinessLogic.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LIS.BusinessLogic
{
    public class SaleInvoiceManager : ISaleInvoiceManager
    {
        private readonly ModuleRepo<SaleInvoice> invoiceRepo;
        private readonly ModuleRepo<SaleInvoiceDetail> detailRepo;
        private readonly ModuleRepo<PatientDetail> patientRepo;
        private readonly ModuleRepo<TestRequestDetail> testRequestRepo;
        private readonly ModuleRepo<RadiologyRequestDetail> radiologyRequestRepo;
        private readonly ModuleRepo<HisTestMaster> testRepo;
        private readonly ModuleRepo<Departments> departmentRepo;
        private readonly ModuleRepo<TestRateMaster> rateRepo;
        private readonly ModuleRepo<TestParameter> parameterRepo;
        private readonly ModuleRepo<HISParameterMaster> hisParameterRepo;
        private readonly ModuleRepo<TestParameterMappingMaster> testParameterMappingRepo;
        private readonly ITestRateMasterManager rateManager;
        private readonly ITestProfileMasterManager profileManager;
        private readonly PatientVisitManager patientVisitManager;
        private readonly IModuleIdentity identity;
        private readonly ILogger logger;
        private readonly GenericUnitOfWork unitOfWork;
        private readonly DepartmentProcessingRouter processingRouter;

        public SaleInvoiceManager(
            ILogger logger,
            IModuleIdentity identity,
            GenericUnitOfWork unitOfWork,
            ITestRateMasterManager rateManager,
            ITestProfileMasterManager profileManager,
            PatientVisitManager patientVisitManager)
        {
            this.logger = logger;
            this.identity = identity;
            this.rateManager = rateManager;
            this.profileManager = profileManager;
            this.patientVisitManager = patientVisitManager;
            this.unitOfWork = unitOfWork;
            invoiceRepo = new ModuleRepo<SaleInvoice>(logger, identity, unitOfWork);
            detailRepo = new ModuleRepo<SaleInvoiceDetail>(logger, identity, unitOfWork);
            patientRepo = new ModuleRepo<PatientDetail>(logger, identity, unitOfWork);
            testRequestRepo = new ModuleRepo<TestRequestDetail>(logger, identity, unitOfWork);
            radiologyRequestRepo = new ModuleRepo<RadiologyRequestDetail>(logger, identity, unitOfWork);
            testRepo = new ModuleRepo<HisTestMaster>(logger, identity, unitOfWork);
            departmentRepo = new ModuleRepo<Departments>(logger, identity, unitOfWork);
            rateRepo = new ModuleRepo<TestRateMaster>(logger, identity, unitOfWork);
            parameterRepo = new ModuleRepo<TestParameter>(logger, identity, unitOfWork);
            hisParameterRepo = new ModuleRepo<HISParameterMaster>(logger, identity, unitOfWork);
            testParameterMappingRepo = new ModuleRepo<TestParameterMappingMaster>(logger, identity, unitOfWork);
            processingRouter = new DepartmentProcessingRouter(departmentRepo);
        }

        public SaleInvoiceDto GetById(long id)
        {
            var invoice = invoiceRepo.Get(id);
            if (invoice == null)
            {
                return null;
            }

            EnrichHeader(invoice);
            SaleInvoiceNotesMeta.ApplyToInvoice(invoice);
            var details = detailRepo.Get(d => d.SaleInvoiceId == id && d.IsActive).ToList();
            EnrichDetails(details);

            return new SaleInvoiceDto
            {
                Invoice = invoice,
                Details = details
            };
        }

        public ItemList<SaleInvoice> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            var result = new ItemList<SaleInvoice>();
            var query = invoiceRepo.Get(i => i.IsActive);

            if (!string.IsNullOrEmpty(option.SearchText))
            {
                var search = option.SearchText.Trim();
                DateTime searchDate;
                bool isDate = DateTime.TryParseExact(search, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out searchDate);

                if (isDate)
                {
                    query = query.Where(i =>
                        i.InvoiceDate.Year == searchDate.Year &&
                        i.InvoiceDate.Month == searchDate.Month &&
                        i.InvoiceDate.Day == searchDate.Day);
                }
                else
                {
                    var patientIds = patientRepo.Get(p =>
                        (p.Name != null && p.Name.Contains(search)) ||
                        (p.Phone != null && p.Phone.Contains(search)))
                        .Select(p => p.Id).ToList();

                    query = query.Where(i =>
                        i.InvoiceNo.Contains(search) ||
                        (i.RefDoctorName != null && i.RefDoctorName.Contains(search)) ||
                        patientIds.Contains(i.PatientId));
                }
            }

            var list = query.ToList().Select(i =>
            {
                EnrichHeader(i);
                return i;
            }).ToList();

            result.TotalRecord = list.Count;
            var sortColumn = ResolveSortColumn(option.SortColumnName);
            int minRow = (option.CurrentPage - 1) * option.RecordPerPage;
            int pageSize = option.RecordPerPage == 0 ? result.TotalRecord : option.RecordPerPage;

            result.Items = list
                .OrderBy(sortColumn, option.SortDirection)
                .Skip(minRow)
                .Take(pageSize)
                .Select(ToListItem)
                .ToList();

            return result;
        }

        public ItemList<BillableItemLookup> GetBillableItems(ListOptions option, DateTime? invoiceDate = null)
        {
            option = option ?? new ListOptions();
            var pageSize = option.RecordPerPage > 0 ? option.RecordPerPage : 50;
            var page = option.CurrentPage > 0 ? option.CurrentPage : 1;
            var search = (option.SearchText ?? string.Empty).Trim();
            var asOf = (invoiceDate ?? DateTime.Today).Date;
            var itemType = (option.BillableItemType ?? string.Empty).Trim();
            var departmentCode = (option.DepartmentCode ?? string.Empty).Trim();
            var includeProfiles = string.IsNullOrEmpty(itemType) ||
                string.Equals(itemType, "profile", StringComparison.OrdinalIgnoreCase);
            var includeTests = string.IsNullOrEmpty(itemType) ||
                string.Equals(itemType, "test", StringComparison.OrdinalIgnoreCase);

            var ratedTestIds = rateRepo.Get(r =>
                    r.IsActive &&
                    r.EffectiveStart <= asOf &&
                    r.EffectiveEnd >= asOf)
                .Select(r => r.TestId)
                .Distinct()
                .ToList();

            var items = new List<BillableItemLookup>();

            if (includeProfiles && string.IsNullOrEmpty(departmentCode))
            {
                var profilePageSize = string.IsNullOrEmpty(search) ? pageSize : 0;
                var profileOptions = new ListOptions
                {
                    RecordPerPage = profilePageSize > 0 ? profilePageSize : 25,
                    CurrentPage = 1,
                    SearchText = search,
                    SortColumnName = "Name",
                    SortDirection = true
                };
                var profileResult = profileManager.Get(profileOptions);
                foreach (var profile in (profileResult?.Items ?? Enumerable.Empty<TestProfileMaster>()).Where(p => p.IsActive))
                {
                    items.Add(new BillableItemLookup
                    {
                        Key = $"profile:{profile.Id}",
                        Label = $"[Profile] {profile.Code} - {profile.Name} ({profile.PackageRate})",
                        ItemType = "profile",
                        TestProfileId = profile.Id
                    });
                }
            }

            if (includeTests && ratedTestIds.Any())
            {
                var testQuery = testRepo.Get(t => t.IsActive && ratedTestIds.Contains(t.Id));
                if (!string.IsNullOrEmpty(departmentCode))
                {
                    var deptLower = departmentCode.ToLower();
                    testQuery = testQuery.Where(t =>
                        t.DepartmentCode != null &&
                        t.DepartmentCode.ToLower() == deptLower);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    testQuery = testQuery.Where(t =>
                        (t.HISTestCode != null && t.HISTestCode.ToLower().Contains(searchLower)) ||
                        (t.HISTestCodeDescription != null && t.HISTestCodeDescription.ToLower().Contains(searchLower)));
                }

                var testTake = string.IsNullOrEmpty(search) ? Math.Max(pageSize, 50) : 200;
                var tests = testQuery
                    .OrderBy(t => t.HISTestCode)
                    .Take(testTake)
                    .ToList();

                foreach (var test in tests)
                {
                    items.Add(new BillableItemLookup
                    {
                        Key = $"test:{test.Id}",
                        Label = $"{test.HISTestCode} - {test.HISTestCodeDescription}",
                        ItemType = "test",
                        TestId = test.Id,
                        DepartmentCode = test.DepartmentCode
                    });
                }
            }

            var sorted = items
                .OrderBy(i => i.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var total = sorted.Count;
            var paged = sorted
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new ItemList<BillableItemLookup>
            {
                TotalRecord = total,
                Items = paged
            };
        }

        public long Save(SaleInvoiceDto dto)
        {
            using (var transaction = unitOfWork.BeginTransaction())
            {
                try
                {
                    var id = SaveCore(dto);
                    transaction.Commit();
                    return id;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private long SaveCore(SaleInvoiceDto dto)
        {
            if (dto?.Invoice == null)
            {
                throw new ArgumentException("Invoice is required");
            }

            var header = dto.Invoice;
            var lines = (dto.Details ?? new List<SaleInvoiceDetail>())
                .Where(l => l.TestId > 0 || (l.TestProfileId.HasValue && l.TestProfileId > 0))
                .ToList();

            if (!lines.Any())
            {
                throw new ArgumentException("At least one test or profile line is required");
            }

            NormalizeProfileLines(lines);

            if (header.PatientId <= 0)
            {
                throw new ArgumentException("Patient is required.");
            }

            var patient = patientRepo.Get(header.PatientId);
            if (patient == null)
            {
                throw new ArgumentException("Selected patient was not found.");
            }

            header.PatientVisitId = patientVisitManager.ResolvePatientVisitIdForInvoice(
                header.PatientId,
                header.PatientVisitId);

            foreach (var line in lines)
            {
                if (line.TestProfileId.HasValue && line.TestProfileId > 0)
                {
                    var profile = profileManager.GetWithDetails(line.TestProfileId.Value);
                    if (profile == null || !profile.IsActive)
                    {
                        throw new InvalidOperationException("Selected test profile is inactive or unavailable.");
                    }

                    if (line.Rate <= 0)
                    {
                        line.Rate = profile.PackageRate;
                    }

                    continue;
                }

                var test = testRepo.Get(line.TestId);
                if (test == null)
                {
                    throw new InvalidOperationException($"Test id {line.TestId} was not found.");
                }

                if (!test.IsActive)
                {
                    throw new InvalidOperationException($"Test '{test.HISTestCode}' is inactive and cannot be invoiced.");
                }
            }

            Recalculate(header, lines);
            SaleInvoiceNotesMeta.EncodeFromInvoice(header);

            var now = DateTime.Now;
            if (header.InvoiceDate == default(DateTime))
            {
                header.InvoiceDate = OperationalDateTime.GetFacilityNow();
            }
            else
            {
                header.InvoiceDate = OperationalDateTime.ToFacilityWallClock(header.InvoiceDate);
            }

            if (header.Id <= 0)
            {
                header.Id = 0;
                var duplicateNo = invoiceRepo.Get(i => i.InvoiceNo == header.InvoiceNo && i.IsActive).FirstOrDefault();
                if (duplicateNo != null)
                {
                    throw new InvalidOperationException("Invoice number already exists");
                }

                if (string.IsNullOrEmpty(header.InvoiceNo))
                {
                    header.InvoiceNo = GenerateInvoiceNo();
                }

                header.CreatedOn = now;
                header.CreatedBy = identity?.ActivityMember;
                header.ModifiedOn = now;
                header.ModifiedBy = identity?.ActivityMember;
                header.IsActive = true;

                var id = invoiceRepo.Add(header);
                header.Id = id;

                LinkTestRequestsToLines(header, lines, header.InvoiceNo);
                LinkRadiologyRequestsToLines(header, lines, header.InvoiceNo, now);
                PersistInvoiceDetails(id, lines, now);

                if ((!header.RequestDetailId.HasValue || header.RequestDetailId <= 0) &&
                    lines.Any(l => l.RequestDetailId.HasValue && l.RequestDetailId > 0))
                {
                    header.RequestDetailId = lines.First(l => l.RequestDetailId.HasValue && l.RequestDetailId > 0).RequestDetailId;
                    invoiceRepo.Update(header);
                }

                patientVisitManager.LinkVisitToInvoice(header.PatientVisitId, id, header.InvoiceStatus);

                return id;
            }

            var existingHeader = invoiceRepo.Get(header.Id);
            if (existingHeader == null)
            {
                throw new KeyNotFoundException("Invoice not found");
            }

            if (existingHeader.InvoiceStatus == (int)InvoiceStatusType.Cancelled)
            {
                throw new InvalidOperationException("Cancelled invoice cannot be edited");
            }

            // Save must never deactivate; Cancel() is the only deactivation path.
            ApplyInvoiceHeaderUpdate(existingHeader, header);
            existingHeader.ModifiedOn = now;
            existingHeader.ModifiedBy = identity?.ActivityMember;
            invoiceRepo.Update(existingHeader);

            var existing = detailRepo.Get(d => d.SaleInvoiceId == existingHeader.Id).ToList();
            foreach (var old in existing)
            {
                detailRepo.Delete(old);
            }

            LinkTestRequestsToLines(existingHeader, lines, existingHeader.InvoiceNo);
            LinkRadiologyRequestsToLines(existingHeader, lines, existingHeader.InvoiceNo, now);
            PersistInvoiceDetails(existingHeader.Id, lines, now);

            if ((!existingHeader.RequestDetailId.HasValue || existingHeader.RequestDetailId <= 0) &&
                lines.Any(l => l.RequestDetailId.HasValue && l.RequestDetailId > 0))
            {
                existingHeader.RequestDetailId = lines.First(l => l.RequestDetailId.HasValue && l.RequestDetailId > 0).RequestDetailId;
                invoiceRepo.Update(existingHeader);
            }

            patientVisitManager.LinkVisitToInvoice(existingHeader.PatientVisitId, existingHeader.Id, existingHeader.InvoiceStatus);

            return existingHeader.Id;
        }

        private void PersistInvoiceDetails(long invoiceId, List<SaleInvoiceDetail> lines, DateTime now)
        {
            foreach (var line in lines)
            {
                line.SaleInvoiceId = invoiceId;
                line.CreatedOn = now;
                line.CreatedBy = identity?.ActivityMember;
                line.IsActive = true;
                line.Id = detailRepo.Add(line);
            }
        }

        public void UpdateStatus(long id, int invoiceStatus, int paymentStatus, decimal? paidAmount = null)
        {
            var invoice = invoiceRepo.Get(id);
            if (invoice == null)
            {
                throw new KeyNotFoundException("Invoice not found");
            }

            if (invoice.InvoiceStatus == (int)InvoiceStatusType.Cancelled)
            {
                throw new InvalidOperationException("Cancelled invoice cannot be updated");
            }

            invoice.InvoiceStatus = invoiceStatus;
            invoice.ModifiedOn = DateTime.Now;
            invoice.ModifiedBy = identity?.ActivityMember;

            if (paidAmount.HasValue)
            {
                if (paidAmount.Value < 0)
                {
                    throw new ArgumentException("Paid amount cannot be negative.");
                }

                if (paidAmount.Value > invoice.NetAmount)
                {
                    throw new ArgumentException("Paid amount cannot exceed net amount.");
                }

                invoice.PaidAmount = paidAmount.Value;
            }
            else if (paymentStatus == (int)PaymentStatusType.Paid)
            {
                invoice.PaidAmount = invoice.NetAmount;
            }

            ApplyPaymentStatus(invoice);
            if (paymentStatus == (int)PaymentStatusType.Paid && invoice.PaymentStatus != (int)PaymentStatusType.Paid)
            {
                invoice.PaymentStatus = (int)PaymentStatusType.Paid;
                invoice.PaidAmount = invoice.NetAmount;
                invoice.DueAmount = 0;
            }
            else if (!paidAmount.HasValue && paymentStatus != (int)PaymentStatusType.Paid)
            {
                invoice.PaymentStatus = paymentStatus;
            }

            invoice.IsActive = true;
            invoiceRepo.Update(invoice);
        }

        public void Cancel(long id)
        {
            var invoice = invoiceRepo.Get(id);
            UpdateStatus(id, (int)InvoiceStatusType.Cancelled, (int)PaymentStatusType.Unpaid);
            invoice = invoiceRepo.Get(id);
            invoice.IsActive = false;
            invoiceRepo.Update(invoice);
            patientVisitManager.MarkVisitCancelled(invoice.PatientVisitId, id);
        }

        /// <summary>
        /// Ensures each invoice line has a valid TestRequestDetail FK before persistence.
        /// </summary>
        private void LinkTestRequestsToLines(SaleInvoice invoice, List<SaleInvoiceDetail> lines, string requestNo)
        {
            if (invoice == null || invoice.PatientId <= 0 || lines == null)
            {
                return;
            }

            var reqNo = string.IsNullOrWhiteSpace(requestNo) ? $"INV{invoice.Id}" : requestNo;
            var now = DateTime.Now;

            foreach (var line in lines.Where(l => l.TestId > 0))
            {
                if (line.TestProfileId.HasValue && line.TestProfileId > 0)
                {
                    ExpandProfileTestRequests(invoice, line, reqNo, now);
                    continue;
                }

                var test = testRepo.Get(line.TestId);
                if (test == null)
                {
                    throw new InvalidOperationException(
                        $"Test id {line.TestId} was not found in HIS Test master.");
                }

                if (processingRouter.IsDiagnosticTest(test))
                {
                    continue;
                }

                if (line.RequestDetailId.HasValue && line.RequestDetailId > 0 &&
                    testRequestRepo.Get(line.RequestDetailId.Value) != null)
                {
                    continue;
                }

                var request = EnsureTestRequest(invoice, test, line, reqNo, now);
                line.RequestDetailId = request.Id;

                if (!line.RequestDetailId.HasValue || line.RequestDetailId <= 0)
                {
                    throw new InvalidOperationException(
                        $"Could not link test request for test id {line.TestId}.");
                }
            }
        }

        private void LinkRadiologyRequestsToLines(
            SaleInvoice invoice,
            List<SaleInvoiceDetail> lines,
            string requestNo,
            DateTime now)
        {
            if (invoice == null || invoice.PatientId <= 0 || lines == null)
            {
                return;
            }

            var reqNo = string.IsNullOrWhiteSpace(requestNo) ? $"INV{invoice.Id}" : requestNo;

            foreach (var line in lines.Where(l => l.TestId > 0 && (!l.TestProfileId.HasValue || l.TestProfileId <= 0)))
            {
                var test = testRepo.Get(line.TestId);
                if (test == null || !processingRouter.IsDiagnosticTest(test))
                {
                    continue;
                }

                EnsureRadiologyRequest(invoice, test, reqNo, now);
            }
        }

        private static string ResolveRadiologyModality(HisTestMaster test)
        {
            var name = test?.HISSpecimenName ?? test?.HISSpecimenCode ?? test?.HISTestCodeDescription ?? "General";
            return name.Length > 30 ? name.Substring(0, 30) : name;
        }

        private string ResolveRadiologyDepartment(HisTestMaster test)
        {
            if (test == null || string.IsNullOrWhiteSpace(test.DepartmentCode))
            {
                return "Radiology";
            }

            var dept = departmentRepo.Get(d =>
                d.Code != null && d.Code.Equals(test.DepartmentCode, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            return !string.IsNullOrWhiteSpace(dept?.Name) ? dept.Name : "Radiology";
        }

        private void ApplyTestDepartment(TestRequestDetail request, HisTestMaster test)
        {
            if (request == null || test == null || string.IsNullOrWhiteSpace(test.DepartmentCode))
            {
                return;
            }

            var dept = departmentRepo.Get(d =>
                d.Code != null && d.Code.Equals(test.DepartmentCode, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (dept == null)
            {
                return;
            }

            request.DepartmentId = dept.Code;
            request.Department = dept.Name;
        }

        public string GenerateInvoiceNo()
        {
            var prefix = $"INV-{OperationalDateTime.GetFacilityNow():yyyyMMdd}-";
            var last = invoiceRepo.Get()
                .Where(i => i.InvoiceNo != null && i.InvoiceNo.StartsWith(prefix))
                .OrderByDescending(i => i.InvoiceNo)
                .FirstOrDefault();

            int seq = 1;
            if (last != null)
            {
                var parts = last.InvoiceNo.Split('-');
                if (parts.Length >= 3)
                {
                    int.TryParse(parts[parts.Length - 1], out seq);
                    seq++;
                }
            }

            return prefix + seq.ToString("D4");
        }

        private void Recalculate(SaleInvoice header, List<SaleInvoiceDetail> lines)
        {
            var headerDiscountType = header?.DiscountType;
            // Raw header discount input the user keyed in. Fall back to the persisted
            // DiscountAmount for legacy callers that only send the computed value.
            var headerDiscountInput = (header != null && header.DiscountValue > 0)
                ? header.DiscountValue
                : (header?.DiscountAmount ?? 0m);

            // Tax is entered manually at the invoice level (per CX request) - never auto-derived.
            var headerTax = header?.TaxAmount ?? 0m;

            foreach (var line in lines)
            {
                if (line.TestProfileId.HasValue && line.TestProfileId > 0)
                {
                    if (line.Rate <= 0)
                    {
                        var profile = profileManager.GetById(line.TestProfileId.Value);
                        if (profile != null)
                        {
                            line.Rate = profile.PackageRate;
                        }
                    }
                }
                else if (line.Rate <= 0 && line.TestId > 0)
                {
                    var invoiceDate = header.InvoiceDate == default(DateTime)
                        ? OperationalDateTime.GetFacilityNow().Date
                        : header.InvoiceDate;
                    var rate = rateManager.GetEffectiveRateForInvoice(
                        line.TestId,
                        invoiceDate,
                        header.CorporateId,
                        header.ReferralDoctorId);

                    if (rate != null)
                    {
                        line.Rate = rate.Rate;
                    }
                }

                line.Amount = Math.Round(line.Rate * line.Quantity, 2);

                // Line discount: compute the rupee amount from the keyed-in type/value.
                // Legacy callers that only set DiscountAmount (no DiscountValue/Type) are honoured as-is.
                var lineDiscountInput = line.DiscountValue > 0 ? line.DiscountValue : line.DiscountAmount;
                if (!string.IsNullOrWhiteSpace(line.DiscountType) || line.DiscountValue > 0)
                {
                    line.DiscountAmount = ComputeDiscount(line.Amount, line.DiscountType, lineDiscountInput);
                }

                // Tax is not carried at the line level; it is entered once at the invoice level.
                line.TaxAmount = 0m;
                line.NetAmount = Math.Round(line.Amount - line.DiscountAmount, 2);
            }

            header.GrossAmount = lines.Sum(l => l.Amount);
            header.DiscountAmount = ResolveInvoiceDiscount(
                header.GrossAmount,
                lines.Sum(l => l.DiscountAmount),
                headerDiscountType,
                headerDiscountInput);
            header.TaxAmount = headerTax < 0 ? 0m : Math.Round(headerTax, 2);
            header.NetAmount = Math.Round(header.GrossAmount - header.DiscountAmount + header.TaxAmount, 2);
            ApplyPaymentStatus(header);
        }

        /// <summary>Resolves a discount amount in rupees from a keyed-in type and value.</summary>
        private static decimal ComputeDiscount(decimal amount, string discountType, decimal discountInput)
        {
            if (discountInput <= 0)
            {
                return 0m;
            }

            if (string.Equals(discountType, "Percentage", StringComparison.OrdinalIgnoreCase))
            {
                return Math.Round(amount * discountInput / 100m, 2);
            }

            return Math.Round(discountInput, 2);
        }

        /// <summary>
        /// Matches sale-invoice-form recalc(): header discount overrides line discounts when set.
        /// </summary>
        private static decimal ResolveInvoiceDiscount(
            decimal grossAmount,
            decimal lineDiscountTotal,
            string discountType,
            decimal discountInput)
        {
            if (string.Equals(discountType, "Percentage", StringComparison.OrdinalIgnoreCase))
            {
                return discountInput > 0
                    ? Math.Round(grossAmount * discountInput / 100m, 2)
                    : 0m;
            }

            if (discountInput > 0)
            {
                return discountInput;
            }

            return lineDiscountTotal;
        }

        private static void ApplyPaymentStatus(SaleInvoice header)
        {
            if (header == null)
            {
                return;
            }

            if (header.PaidAmount < 0)
            {
                throw new ArgumentException("Paid amount cannot be negative.");
            }

            if (header.PaidAmount > header.NetAmount)
            {
                throw new ArgumentException("Paid amount cannot exceed net amount.");
            }

            if (header.PaidAmount <= 0)
            {
                header.PaymentStatus = (int)PaymentStatusType.Unpaid;
            }
            else if (header.PaidAmount >= header.NetAmount)
            {
                header.PaymentStatus = (int)PaymentStatusType.Paid;
            }
            else
            {
                header.PaymentStatus = (int)PaymentStatusType.Partial;
            }

            header.DueAmount = Math.Round(header.NetAmount - header.PaidAmount, 2);
        }

        private void EnrichHeader(SaleInvoice invoice)
        {
            var patient = patientRepo.Get(invoice.PatientId);
            if (patient != null)
            {
                invoice.PatientName = patient.Name;
                invoice.PatientPhone = patient.Phone;
            }
        }

        private static void ApplyInvoiceHeaderUpdate(SaleInvoice existing, SaleInvoice incoming)
        {
            existing.InvoiceNo = incoming.InvoiceNo;
            existing.InvoiceDate = incoming.InvoiceDate;
            existing.InvoiceStatus = incoming.InvoiceStatus;
            existing.PaymentStatus = incoming.PaymentStatus;
            existing.RequestDetailId = incoming.RequestDetailId;
            existing.PatientId = incoming.PatientId;
            existing.PatientVisitId = incoming.PatientVisitId;
            existing.GrossAmount = incoming.GrossAmount;
            existing.DiscountAmount = incoming.DiscountAmount;
            existing.DiscountType = incoming.DiscountType;
            existing.DiscountValue = incoming.DiscountValue;
            existing.TaxAmount = incoming.TaxAmount;
            existing.NetAmount = incoming.NetAmount;
            existing.PaidAmount = incoming.PaidAmount;
            existing.DueAmount = incoming.DueAmount;
            existing.RefDoctorName = incoming.RefDoctorName;
            existing.ReferralDoctorId = incoming.ReferralDoctorId;
            existing.CorporateId = incoming.CorporateId;
            existing.Notes = incoming.Notes;
            existing.IsActive = true;
        }

        private static string ResolveSortColumn(string sortColumnName)
        {
            if (string.IsNullOrWhiteSpace(sortColumnName))
            {
                return "InvoiceDate";
            }

            switch (sortColumnName.Trim())
            {
                case "invoiceNo":
                case "InvoiceNo":
                    return "InvoiceNo";
                case "invoiceDate":
                case "InvoiceDate":
                    return "InvoiceDate";
                case "netAmount":
                case "NetAmount":
                    return "NetAmount";
                case "patientName":
                case "PatientName":
                    return "PatientName";
                case "id":
                case "Id":
                    return "Id";
                default:
                    return "InvoiceDate";
            }
        }

        private void EnrichDetails(List<SaleInvoiceDetail> details)
        {
            if (details == null)
            {
                return;
            }

            foreach (var line in details)
            {
                if (line.TestProfileId.HasValue && line.TestProfileId > 0)
                {
                    var profile = profileManager.GetById(line.TestProfileId.Value);
                    if (profile != null)
                    {
                        line.TestProfileName = profile.Name;
                    }
                }

                var test = testRepo.Get(line.TestId);
                if (test != null)
                {
                    line.TestName = $"{test.HISTestCode} - {test.HISTestCodeDescription}";
                    line.DepartmentCode = test.DepartmentCode;
                }
            }
        }

        private void NormalizeProfileLines(List<SaleInvoiceDetail> lines)
        {
            foreach (var line in lines.Where(l => l.TestProfileId.HasValue && l.TestProfileId > 0))
            {
                var profile = profileManager.GetWithDetails(line.TestProfileId.Value);
                if (profile?.ProfileDetails == null || !profile.ProfileDetails.Any())
                {
                    throw new InvalidOperationException("Selected profile has no tests configured.");
                }

                if (line.TestId <= 0)
                {
                    line.TestId = profile.ProfileDetails.First().TestId;
                }

                if (line.Quantity <= 0)
                {
                    line.Quantity = 1;
                }

                if (line.Rate <= 0)
                {
                    line.Rate = profile.PackageRate;
                }
            }
        }

        private void ExpandProfileTestRequests(SaleInvoice invoice, SaleInvoiceDetail line, string requestNo, DateTime now)
        {
            var profile = profileManager.GetWithDetails(line.TestProfileId.Value);
            if (profile?.ProfileDetails == null)
            {
                return;
            }

            long firstLabRequestId = 0;
            foreach (var detail in profile.ProfileDetails)
            {
                var test = testRepo.Get(detail.TestId);
                if (test == null)
                {
                    continue;
                }

                processingRouter.EnsureLaboratoryTest(test, "a test profile");

                for (var i = 0; i < Math.Max(1, detail.Quantity); i++)
                {
                    var request = EnsureTestRequest(invoice, test, line, requestNo, now);
                    if (firstLabRequestId <= 0)
                    {
                        firstLabRequestId = request.Id;
                    }
                }
            }

            if (firstLabRequestId > 0)
            {
                line.RequestDetailId = firstLabRequestId;
            }
        }

        private TestRequestDetail EnsureTestRequest(
            SaleInvoice invoice,
            HisTestMaster test,
            SaleInvoiceDetail line,
            string requestNo,
            DateTime now)
        {
            var reqNo = string.IsNullOrWhiteSpace(requestNo) ? $"INV{invoice.Id}" : requestNo;
            var specimenCode = test.HISSpecimenCode?.Trim();
            var request = testRequestRepo.Get(t =>
                t.PatientId == invoice.PatientId &&
                t.HISTestCode == test.HISTestCode &&
                t.HISRequestNo == reqNo).FirstOrDefault();

            if (request != null)
            {
                var updated = false;
                if (!request.PatientVisitId.HasValue && invoice.PatientVisitId.HasValue)
                {
                    request.PatientVisitId = invoice.PatientVisitId;
                    updated = true;
                }

                if (string.IsNullOrWhiteSpace(request.Department))
                {
                    ApplyTestDepartment(request, test);
                    if (!string.IsNullOrWhiteSpace(request.Department))
                    {
                        updated = true;
                    }
                }

                if (updated)
                {
                    testRequestRepo.Update(request);
                }

                SeedTestParameters(request);
                return request;
            }

            var sharedSample = string.IsNullOrWhiteSpace(specimenCode)
                ? null
                : testRequestRepo.Get(t =>
                        t.HISRequestNo == reqNo &&
                        t.SpecimenCode == specimenCode)
                    .AsEnumerable()
                    .Where(t => !string.IsNullOrWhiteSpace(t.SampleNo))
                    .Select(t => t.SampleNo)
                    .FirstOrDefault();

            var sampleNo = !string.IsNullOrWhiteSpace(line?.SampleNo)
                ? line.SampleNo
                : (!string.IsNullOrWhiteSpace(sharedSample)
                    ? sharedSample
                    : Helper.Helper.BuildSampleNo(reqNo, specimenCode));

            request = new TestRequestDetail
            {
                PatientId = invoice.PatientId,
                PatientVisitId = invoice.PatientVisitId,
                HISTestCode = test.HISTestCode,
                HISTestName = test.HISTestCodeDescription,
                HISRequestNo = reqNo,
                HISRequestId = reqNo,
                SampleNo = sampleNo,
                SampleCollectionDate = OperationalDateTime.GetFacilityNow(),
                SampleReceivedDate = OperationalDateTime.GetFacilityNow(),
                SpecimenCode = test.HISSpecimenCode,
                SpecimenName = test.HISSpecimenName,
                ReportStatus = ReportStatusType.New,
                CreatedOn = now,
                CreatedBy = identity?.ActivityMember
            };
            ApplyTestDepartment(request, test);
            request.Id = testRequestRepo.Add(request);
            SeedTestParameters(request);
            return request;
        }

        /// <summary>
        /// Mirrors PatientDetailManager / TestRequestDetailsManager: persist parameter rows
        /// from TestParameterMappingMaster (preferred) or legacy HISParameterMaster by test code.
        /// </summary>
        private void SeedTestParameters(TestRequestDetail request)
        {
            if (request == null || request.Id <= 0 || string.IsNullOrWhiteSpace(request.HISTestCode))
            {
                return;
            }

            if (parameterRepo.Get(p => p.TestRequestDetailsId == request.Id).Any())
            {
                return;
            }

            var testCode = request.HISTestCode.Trim();
            var seeded = false;
            var hisTest = testRepo.Get(t =>
                    t.HISTestCode != null
                    && t.HISTestCode.Equals(testCode, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (hisTest != null)
            {
                var mappings = testParameterMappingRepo
                    .Get(m => m.IsActive && m.HisTestId == hisTest.Id)
                    .ToList();

                foreach (var map in mappings)
                {
                    var param = hisParameterRepo.Get(map.HisParameterId);
                    if (param == null || string.IsNullOrWhiteSpace(param.HISParamCode))
                    {
                        continue;
                    }

                    parameterRepo.Add(new TestParameter
                    {
                        HISParamCode = param.HISParamCode,
                        HISParamName = param.HISParamDescription,
                        HISTestCode = testCode,
                        TestRequestDetailsId = request.Id,
                        CreatedBy = identity?.ActivityMember,
                        CreatedOn = DateTime.UtcNow
                    });
                    seeded = true;
                }
            }

            if (seeded)
            {
                return;
            }

            var legacy = hisParameterRepo
                .Get(p => p.HISTestCode != null
                    && p.HISTestCode.Equals(testCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var param in legacy)
            {
                if (string.IsNullOrWhiteSpace(param.HISParamCode))
                {
                    continue;
                }

                parameterRepo.Add(new TestParameter
                {
                    HISParamCode = param.HISParamCode,
                    HISParamName = param.HISParamDescription,
                    HISTestCode = testCode,
                    TestRequestDetailsId = request.Id,
                    CreatedBy = identity?.ActivityMember,
                    CreatedOn = DateTime.UtcNow
                });
            }
        }

        private void EnsureRadiologyRequest(SaleInvoice invoice, HisTestMaster test, string requestNo, DateTime now)
        {
            var reqNo = string.IsNullOrWhiteSpace(requestNo) ? $"INV{invoice.Id}" : requestNo;
            var existing = radiologyRequestRepo.Get(r =>
                r.PatientId == invoice.PatientId &&
                r.HISRequestNo == reqNo &&
                r.HISTestCode == test.HISTestCode).FirstOrDefault();

            if (existing != null)
            {
                return;
            }

            var modality = ResolveRadiologyModality(test);
            var rad = new RadiologyRequestDetail
            {
                PatientId = invoice.PatientId,
                PatientVisitId = invoice.PatientVisitId,
                HISRequestNo = reqNo,
                AccessionNo = $"{reqNo}-{test.HISTestCode}",
                HISTestCode = test.HISTestCode,
                HISTestName = test.HISTestCodeDescription,
                Modality = modality,
                Department = ResolveRadiologyDepartment(test),
                ReportStatus = RadiologyReportStatus.Pending,
                CreatedOn = now,
                ModifiedOn = now,
                CreatedBy = identity?.ActivityMember ?? "system",
                ModifiedBy = identity?.ActivityMember ?? "system"
            };

            radiologyRequestRepo.Add(rad);
        }

        private static SaleInvoice ToListItem(SaleInvoice invoice)
        {
            return new SaleInvoice
            {
                Id = invoice.Id,
                InvoiceNo = invoice.InvoiceNo,
                InvoiceDate = invoice.InvoiceDate,
                InvoiceStatus = invoice.InvoiceStatus,
                PaymentStatus = invoice.PaymentStatus,
                RequestDetailId = invoice.RequestDetailId,
                PatientId = invoice.PatientId,
                GrossAmount = invoice.GrossAmount,
                DiscountAmount = invoice.DiscountAmount,
                TaxAmount = invoice.TaxAmount,
                NetAmount = invoice.NetAmount,
                PaidAmount = invoice.PaidAmount,
                DueAmount = invoice.DueAmount,
                RefDoctorName = invoice.RefDoctorName,
                ReferralDoctorId = invoice.ReferralDoctorId,
                CorporateId = invoice.CorporateId,
                Notes = invoice.Notes,
                CreatedOn = invoice.CreatedOn,
                CreatedBy = invoice.CreatedBy,
                ModifiedOn = invoice.ModifiedOn,
                ModifiedBy = invoice.ModifiedBy,
                IsActive = invoice.IsActive,
                PatientName = invoice.PatientName,
                PatientPhone = invoice.PatientPhone
            };
        }
    }

    internal static class SaleInvoiceNotesMeta
    {
        private const string Prefix = "[META|";
        private const string Suffix = "]";

        public static void ApplyToInvoice(SaleInvoice invoice)
        {
            if (invoice == null || string.IsNullOrWhiteSpace(invoice.Notes) || !invoice.Notes.StartsWith(Prefix))
            {
                return;
            }

            var end = invoice.Notes.IndexOf(Suffix, StringComparison.Ordinal);
            if (end < 0)
            {
                return;
            }

            var meta = invoice.Notes.Substring(Prefix.Length, end - Prefix.Length);
            var userNotes = invoice.Notes.Length > end + Suffix.Length
                ? invoice.Notes.Substring(end + Suffix.Length).TrimStart('\r', '\n')
                : string.Empty;

            foreach (var part in meta.Split('|'))
            {
                var kv = part.Split(new[] { '=' }, 2);
                if (kv.Length != 2)
                {
                    continue;
                }

                if (kv[0] == "PaymentType")
                {
                    invoice.PaymentType = kv[1];
                }
                else if (kv[0] == "DiscountType" && string.IsNullOrWhiteSpace(invoice.DiscountType))
                {
                    // Legacy backfill only: DiscountType is now a real column. Older invoices
                    // stored it in the notes meta, so honour that when the column is empty.
                    invoice.DiscountType = kv[1];
                }
            }

            invoice.Notes = userNotes;
        }

        public static void EncodeFromInvoice(SaleInvoice invoice)
        {
            if (invoice == null)
            {
                return;
            }

            var userNotes = invoice.Notes ?? string.Empty;
            if (userNotes.StartsWith(Prefix))
            {
                ApplyToInvoice(invoice);
                userNotes = invoice.Notes ?? string.Empty;
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(invoice.PaymentType))
            {
                parts.Add("PaymentType=" + invoice.PaymentType.Trim());
            }

            // DiscountType is persisted as a real column now, so it is no longer encoded into notes.

            if (!parts.Any())
            {
                invoice.Notes = userNotes;
                return;
            }

            invoice.Notes = Prefix + string.Join("|", parts) + Suffix +
                (string.IsNullOrWhiteSpace(userNotes) ? string.Empty : Environment.NewLine + userNotes);
        }
    }
}
