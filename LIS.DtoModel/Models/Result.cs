using LIS.DtoModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIS.DtoModel.Models
{
    public class Result : IResult
    {
        public TestResult TestResult { get; set; }
        public IEnumerable<TestResultDetails> ResultDetails { get; set; }
    }

    public class HeartBeatStatus
    {
        public bool IsAlive { get; set; }
    }
}
