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
    public class SaleInvoiceManager : ISaleInvoiceManager
    {
        private readonly ModuleRepo<SaleInvoice> invoiceRepo;
        private readonly ModuleRepo<SaleInvoiceDetail> detailRepo;
        private readonly ModuleRepo<PatientDetail> patientRepo;
        private readonly ModuleRepo<TestRequestDetail> testRequestRepo;
        private readonly ModuleRepo<HisTestMaster> testRepo;
        private readonly ITestRateMasterManager rateManager;
        private readonly IModuleIdentity identity;
        private readonly ILogger logger;

        public SaleInvoiceManager(
            ILogger logger,
            IModuleIdentity identity,
            GenericUnitOfWork unitOfWork,
            ITestRateMasterManager rateManager)
        {
            this.logger = logger;
            this.identity = identity;
            this.rateManager = rateManager;
            invoiceRepo = new ModuleRepo<SaleInvoice>(logger, identity, unitOfWork);
            detailRepo = new ModuleRepo<SaleInvoiceDetail>(logger, identity, unitOfWork);
            patientRepo = new ModuleRepo<PatientDetail>(logger, identity, unitOfWork);
            testRequestRepo = new ModuleRepo<TestRequestDetail>(logger, identity, unitOfWork);
            testRepo = new ModuleRepo<HisTestMaster>(logger, identity, unitOfWork);
        }

        public SaleInvoiceDto GetById(long id)
        {
            var invoice = invoiceRepo.Get(id);
            if (invoice == null)
            {
                return null;
            }

            EnrichHeader(invoice);
            var details = detailRepo.Get(d => d.SaleInvoiceId == id && d.IsActive).ToList();

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

        public long Save(SaleInvoiceDto dto)
        {
            if (dto?.Invoice == null)
            {
                throw new ArgumentException("Invoice is required");
            }

            var header = dto.Invoice;
            var lines = (dto.Details ?? new List<SaleInvoiceDetail>())
                .Where(l => l.TestId > 0)
                .ToList();

            if (!lines.Any())
            {
                throw new ArgumentException("At least one test line is required");
            }

            if (header.PatientId <= 0)
            {
                throw new ArgumentException("Patient is required.");
            }

            var patient = patientRepo.Get(header.PatientId);
            if (patient == null)
            {
                throw new ArgumentException("Selected patient was not found.");
            }

            foreach (var line in lines)
            {
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

            var now = DateTime.Now;
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
                if (header.InvoiceDate == default(DateTime))
                {
                    header.InvoiceDate = now;
                }

                var id = invoiceRepo.Add(header);
                header.Id = id;

                LinkTestRequestsToLines(header, lines, header.InvoiceNo);

                foreach (var line in lines)
                {
                    line.SaleInvoiceId = id;
                    line.CreatedOn = now;
                    line.CreatedBy = identity?.ActivityMember;
                    line.IsActive = true;
                    detailRepo.Add(line);
                }

                if ((!header.RequestDetailId.HasValue || header.RequestDetailId <= 0) && lines.Any(l => l.RequestDetailId > 0))
                {
                    header.RequestDetailId = lines.First(l => l.RequestDetailId > 0).RequestDetailId;
                    invoiceRepo.Update(header);
                }

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

            foreach (var line in lines)
            {
                line.SaleInvoiceId = existingHeader.Id;
                line.CreatedOn = now;
                line.CreatedBy = identity?.ActivityMember;
                line.IsActive = true;
                detailRepo.Add(line);
            }

            if ((!existingHeader.RequestDetailId.HasValue || existingHeader.RequestDetailId <= 0) && lines.Any(l => l.RequestDetailId > 0))
            {
                existingHeader.RequestDetailId = lines.First(l => l.RequestDetailId > 0).RequestDetailId;
                invoiceRepo.Update(existingHeader);
            }

            return existingHeader.Id;
        }

        public void UpdateStatus(long id, int invoiceStatus, int paymentStatus)
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
            invoice.PaymentStatus = paymentStatus;
            invoice.IsActive = true;
            invoice.ModifiedOn = DateTime.Now;
            invoice.ModifiedBy = identity?.ActivityMember;

            if (paymentStatus == (int)PaymentStatusType.Paid)
            {
                invoice.PaidAmount = invoice.NetAmount;
                invoice.DueAmount = 0;
            }

            invoiceRepo.Update(invoice);
        }

        public void Cancel(long id)
        {
            UpdateStatus(id, (int)InvoiceStatusType.Cancelled, (int)PaymentStatusType.Unpaid);
            var invoice = invoiceRepo.Get(id);
            invoice.IsActive = false;
            invoiceRepo.Update(invoice);
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
                if (line.RequestDetailId > 0 && testRequestRepo.Get(line.RequestDetailId) != null)
                {
                    continue;
                }

                var test = testRepo.Get(line.TestId);
                if (test == null)
                {
                    throw new InvalidOperationException(
                        $"Test id {line.TestId} was not found in HIS Test master.");
                }

                var request = testRequestRepo.Get(t =>
                    t.PatientId == invoice.PatientId &&
                    t.HISTestCode == test.HISTestCode &&
                    t.HISRequestNo == reqNo).FirstOrDefault();

                if (request == null)
                {
                    var sampleNo = !string.IsNullOrWhiteSpace(line.SampleNo)
                        ? line.SampleNo
                        : $"{reqNo}-{test.HISTestCode}";

                    request = new TestRequestDetail
                    {
                        PatientId = invoice.PatientId,
                        HISTestCode = test.HISTestCode,
                        HISTestName = test.HISTestCodeDescription,
                        HISRequestNo = reqNo,
                        HISRequestId = reqNo,
                        SampleNo = sampleNo,
                        SampleCollectionDate = now,
                        SampleReceivedDate = now,
                        SpecimenCode = test.HISSpecimenCode,
                        SpecimenName = test.HISSpecimenName,
                        ReportStatus = ReportStatusType.New,
                        CreatedOn = now,
                        CreatedBy = identity?.ActivityMember
                    };

                    request.Id = testRequestRepo.Add(request);
                }

                line.RequestDetailId = request.Id;

                if (line.RequestDetailId <= 0)
                {
                    throw new InvalidOperationException(
                        $"Could not link test request for test id {line.TestId}.");
                }
            }
        }

        public string GenerateInvoiceNo()
        {
            var prefix = $"INV-{DateTime.Now:yyyyMMdd}-";
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
            foreach (var line in lines)
            {
                if (line.Rate <= 0 && line.TestId > 0)
                {
                    var invoiceDate = header.InvoiceDate == default(DateTime) ? DateTime.Today : header.InvoiceDate;
                    var rate = rateManager.GetEffectiveRateForInvoice(
                        line.TestId,
                        invoiceDate,
                        header.CorporateId,
                        header.ReferralDoctorId);

                    if (rate != null)
                    {
                        line.Rate = rate.Rate;
                        if (line.DiscountAmount == 0 && rate.DiscountPercent > 0)
                        {
                            line.DiscountAmount = Math.Round(line.Rate * line.Quantity * rate.DiscountPercent / 100m, 2);
                        }
                        if (line.TaxAmount == 0 && rate.TaxPercent > 0)
                        {
                            line.TaxAmount = Math.Round(line.Rate * line.Quantity * rate.TaxPercent / 100m, 2);
                        }
                    }
                }

                line.Amount = Math.Round(line.Rate * line.Quantity, 2);
                line.NetAmount = Math.Round(line.Amount - line.DiscountAmount + line.TaxAmount, 2);
            }

            header.GrossAmount = lines.Sum(l => l.Amount);
            header.DiscountAmount = lines.Sum(l => l.DiscountAmount);
            header.TaxAmount = lines.Sum(l => l.TaxAmount);
            header.NetAmount = lines.Sum(l => l.NetAmount);
            header.DueAmount = header.NetAmount - header.PaidAmount;
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
            existing.GrossAmount = incoming.GrossAmount;
            existing.DiscountAmount = incoming.DiscountAmount;
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
}
