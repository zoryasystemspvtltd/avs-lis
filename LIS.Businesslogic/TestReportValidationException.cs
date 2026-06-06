using System;

namespace LIS.BusinessLogic
{
    public class TestReportValidationException : Exception
    {
        public TestReportValidationException(string message) : base(message) { }
    }
}
