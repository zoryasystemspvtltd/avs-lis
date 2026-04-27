using LIS.DtoModel.Models;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace LIS.DtoModel.Interfaces
{
    public interface ITestRequestDetailsManager
    {
        long Add(TestRequestDetail testRequestDetail);
        void Update(TestRequestDetail testRequestDetail);
        IEnumerable<TestRequestDetail> Get(long Id);
        IEnumerable<TestRequestDetail> GetBySampleNo(string SampleNo, ReportStatusType status);
        IEnumerable<TestRequestDetail> GetAllNewSamples(ReportStatusType status);
        IEnumerable<TestRequestDetail> GetByHisRequestNo(string RequestNo, ReportStatusType status);
        bool IsPanelTest(string SampleNo, string LisHostCode);
        List<TestRequestDetail> GetRequestDetails(string SampleNo, string lisTestCode);
        void Delete(TestRequestDetail testRequestDetail);

        void TechnicianReview(long Id, ReportStatusType reportStatusType, string note, long recentTestRequestId);
        void DoctorReview(long Id, ReportStatusType reportStatusType, string note, long recentTestRequestId);

        bool Ping();

        bool UpdateStatus(long Id, ReportStatusType status);

        IEnumerable<NameValue> DailySampleSummary();
        IEnumerable<NameValue> DailyTechnicianApprovalSummary();
        IEnumerable<NameValue> DailyDoctorApprovalSummary();
        ReviewTest GetTestResultByRequestId(long RequestId);
        long[] GetTestResultByRequestId(string SampleNumber);
        IEnumerable<TestResultDetails> GetTestResultDetailsByRequestId(long ResultId);

        TestRequestDetail GetTestRequestByRequestId(long RequestId);
        IEnumerable<TestRequestDetail> GetTestRequestsBySampleNo(string SampleNo);

        IEnumerable<TestParameter> GetTestParametersByRequestId(long RequestId);
    }
}
