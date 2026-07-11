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

namespace RangeQaApp
{
    class Identity : IModuleIdentity
    {
        public string AccessKey { get { return "QA"; } }
        public string ActivityMember { get { return "qa"; } }
    }

    class StubFile : IFileHandler
    {
        public List<TestNameItem> GetJsonMappings(string model) { return new List<TestNameItem>(); }
        public string[] GetModels() { return new string[0]; }
    }

    class Program
    {
        static int Main(string[] args)
        {
            long requestId = long.Parse(args[0]);
            var db = new ApplicationDBContext();
            var uow = new GenericUnitOfWork(db);
            var logger = Logger.LogInstance;
            var identity = new Identity();
            var mgr = new TestRequestDetailsManager(logger, identity, uow, new StubFile());
            var review = mgr.GetTestResultByRequestId(requestId);
            if (review == null || review.TestRuns == null)
            {
                Console.WriteLine("FAIL REVIEW_NULL");
                return 2;
            }
            var vals = review.TestRuns.SelectMany(r => r.TestValues ?? Enumerable.Empty<TestValues>()).ToList();
            var withRanges = vals.Where(v => v.HISRangeValues != null && v.HISRangeValues.Any(x => !string.IsNullOrWhiteSpace(x))).ToList();
            Console.WriteLine("REQUEST=" + requestId + " PARAMS=" + vals.Count + " WITH_RANGES=" + withRanges.Count);
            foreach (var v in withRanges.Take(8))
            {
                Console.WriteLine("RANGE " + (v.HISParamCode ?? v.LISParamCode) + " => " + string.Join(" ;; ", v.HISRangeValues));
            }
            if (withRanges.Count == 0)
            {
                Console.WriteLine("FAIL NO_RANGE_VALUES");
                return 3;
            }
            // Prefer asserting HISRangeValue text is present for known ids
            var flat = string.Join(" ", withRanges.SelectMany(v => v.HISRangeValues ?? new string[0]));
            if (requestId == 1574 && flat.IndexOf("0-100", StringComparison.OrdinalIgnoreCase) < 0)
            {
                Console.WriteLine("FAIL EXPECTED_0-100 missing in: " + flat);
                return 4;
            }
            if (requestId == 1575 && flat.IndexOf("0.6", StringComparison.OrdinalIgnoreCase) < 0)
            {
                Console.WriteLine("FAIL EXPECTED_0.6-1.2 missing in: " + flat);
                return 4;
            }
            Console.WriteLine("PASS_APPROVAL_RANGES");
            return 0;
        }
    }
}
