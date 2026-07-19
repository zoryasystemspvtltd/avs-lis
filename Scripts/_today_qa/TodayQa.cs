using System;
using System.Collections.Generic;
using System.Linq;
using LIS.Businesslogic;
using LIS.BusinessLogic;
using LIS.DataAccess;
using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;

namespace TodayQaApp
{
    class Identity : IModuleIdentity
    {
        public string AccessKey { get { return "QA"; } }
        public string ActivityMember { get { return "qa-e2e"; } }
    }

    class StubFile : IFileHandler
    {
        public List<TestNameItem> GetJsonMappings(string model) { return new List<TestNameItem>(); }
        public string[] GetModels() { return new string[0]; }
    }

    class Program
    {
        static int failures = 0;

        static void Check(bool ok, string name, string detail = null)
        {
            Console.WriteLine((ok ? "PASS " : "FAIL ") + name + (detail == null ? "" : " | " + detail));
            if (!ok) { failures++; }
        }

        static int Main(string[] args)
        {
            var ts = DateTime.Now.ToString("HHmmss");
            var db = new ApplicationDBContext();
            var uow = new GenericUnitOfWork(db);
            var logger = Logger.LogInstance;
            var identity = new Identity();

            // ---- T1: Parameter Master Comments CRUD (rich text round-trip) ----
            try
            {
                var pm = new HisParameterMasterManager(logger, identity, uow);
                var code = "QAC" + ts;
                var rich = "<p><strong>Bold</strong> note &amp; <em>italic</em></p>";
                var id = pm.Add(new HISParameterMaster
                {
                    HISParamCode = code,
                    HISParamDescription = "QA Comment Param " + ts,
                    HISParamUnit = "mg/dL",
                    Comments = rich
                });
                var fetched = pm.GetById((int)id);
                Check(fetched != null && fetched.Comments == rich, "T1a ParamMaster Add+Get Comments", fetched == null ? "null" : fetched.Comments);

                fetched.Comments = "<p>Updated <em>rich</em> comment</p>";
                pm.Update(fetched);
                var refetched = pm.GetById((int)id);
                Check(refetched != null && refetched.Comments == "<p>Updated <em>rich</em> comment</p>", "T1b ParamMaster Update Comments");

                pm.Delete(refetched);
                Check(pm.GetById((int)id) == null, "T1c ParamMaster Delete");
            }
            catch (Exception ex) { Check(false, "T1 ParamMaster Comments CRUD", ex.Message); }

            // ---- T2: Method Master 300-char Name CRUD ----
            try
            {
                var mm = new MethodManager(logger, identity, uow);
                var longName = ("QA Method " + ts + " ").PadRight(295, 'X');
                var mid = mm.Add(new MethodMaster { Code = "QM" + ts, Name = longName });
                var mFetched = mm.GetById((int)mid);
                Check(mFetched != null && mFetched.Name == longName && mFetched.Name.Length == 295,
                    "T2a Method Add+Get 295-char name", mFetched == null ? "null" : ("len=" + mFetched.Name.Length));

                mFetched.Name = longName.Substring(0, 290) + "-EDIT";
                mm.Update(mFetched);
                var mRe = mm.GetById((int)mid);
                Check(mRe != null && mRe.Name.EndsWith("-EDIT"), "T2b Method Update long name");

                var methodRepo = new ModuleRepo<MethodMaster>(logger, identity, uow);
                methodRepo.Delete(mRe);
                Check(true, "T2c Method cleanup");
            }
            catch (Exception ex) { Check(false, "T2 Method 300 CRUD", ex.Message); }

            // ---- T3: Test Parameter Mapping — search, sequence order, validation ----
            try
            {
                var tpm = new TestParameterMappingCrudManager(logger, identity, uow);

                var all = tpm.Get(new ListOptions { RecordPerPage = 1000, CurrentPage = 1 });
                var ordered = true;
                string firstBad = null;
                foreach (var grp in all.Items.GroupBy(m => (m.HISTestCode ?? "").ToUpperInvariant()))
                {
                    var seqs = grp.Select(m => m.Sequence).ToList();
                    for (int i = 1; i < seqs.Count; i++)
                    {
                        if (seqs[i] < seqs[i - 1]) { ordered = false; firstBad = grp.Key; break; }
                    }
                    if (!ordered) { break; }
                }
                Check(all.TotalRecord > 0 && ordered, "T3a TPM list ordered by Test then ascending Sequence",
                    "total=" + all.TotalRecord + (firstBad == null ? "" : " badGroup=" + firstBad));

                foreach (var term in new[] { "LIPID", "Lipid Profile", "Cholesterol" })
                {
                    var found = tpm.Get(new ListOptions { RecordPerPage = 50, CurrentPage = 1, SearchText = term });
                    Check(found.TotalRecord > 0, "T3b TPM search '" + term + "'", "hits=" + found.TotalRecord);
                }
                var noise = tpm.Get(new ListOptions { RecordPerPage = 50, CurrentPage = 1, SearchText = "ZZQQ-NO-MATCH" });
                Check(noise.TotalRecord == 0, "T3c TPM search no-match returns empty");

                var testNameOk = all.Items.All(m => !string.IsNullOrEmpty(m.HISTestCodeDescription));
                Check(testNameOk, "T3d TPM rows enriched with Test Name");

                try
                {
                    tpm.Add(new TestParameterMappingMaster { HisTestId = 1, HisParameterId = 1, Sequence = 0 });
                    Check(false, "T3e TPM rejects Sequence=0", "no exception thrown");
                }
                catch (InvalidOperationException ex)
                {
                    Check(ex.Message.IndexOf("Sequence", StringComparison.OrdinalIgnoreCase) >= 0,
                        "T3e TPM rejects Sequence=0", ex.Message);
                }
            }
            catch (Exception ex) { Check(false, "T3 TPM suite", ex.Message); }

            // ---- Locate a real sample whose test has >=2 sequenced mappings and saved results ----
            var mappingRepo = new ModuleRepo<TestParameterMappingMaster>(logger, identity, uow);
            var testRepo = new ModuleRepo<HisTestMaster>(logger, identity, uow);
            var paramRepo = new ModuleRepo<HISParameterMaster>(logger, identity, uow);
            var resultRepo = new ModuleRepo<TestResult>(logger, identity, uow);
            var detailRepo = new ModuleRepo<TestResultDetails>(logger, identity, uow);

            var mapsByTest = mappingRepo.Get(m => m.IsActive).AsEnumerable()
                .GroupBy(m => m.HisTestId)
                .Where(g => g.Count() >= 2)
                .ToDictionary(g => g.Key, g => g.ToList());
            var testsById = testRepo.Get().ToDictionary(t => t.Id, t => t);
            var paramsById = paramRepo.Get().ToDictionary(p => p.Id, p => p);

            TestResult targetResult = null;
            List<TestParameterMappingMaster> targetMaps = null;
            foreach (var kv in mapsByTest)
            {
                HisTestMaster t;
                if (!testsById.TryGetValue(kv.Key, out t)) { continue; }
                var res = resultRepo.Get(r => r.HISTestCode == t.HISTestCode)
                    .AsEnumerable()
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefault(r => detailRepo.Get(d => d.TestResultId == r.Id).Count() >= 2);
                if (res != null)
                {
                    targetResult = res;
                    targetMaps = kv.Value;
                    break;
                }
            }

            if (targetResult == null)
            {
                Check(false, "T4/T5 sample discovery", "no sample with >=2 sequenced mapped results found");
            }
            else
            {
                Console.WriteLine("INFO sample=" + targetResult.SampleNo + " test=" + targetResult.HISTestCode
                    + " requestId=" + targetResult.TestRequestId);
                var seqByCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var m in targetMaps)
                {
                    HISParameterMaster p;
                    if (paramsById.TryGetValue(m.HisParameterId, out p))
                    {
                        if (!string.IsNullOrEmpty(p.HISParamCode) && !seqByCode.ContainsKey(p.HISParamCode)) { seqByCode[p.HISParamCode] = m.Sequence; }
                        if (!string.IsNullOrEmpty(p.LISParamCode) && !seqByCode.ContainsKey(p.LISParamCode)) { seqByCode[p.LISParamCode] = m.Sequence; }
                    }
                }

                // ---- T4: Approval screens (Technician/Doctor/Approved) parameter order ----
                try
                {
                    var trdm = new TestRequestDetailsManager(logger, identity, uow, new StubFile());
                    var review = trdm.GetTestResultByRequestId(targetResult.TestRequestId);
                    Check(review != null && review.TestRuns != null && review.TestRuns.Any(), "T4a approval details load");
                    var orderedOk = true;
                    foreach (var run in review.TestRuns)
                    {
                        var seqs = run.TestValues
                            .Select(v =>
                            {
                                int s;
                                if (v.HISParamCode != null && seqByCode.TryGetValue(v.HISParamCode, out s)) { return s; }
                                if (v.LISParamCode != null && seqByCode.TryGetValue(v.LISParamCode, out s)) { return s; }
                                return int.MaxValue;
                            })
                            .ToList();
                        for (int i = 1; i < seqs.Count; i++)
                        {
                            if (seqs[i] < seqs[i - 1]) { orderedOk = false; break; }
                        }
                        Console.WriteLine("INFO run=" + run.RunIndex + " order=[" + string.Join(",",
                            run.TestValues.Select(v => (v.HISParamCode ?? v.LISParamCode))) + "] seq=[" + string.Join(",", seqs) + "]");
                    }
                    Check(orderedOk, "T4b approval TestValues ascending Sequence");
                }
                catch (Exception ex) { Check(false, "T4 approval ordering", ex.Message); }

                // ---- T5: Test Result Entry/Edit parameter order ----
                try
                {
                    var trem = new TestResultEditManager(logger, identity, uow);
                    var dto = trem.GetBySampleNo(targetResult.SampleNo, true);
                    var testDto = dto.Tests.FirstOrDefault(t =>
                        string.Equals(t.HisTestCode, targetResult.HISTestCode, StringComparison.OrdinalIgnoreCase));
                    Check(testDto != null, "T5a result-edit sample load", "tests=" + dto.Tests.Count);
                    if (testDto != null)
                    {
                        var seqs = testDto.Parameters
                            .Select(p =>
                            {
                                int s;
                                if (p.ParameterCode != null && seqByCode.TryGetValue(p.ParameterCode, out s)) { return s; }
                                return int.MaxValue;
                            })
                            .ToList();
                        var orderedOk = true;
                        for (int i = 1; i < seqs.Count; i++)
                        {
                            if (seqs[i] < seqs[i - 1]) { orderedOk = false; break; }
                        }
                        Console.WriteLine("INFO editParams=[" + string.Join(",", testDto.Parameters.Select(p => p.ParameterCode)) + "] seq=[" + string.Join(",", seqs) + "]");
                        Check(orderedOk, "T5b result-edit parameters ascending Sequence");
                    }
                }
                catch (Exception ex) { Check(false, "T5 result-edit ordering", ex.Message); }
            }

            Console.WriteLine(failures == 0 ? "ALL_PASS" : ("FAILURES=" + failures));
            return failures == 0 ? 0 : 1;
        }
    }
}
