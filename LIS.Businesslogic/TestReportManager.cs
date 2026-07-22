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
    public class TestReportManager : ITestReportManager
    {
        private readonly ModuleRepo<SaleInvoice> invoiceRepo;
        private readonly ModuleRepo<SaleInvoiceDetail> detailRepo;
        private readonly ModuleRepo<TestRequestDetail> requestRepo;
        private readonly ModuleRepo<TestResult> resultRepo;
        private readonly ModuleRepo<TestResultDetails> resultDetailsRepo;
        private readonly ModuleRepo<PatientDetail> patientRepo;
        private readonly ModuleRepo<PatientVisit> visitRepo;
        private readonly ModuleRepo<ReferralDoctorMaster> doctorRepo;
        private readonly ModuleRepo<CorporateMaster> corporateRepo;
        private readonly ModuleRepo<HISParameterMaster> parameterRepo;
        private readonly ModuleRepo<HISParameterRangMaster> rangeRepo;
        private readonly ModuleRepo<HisTestMaster> testRepo;
        private readonly ModuleRepo<Departments> departmentRepo;
        private readonly ModuleRepo<TestProfileMaster> profileRepo;
        private readonly ModuleRepo<TestProfileDetail> profileDetailRepo;
        private readonly ModuleRepo<TestMappingMaster> mappingRepo;
        private readonly ModuleRepo<TestParameterMappingMaster> testParamMappingRepo;
        private readonly ITestRequestDetailsManager testRequestManager;

        public TestReportManager(
            ILogger logger,
            IModuleIdentity identity,
            GenericUnitOfWork unitOfWork,
            ITestRequestDetailsManager testRequestDetailsManager)
        {
            invoiceRepo = new ModuleRepo<SaleInvoice>(logger, identity, unitOfWork);
            detailRepo = new ModuleRepo<SaleInvoiceDetail>(logger, identity, unitOfWork);
            requestRepo = new ModuleRepo<TestRequestDetail>(logger, identity, unitOfWork);
            resultRepo = new ModuleRepo<TestResult>(logger, identity, unitOfWork);
            resultDetailsRepo = new ModuleRepo<TestResultDetails>(logger, identity, unitOfWork);
            patientRepo = new ModuleRepo<PatientDetail>(logger, identity, unitOfWork);
            visitRepo = new ModuleRepo<PatientVisit>(logger, identity, unitOfWork);
            doctorRepo = new ModuleRepo<ReferralDoctorMaster>(logger, identity, unitOfWork);
            corporateRepo = new ModuleRepo<CorporateMaster>(logger, identity, unitOfWork);
            parameterRepo = new ModuleRepo<HISParameterMaster>(logger, identity, unitOfWork);
            rangeRepo = new ModuleRepo<HISParameterRangMaster>(logger, identity, unitOfWork);
            testRepo = new ModuleRepo<HisTestMaster>(logger, identity, unitOfWork);
            departmentRepo = new ModuleRepo<Departments>(logger, identity, unitOfWork);
            profileRepo = new ModuleRepo<TestProfileMaster>(logger, identity, unitOfWork);
            profileDetailRepo = new ModuleRepo<TestProfileDetail>(logger, identity, unitOfWork);
            mappingRepo = new ModuleRepo<TestMappingMaster>(logger, identity, unitOfWork);
            testParamMappingRepo = new ModuleRepo<TestParameterMappingMaster>(logger, identity, unitOfWork);
            testRequestManager = testRequestDetailsManager;
        }

        public DiagnosticTestReportDto GetDiagnosticTestReport(string labNo, string invoiceNo)
        {
            if (string.IsNullOrWhiteSpace(labNo) && string.IsNullOrWhiteSpace(invoiceNo))
            {
                throw new TestReportValidationException("Lab No or Sale Invoice No is required.");
            }

            var invoice = ResolveInvoice(labNo, invoiceNo);
            if (invoice == null)
            {
                throw new TestReportValidationException("Invalid Lab No or Invoice No. No matching invoice was found.");
            }

            if (invoice.InvoiceStatus == (int)InvoiceStatusType.Cancelled)
            {
                throw new TestReportValidationException("Invoice is cancelled. Test report cannot be printed.");
            }

            if (invoice.PaymentStatus != (int)PaymentStatusType.Paid)
            {
                throw new TestReportValidationException("Payment pending. Test report can only be printed after full payment is completed.");
            }

            var patient = patientRepo.Get(invoice.PatientId);
            if (patient == null)
            {
                throw new TestReportValidationException("Patient record not found for this invoice.");
            }

            var requests = ResolveTestRequests(invoice);
            if (!requests.Any())
            {
                throw new TestReportValidationException("No test bookings found for this invoice.");
            }

            ValidateWorkflow(requests);

            var header = BuildHeader(invoice, patient, requests);
            var sectionByRequestId = new Dictionary<long, DiagnosticTestReportSection>();

            foreach (var request in requests)
            {
                var section = BuildSection(request, patient);
                if (section != null && section.Parameters != null && section.Parameters.Any())
                {
                    sectionByRequestId[request.Id] = section;
                }
            }

            if (!sectionByRequestId.Any())
            {
                throw new TestReportValidationException("Test results are not available for printing.");
            }

            header.DoctorApprovalComment = BuildDoctorApprovalComment(requests);

            var grouped = BuildProfileGroupedSections(invoice, requests, sectionByRequestId);
            var departmentGroups = BuildDepartmentGroups(invoice, requests, sectionByRequestId);

            return new DiagnosticTestReportDto
            {
                Header = header,
                ProfileGroups = grouped.ProfileGroups,
                DepartmentGroups = departmentGroups,
                Sections = grouped.StandaloneSections
            };
        }

        private (List<DiagnosticTestReportProfileGroup> ProfileGroups, List<DiagnosticTestReportSection> StandaloneSections)
            BuildProfileGroupedSections(
                SaleInvoice invoice,
                List<TestRequestDetail> requests,
                Dictionary<long, DiagnosticTestReportSection> sectionByRequestId)
        {
            var invoiceLines = detailRepo.Get()
                .Where(d => d.SaleInvoiceId == invoice.Id && d.IsActive)
                .OrderBy(d => d.Id)
                .ToList();

            var profileLines = invoiceLines
                .Where(d => d.TestProfileId.HasValue && d.TestProfileId > 0)
                .ToList();

            if (!profileLines.Any())
            {
                return (null, OrderSections(requests, sectionByRequestId));
            }

            var assignedRequestIds = new HashSet<long>();
            var profileGroups = new List<DiagnosticTestReportProfileGroup>();

            foreach (var line in profileLines)
            {
                var profile = profileRepo.Get(line.TestProfileId.Value);
                if (profile == null)
                {
                    continue;
                }

                var profileDetails = profileDetailRepo.Get(d => d.TestProfileId == profile.Id)
                    .OrderBy(d => d.Id)
                    .ToList();

                var profileTestCodes = profileDetails
                    .Select(d => testRepo.Get(d.TestId)?.HISTestCode)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToList();

                var groupSections = new List<DiagnosticTestReportSection>();
                foreach (var testCode in profileTestCodes)
                {
                    var request = requests.FirstOrDefault(r =>
                        !assignedRequestIds.Contains(r.Id) &&
                        r.HISTestCode != null &&
                        r.HISTestCode.Equals(testCode, StringComparison.OrdinalIgnoreCase) &&
                        sectionByRequestId.ContainsKey(r.Id));

                    if (request != null)
                    {
                        groupSections.Add(sectionByRequestId[request.Id]);
                        assignedRequestIds.Add(request.Id);
                    }
                }

                if (groupSections.Any())
                {
                    profileGroups.Add(new DiagnosticTestReportProfileGroup
                    {
                        ProfileName = profile.Name,
                        ProfileCode = profile.Code,
                        Sections = groupSections
                    });
                }
            }

            var standalone = OrderSections(
                requests.Where(r => !assignedRequestIds.Contains(r.Id)).ToList(),
                sectionByRequestId);

            return (profileGroups.Any() ? profileGroups : null, standalone);
        }

        private List<DiagnosticTestReportSection> OrderSections(
            List<TestRequestDetail> requests,
            Dictionary<long, DiagnosticTestReportSection> sectionByRequestId)
        {
            var orderLookup = BuildInvoiceTestOrderLookup(requests);
            return requests
                .Where(r => sectionByRequestId.ContainsKey(r.Id))
                .OrderBy(r => orderLookup.ContainsKey(r.Id) ? orderLookup[r.Id] : int.MaxValue)
                .ThenBy(r => r.Id)
                .Select(r => sectionByRequestId[r.Id])
                .ToList();
        }

        /// <summary>
        /// Groups printable sections by department. Department sequence follows Sale Invoice
        /// booking order (first appearance of a test from that department), not alphabetically.
        /// </summary>
        private List<DiagnosticTestReportDepartmentGroup> BuildDepartmentGroups(
            SaleInvoice invoice,
            List<TestRequestDetail> requests,
            Dictionary<long, DiagnosticTestReportSection> sectionByRequestId)
        {
            var orderLookup = BuildInvoiceTestOrderLookup(requests);
            var orderedSections = requests
                .Where(r => sectionByRequestId.ContainsKey(r.Id))
                .OrderBy(r => orderLookup.ContainsKey(r.Id) ? orderLookup[r.Id] : int.MaxValue)
                .ThenBy(r => r.Id)
                .Select(r => sectionByRequestId[r.Id])
                .ToList();

            if (!orderedSections.Any())
            {
                return null;
            }

            var groups = new List<DiagnosticTestReportDepartmentGroup>();
            var indexByDept = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var section in orderedSections)
            {
                var deptName = string.IsNullOrWhiteSpace(section.Department)
                    ? "General"
                    : section.Department.Trim();

                int idx;
                if (!indexByDept.TryGetValue(deptName, out idx))
                {
                    idx = groups.Count;
                    indexByDept[deptName] = idx;
                    groups.Add(new DiagnosticTestReportDepartmentGroup
                    {
                        DepartmentName = deptName,
                        Sections = new List<DiagnosticTestReportSection>()
                    });
                }

                groups[idx].Sections.Add(section);
            }

            return groups;
        }

        /// <summary>
        /// Maps TestRequestDetail.Id → booking sequence from SaleInvoiceDetail (and request create order as fallback).
        /// </summary>
        private Dictionary<long, int> BuildInvoiceTestOrderLookup(List<TestRequestDetail> requests)
        {
            var lookup = new Dictionary<long, int>();
            if (requests == null || !requests.Any())
            {
                return lookup;
            }

            // Prefer invoice line order via RequestDetailId.
            var requestIds = requests.Select(r => r.Id).ToList();
            var lines = detailRepo.Get()
                .Where(d => d.IsActive && d.RequestDetailId.HasValue && requestIds.Contains(d.RequestDetailId.Value))
                .OrderBy(d => d.Id)
                .ToList();

            var seq = 0;
            foreach (var line in lines)
            {
                var rid = line.RequestDetailId.Value;
                if (!lookup.ContainsKey(rid))
                {
                    lookup[rid] = seq++;
                }
            }

            // Remaining requests: preserve CreatedOn then Id.
            foreach (var request in requests.OrderBy(r => r.CreatedOn).ThenBy(r => r.Id))
            {
                if (!lookup.ContainsKey(request.Id))
                {
                    lookup[request.Id] = seq++;
                }
            }

            return lookup;
        }

        private SaleInvoice ResolveInvoice(string labNo, string invoiceNo)
        {
            if (!string.IsNullOrWhiteSpace(invoiceNo))
            {
                return FindInvoiceByNumber(invoiceNo.Trim());
            }

            return FindInvoiceByNumber(labNo.Trim());
        }

        private SaleInvoice FindInvoiceByNumber(string number)
        {
            var key = (number ?? string.Empty).Trim();
            var invoice = invoiceRepo.Get()
                .Where(i => i.IsActive && i.InvoiceNo != null && i.InvoiceNo.Equals(key, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.Id)
                .FirstOrDefault();

            if (invoice != null)
            {
                return invoice;
            }

            var request = requestRepo.Get()
                .Where(r => r.HISRequestNo != null && r.HISRequestNo.Equals(key, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.Id)
                .FirstOrDefault();

            if (request == null || string.IsNullOrWhiteSpace(request.HISRequestNo))
            {
                return null;
            }

            return invoiceRepo.Get()
                .Where(i => i.IsActive && i.InvoiceNo != null && i.InvoiceNo.Equals(request.HISRequestNo, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.Id)
                .FirstOrDefault();
        }

        private List<TestRequestDetail> ResolveTestRequests(SaleInvoice invoice)
        {
            var invNo = invoice.InvoiceNo ?? string.Empty;
            var merged = new Dictionary<long, TestRequestDetail>();

            var requestIds = detailRepo.Get()
                .Where(d => d.SaleInvoiceId == invoice.Id && d.IsActive && d.RequestDetailId.HasValue && d.RequestDetailId.Value > 0)
                .Select(d => d.RequestDetailId.Value)
                .Distinct()
                .ToList();

            if (requestIds.Any())
            {
                foreach (var request in requestRepo.Get().Where(r => requestIds.Contains(r.Id)))
                {
                    merged[request.Id] = request;
                }
            }

            if (!string.IsNullOrWhiteSpace(invNo))
            {
                foreach (var request in requestRepo.Get()
                    .Where(r => r.HISRequestNo != null && r.HISRequestNo.Equals(invNo, StringComparison.OrdinalIgnoreCase)))
                {
                    merged[request.Id] = request;
                }
            }

            return merged.Values.ToList();
        }

        private void ValidateWorkflow(List<TestRequestDetail> requests)
        {
            var notApproved = requests.Where(r => r.ReportStatus != ReportStatusType.DoctorApproved).ToList();
            if (notApproved.Any())
            {
                var names = string.Join(", ", notApproved.Select(r => r.HISTestName ?? r.HISTestCode ?? "Test"));
                throw new TestReportValidationException($"Report not approved for: {names}. Doctor approval is required before printing.");
            }

            foreach (var request in requests)
            {
                var result = resultRepo.Get()
                    .FirstOrDefault(r => r.TestRequestId == request.Id);
                if (result == null)
                {
                    throw new TestReportValidationException($"Results not ready for test: {request.HISTestName ?? request.HISTestCode}.");
                }

                var hasValues = resultDetailsRepo.Get()
                    .Any(d => d.TestResultId == result.Id);
                if (!hasValues)
                {
                    throw new TestReportValidationException($"Results not ready for test: {request.HISTestName ?? request.HISTestCode}.");
                }
            }
        }

        private DiagnosticTestReportHeader BuildHeader(SaleInvoice invoice, PatientDetail patient, List<TestRequestDetail> requests)
        {
            string doctorName = invoice.RefDoctorName;
            if (string.IsNullOrWhiteSpace(doctorName) && invoice.ReferralDoctorId.HasValue)
            {
                var doc = doctorRepo.Get(invoice.ReferralDoctorId.Value);
                doctorName = doc?.Name;
            }

            string corporateName = null;
            if (invoice.CorporateId.HasValue)
            {
                var corp = corporateRepo.Get(invoice.CorporateId.Value);
                corporateName = corp?.Name;
            }

            var firstRequest = requests.OrderBy(r => r.SampleCollectionDate).FirstOrDefault();
            var latestResult = requests
                .Select(r => resultRepo.Get(res => res.TestRequestId == r.Id).FirstOrDefault())
                .Where(r => r != null)
                .OrderByDescending(r => r.AuthorizationDate ?? r.ResultDate)
                .FirstOrDefault();

            DateTime? receivedDate = null;
            var receivedCandidates = requests
                .Where(r => r.SampleReceivedDate > DateTime.MinValue)
                .Select(r => (DateTime?)r.SampleReceivedDate)
                .ToList();
            if (receivedCandidates.Any())
            {
                receivedDate = receivedCandidates.Min();
            }

            return new DiagnosticTestReportHeader
            {
                LabNo = firstRequest?.HISRequestNo ?? invoice.InvoiceNo,
                InvoiceNo = invoice.InvoiceNo,
                PatientName = patient.Name,
                PatientId = patient.HisPatientId,
                MRNo = patient.MRNo,
                VisitId = ResolveReportVisitId(invoice, patient, requests),
                Age = patient.Age,
                Gender = patient.Gender,
                ReferralDoctor = doctorName,
                Corporate = corporateName,
                CollectionDate = firstRequest?.SampleCollectionDate,
                ReceivedDate = receivedDate,
                ReportDate = latestResult?.AuthorizationDate ?? latestResult?.ResultDate ?? DateTime.Now,
                Status = "Final",
                ApprovedBy = latestResult?.AuthorizedBy
            };
        }

        private DiagnosticTestReportSection BuildSection(TestRequestDetail request, PatientDetail patient)
        {
            var review = testRequestManager.GetTestResultByRequestId(request.Id);
            if (review?.Test == null)
            {
                return null;
            }

            var approvedRun = review.TestRuns?
                .Where(r => r.ReportStatus == ReportStatusType.DoctorApproved)
                .OrderByDescending(r => r.ReviewDate)
                .FirstOrDefault();

            var values = approvedRun?.TestValues?.ToList() ?? new List<TestValues>();
            if (!values.Any())
            {
                return null;
            }

            var testCode = request.HISTestCode ?? string.Empty;
            var testEntity = testRepo.Get()
                .FirstOrDefault(t => t.HISTestCode != null && t.HISTestCode.Equals(testCode, StringComparison.OrdinalIgnoreCase));
            var department = request.Department;
            if (string.IsNullOrWhiteSpace(department) && testEntity != null)
            {
                var dept = departmentRepo.Get(d => d.Code == testEntity.DepartmentCode).FirstOrDefault();
                department = dept?.Name;
            }

            var parameters = values.Select(v => MapParameter(v, request.HISTestCode, patient)).Where(p => p != null).ToList();
            ApplyParameterSectionNames(parameters);
            var comment = BuildTestComment(request.HISTestCode, parameters);

            return new DiagnosticTestReportSection
            {
                TestCode = request.HISTestCode,
                TestName = FormatTestHeading(review.Test.TestName ?? request.HISTestName, review.Test.SpecimenName ?? request.SpecimenName),
                Specimen = review.Test.SpecimenName ?? request.SpecimenName,
                SampleNo = request.SampleNo,
                Department = department,
                Comment = comment,
                Parameters = parameters
            };
        }

        private static string FormatTestHeading(string testName, string specimen)
        {
            var name = (testName ?? string.Empty).Trim();
            var spec = (specimen ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return spec;
            }
            if (string.IsNullOrWhiteSpace(spec))
            {
                return name;
            }
            // Avoid duplicating specimen when already part of the test name.
            if (name.IndexOf(spec, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return name;
            }
            return name + ", " + spec.ToUpperInvariant();
        }

        private string BuildTestComment(string testCode, List<DiagnosticTestReportParameter> parameters)
        {
            var comments = new List<string>();
            if (parameters == null || !parameters.Any())
            {
                return null;
            }

            // Parameter Master is independent of Test — resolve comments by parameter code
            // (and by Test↔Parameter mapping when available), not by HISTestCode alone.
            var masters = new List<HISParameterMaster>();

            foreach (var parameter in parameters)
            {
                var master = ResolveParameterMaster(testCode, parameter.ParameterCode, parameter.ParameterCode);
                if (master != null && !string.IsNullOrWhiteSpace(master.Comments))
                {
                    masters.Add(master);
                }
            }

            // Also include mapped parameters for this test that have comments (even if not in result set codes mismatch).
            if (!string.IsNullOrWhiteSpace(testCode))
            {
                var testEntity = testRepo.Get()
                    .FirstOrDefault(t => t.HISTestCode != null
                        && t.HISTestCode.Equals(testCode, StringComparison.OrdinalIgnoreCase));
                if (testEntity != null)
                {
                    var mappedIds = testParamMappingRepo.Get(m => m.IsActive && m.HisTestId == testEntity.Id)
                        .Select(m => m.HisParameterId)
                        .Distinct()
                        .ToList();

                    foreach (var paramId in mappedIds)
                    {
                        var mapped = parameterRepo.Get(paramId);
                        if (mapped != null
                            && !string.IsNullOrWhiteSpace(mapped.Comments)
                            && !masters.Any(m => m.Id == mapped.Id))
                        {
                            // Only include if this mapped parameter appears on the printed result set.
                            var onReport = parameters.Any(p =>
                                (!string.IsNullOrWhiteSpace(mapped.HISParamCode)
                                    && p.ParameterCode != null
                                    && p.ParameterCode.Equals(mapped.HISParamCode, StringComparison.OrdinalIgnoreCase))
                                || (!string.IsNullOrWhiteSpace(mapped.LISParamCode)
                                    && p.ParameterCode != null
                                    && p.ParameterCode.Equals(mapped.LISParamCode, StringComparison.OrdinalIgnoreCase)));
                            if (onReport)
                            {
                                masters.Add(mapped);
                            }
                        }
                    }
                }
            }

            foreach (var master in masters)
            {
                var text = StripHtmlToPlain(master.Comments);
                if (!string.IsNullOrWhiteSpace(text) && !comments.Any(c => c.Equals(text, StringComparison.OrdinalIgnoreCase)))
                {
                    comments.Add(text.Trim());
                }
            }

            return comments.Any() ? string.Join(Environment.NewLine, comments) : null;
        }

        /// <summary>
        /// Resolves Parameter Master without requiring HISTestCode (Parameter Master is test-independent).
        /// Prefers a row whose HISTestCode matches when present.
        /// </summary>
        private HISParameterMaster ResolveParameterMaster(string testCode, string hisParamCode, string lisParamCode)
        {
            if (string.IsNullOrWhiteSpace(hisParamCode) && string.IsNullOrWhiteSpace(lisParamCode))
            {
                return null;
            }

            var hisCode = (hisParamCode ?? string.Empty).Trim();
            var lisCode = (lisParamCode ?? string.Empty).Trim();

            var candidates = parameterRepo.Get()
                .AsEnumerable()
                .Where(p =>
                    (!string.IsNullOrWhiteSpace(hisCode)
                        && p.HISParamCode != null
                        && p.HISParamCode.Trim().Equals(hisCode, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(lisCode)
                        && p.LISParamCode != null
                        && p.LISParamCode.Trim().Equals(lisCode, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(lisCode)
                        && p.HISParamCode != null
                        && p.HISParamCode.Trim().Equals(lisCode, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(hisCode)
                        && p.LISParamCode != null
                        && p.LISParamCode.Trim().Equals(hisCode, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (!candidates.Any())
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(testCode))
            {
                var byTest = candidates.FirstOrDefault(p =>
                    !string.IsNullOrWhiteSpace(p.HISTestCode)
                    && p.HISTestCode.Trim().Equals(testCode, StringComparison.OrdinalIgnoreCase));
                if (byTest != null)
                {
                    return byTest;
                }
            }

            // Prefer a row that has comments when duplicates exist.
            return candidates.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Comments))
                ?? candidates.FirstOrDefault();
        }

        /// <summary>
        /// Collects distinct doctor approval notes from authorized TestResult rows for this invoice.
        /// </summary>
        private string BuildDoctorApprovalComment(List<TestRequestDetail> requests)
        {
            if (requests == null || !requests.Any())
            {
                return null;
            }

            var notes = new List<string>();
            foreach (var request in requests.OrderBy(r => r.Id))
            {
                var result = resultRepo.Get(r => r.TestRequestId == request.Id)
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefault();

                if (result == null || string.IsNullOrWhiteSpace(result.DoctorNote))
                {
                    continue;
                }

                var text = StripHtmlToPlain(result.DoctorNote);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                text = text.Trim();
                if (!notes.Any(n => n.Equals(text, StringComparison.OrdinalIgnoreCase)))
                {
                    notes.Add(text);
                }
            }

            return notes.Any() ? string.Join(Environment.NewLine + Environment.NewLine, notes) : null;
        }

        private static string StripHtmlToPlain(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }
            var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
            text = System.Net.WebUtility.HtmlDecode(text);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
            return text;
        }

        /// <summary>
        /// Applies optional section headings from Analyzer Parameter Mapping GroupName when
        /// a test has multiple distinct groups (e.g. Physical / Biochemical). Does not hardcode names.
        /// </summary>
        private void ApplyParameterSectionNames(List<DiagnosticTestReportParameter> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return;
            }

            var codes = parameters
                .Select(p => p.ParameterCode)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!codes.Any())
            {
                return;
            }

            var mappings = mappingRepo.Get(m => m.IsActive && m.HISParamCode != null)
                .AsEnumerable()
                .Where(m => codes.Any(c => c.Equals(m.HISParamCode, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var groupByCode = mappings
                .Where(m => !string.IsNullOrWhiteSpace(m.GroupName))
                .GroupBy(m => m.HISParamCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().GroupName.Trim(), StringComparer.OrdinalIgnoreCase);

            if (groupByCode.Count == 0)
            {
                return;
            }

            var distinctGroups = groupByCode.Values
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Only emit section headers when grouping adds structure (2+ distinct groups).
            if (distinctGroups.Count < 2)
            {
                return;
            }

            foreach (var parameter in parameters)
            {
                string group;
                if (!string.IsNullOrWhiteSpace(parameter.ParameterCode)
                    && groupByCode.TryGetValue(parameter.ParameterCode, out group))
                {
                    parameter.SectionName = group;
                }
            }
        }

        private DiagnosticTestReportParameter MapParameter(TestValues value, string testCode, PatientDetail patient)
        {
            if (value == null)
            {
                return null;
            }

            var row = new DiagnosticTestReportParameter
            {
                ParameterCode = value.HISParamCode ?? value.LISParamCode,
                ParameterName = value.HISParamName ?? value.HISParamCode ?? value.LISParamCode,
                ResultValue = value.ParamValue,
                Unit = value.ParamUnit
            };

            var paramMaster = ResolveParameterMaster(
                testCode,
                value.HISParamCode ?? row.ParameterCode,
                value.LISParamCode);

            ApplyReferenceRange(row, paramMaster, patient, value);

            return row;
        }

        private void ApplyReferenceRange(DiagnosticTestReportParameter row, HISParameterMaster paramMaster, PatientDetail patient, TestValues source)
        {
            HISParameterRangMaster matchedRange = null;

            if (paramMaster != null)
            {
                var ranges = rangeRepo.Get(r => r.HisParameterId == paramMaster.Id).ToList();
                matchedRange = ranges
                    .Where(r => MatchesPatientRange(r, patient))
                    .OrderByDescending(r => r.MinValue > 0 || r.MaxValue > 0)
                    .ThenBy(r => r.Id)
                    .FirstOrDefault();
            }

            if (matchedRange == null && source.HISRangeValues != null && source.HISRangeValues.Length > 0)
            {
                row.ReferenceRange = string.Join("; ", source.HISRangeValues.Where(v => !string.IsNullOrWhiteSpace(v)));
            }
            else if (matchedRange != null)
            {
                // Prefer Parameter Range Master "Range Value"; fall back to numeric min/max.
                if (!string.IsNullOrWhiteSpace(matchedRange.HISRangeValue))
                {
                    row.ReferenceRange = matchedRange.HISRangeValue.Trim();
                }
                else if (matchedRange.MinValue > 0 || matchedRange.MaxValue > 0)
                {
                    row.ReferenceRange = $"{FormatDecimal(matchedRange.MinValue)} - {FormatDecimal(matchedRange.MaxValue)}";
                }
            }

            if (matchedRange != null && TryParseResult(row.ResultValue, out var numeric))
            {
                if (matchedRange.MinValue > 0 && numeric < matchedRange.MinValue)
                {
                    row.Flag = "L";
                    row.IsAbnormal = true;
                }
                else if (matchedRange.MaxValue > 0 && numeric > matchedRange.MaxValue)
                {
                    row.Flag = "H";
                    row.IsAbnormal = true;
                }
            }
        }

        private static bool MatchesPatientRange(HISParameterRangMaster range, PatientDetail patient)
        {
            if (range == null || patient == null)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(range.Gender) &&
                !string.IsNullOrWhiteSpace(patient.Gender) &&
                !GenderMatches(range.Gender, patient.Gender))
            {
                return false;
            }

            if (range.AgeFrom > 0 || range.AgeTo > 0)
            {
                var age = patient.Age;
                if (age < range.AgeFrom || (range.AgeTo > 0 && age > range.AgeTo))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool GenderMatches(string rangeGender, string patientGender)
        {
            var rg = rangeGender.Trim().ToUpperInvariant();
            var pg = patientGender.Trim().ToUpperInvariant();
            if (rg.StartsWith("M") && pg.StartsWith("M")) return true;
            if (rg.StartsWith("F") && pg.StartsWith("F")) return true;
            return rg == pg;
        }

        private static bool TryParseResult(string value, out decimal numeric)
        {
            numeric = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var cleaned = value.Trim().Replace(",", "");
            return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out numeric);
        }

        private static string FormatDecimal(decimal value)
        {
            return value % 1 == 0 ? value.ToString("0", CultureInfo.InvariantCulture) : value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        public IEnumerable<TestReportLabNoOption> GetPrintableLabNumbers()
        {
            var approvedRequests = requestRepo
                .Get(r => r.ReportStatus == ReportStatusType.DoctorApproved && r.HISRequestNo != null && r.HISRequestNo != "")
                .OrderByDescending(r => r.CreatedOn)
                .ToList();

            var options = new Dictionary<string, TestReportLabNoOption>(StringComparer.OrdinalIgnoreCase);

            foreach (var request in approvedRequests)
            {
                var labNo = request.HISRequestNo.Trim();
                if (options.ContainsKey(labNo))
                {
                    continue;
                }

                var invoice = FindInvoiceByNumber(labNo);
                if (invoice == null || invoice.PaymentStatus != (int)PaymentStatusType.Paid)
                {
                    continue;
                }

                var result = resultRepo.Get(r => r.TestRequestId == request.Id).FirstOrDefault();
                if (result == null || !resultDetailsRepo.Get(d => d.TestResultId == result.Id).Any())
                {
                    continue;
                }

                var patient = patientRepo.Get(invoice.PatientId);
                var patientName = patient?.Name ?? string.Empty;
                options[labNo] = new TestReportLabNoOption
                {
                    LabNo = labNo,
                    InvoiceNo = invoice.InvoiceNo,
                    PatientName = patientName,
                    DisplayLabel = string.IsNullOrWhiteSpace(patientName) ? labNo : $"{labNo} — {patientName}"
                };
            }

            return options.Values.OrderByDescending(o => o.LabNo).ToList();
        }

        private string ResolveReportVisitId(SaleInvoice invoice, PatientDetail patient, List<TestRequestDetail> requests)
        {
            var visitId = PatientVisitManager.ResolveVisitId(visitRepo, patientRepo, invoice.PatientVisitId, patient.Id);
            if (!string.IsNullOrWhiteSpace(visitId))
            {
                return visitId;
            }

            var requestVisitId = requests?
                .Select(r => r.PatientVisitId)
                .FirstOrDefault(v => v.HasValue && v.Value > 0);

            return PatientVisitManager.ResolveVisitId(visitRepo, patientRepo, requestVisitId, patient.Id);
        }
    }
}
