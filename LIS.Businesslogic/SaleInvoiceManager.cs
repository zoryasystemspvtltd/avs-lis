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
            var lines = dto.Details ?? new List<SaleInvoiceDetail>();
            Recalculate(header, lines);

            var now = DateTime.Now;
            if (header.Id == 0)
            {
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

                foreach (var line in lines)
                {
                    line.SaleInvoiceId = id;
                    line.CreatedOn = now;
                    line.CreatedBy = identity?.ActivityMember;
                    line.IsActive = true;
                    detailRepo.Add(line);
                }

                return id;
            }

            header.ModifiedOn = now;
            header.ModifiedBy = identity?.ActivityMember;
            invoiceRepo.Update(header);

            var existing = detailRepo.Get(d => d.SaleInvoiceId == header.Id).ToList();
            foreach (var old in existing)
            {
                detailRepo.Delete(old);
            }

            foreach (var line in lines)
            {
                line.SaleInvoiceId = header.Id;
                line.CreatedOn = now;
                line.CreatedBy = identity?.ActivityMember;
                line.IsActive = true;
                detailRepo.Add(line);
            }

            return header.Id;
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
                    var rate = rateManager.GetEffectiveRate(
                        line.TestId,
                        header.CorporateId.HasValue ? (int)RateType.Corporate :
                        header.ReferralDoctorId.HasValue ? (int)RateType.ReferralDoctor : (int)RateType.Standard,
                        header.CorporateId,
                        header.ReferralDoctorId,
                        null);

                    if (rate != null)
                    {
                        line.Rate = rate.Rate;
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
