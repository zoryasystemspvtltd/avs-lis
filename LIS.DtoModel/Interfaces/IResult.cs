using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface IResult
    {
        TestResult TestResult { get; set; }
        IEnumerable<TestResultDetails> ResultDetails { get; set; }
    }
}
