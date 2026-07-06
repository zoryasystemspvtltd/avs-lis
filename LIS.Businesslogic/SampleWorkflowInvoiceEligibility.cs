using LIS.DataAccess.Repo;
using LIS.DtoModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.Businesslogic
{
    /// <summary>
    /// Resolves which TestRequestDetail rows are eligible for sample collection/receiving
    /// based on linked Sale Invoice status (Confirmed or Paid; not Draft or Cancelled).
    /// </summary>
    internal static class SampleWorkflowInvoiceEligibility
    {
        public const string NotConfirmedMessage =
            "Sample is not available until the sale invoice is confirmed.";

        public static HashSet<long> GetEligibleRequestIds(
            ModuleRepo<SaleInvoice> invoiceRepo,
            ModuleRepo<SaleInvoiceDetail> invoiceDetailRepo,
            ModuleRepo<TestRequestDetail> requestRepo)
        {
            var eligibleInvoices = invoiceRepo.Get(i =>
                i.IsActive &&
                i.InvoiceStatus >= (int)InvoiceStatusType.Confirmed &&
                i.InvoiceStatus != (int)InvoiceStatusType.Cancelled).ToList();

            if (!eligibleInvoices.Any())
            {
                return new HashSet<long>();
            }

            var eligibleInvoiceIds = eligibleInvoices.Select(i => i.Id).ToHashSet();
            var eligibleInvoiceNos = eligibleInvoices
                .Select(i => i.InvoiceNo)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var requestIds = invoiceDetailRepo.Get(d =>
                    eligibleInvoiceIds.Contains(d.SaleInvoiceId) &&
                    d.RequestDetailId.HasValue &&
                    d.RequestDetailId.Value > 0)
                .Select(d => d.RequestDetailId.Value)
                .ToHashSet();

            var requestsByInvoiceNo = requestRepo.Get()
                .Where(r => !string.IsNullOrWhiteSpace(r.HISRequestNo) && eligibleInvoiceNos.Contains(r.HISRequestNo))
                .Select(r => r.Id);

            foreach (var id in requestsByInvoiceNo)
            {
                requestIds.Add(id);
            }

            return requestIds;
        }

        public static bool IsRequestEligible(
            long requestId,
            ModuleRepo<SaleInvoice> invoiceRepo,
            ModuleRepo<SaleInvoiceDetail> invoiceDetailRepo,
            ModuleRepo<TestRequestDetail> requestRepo)
        {
            if (requestId <= 0)
            {
                return false;
            }

            return GetEligibleRequestIds(invoiceRepo, invoiceDetailRepo, requestRepo).Contains(requestId);
        }

        public static void EnsureRequestEligible(
            long requestId,
            ModuleRepo<SaleInvoice> invoiceRepo,
            ModuleRepo<SaleInvoiceDetail> invoiceDetailRepo,
            ModuleRepo<TestRequestDetail> requestRepo)
        {
            if (!IsRequestEligible(requestId, invoiceRepo, invoiceDetailRepo, requestRepo))
            {
                throw new InvalidOperationException(NotConfirmedMessage);
            }
        }
    }
}
