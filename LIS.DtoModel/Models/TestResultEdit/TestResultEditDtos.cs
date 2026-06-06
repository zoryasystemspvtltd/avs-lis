using LIS.DtoModel.Models;
using System;
using System.Collections.Generic;

namespace LIS.DtoModel.Models.TestResultEdit
{
    public class TestResultEditSearchOptions
    {
        public string SampleNo { get; set; }
        public string InvoiceNo { get; set; }
        public string PatientName { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class TestResultEditSearchRow
    {
        public string SampleNo { get; set; }
        public string InvoiceNo { get; set; }
        public string PatientName { get; set; }
        public DateTime? CollectionDate { get; set; }
        public int ReportStatus { get; set; }
        public string ReportStatusLabel { get; set; }
        public bool HasResults { get; set; }
    }

    public class TestResultEditSampleDto
    {
        public string SampleNo { get; set; }
        public string InvoiceNo { get; set; }
        public string PatientName { get; set; }
        public string PatientId { get; set; }
        public string Age { get; set; }
        public string Gender { get; set; }
        public bool CanEditAny { get; set; }
        public bool IsAdministrator { get; set; }
        public IList<TestResultEditTestDto> Tests { get; set; }
    }

    public class TestResultEditTestDto
    {
        public long TestRequestId { get; set; }
        public long TestResultId { get; set; }
        public string HisTestCode { get; set; }
        public string HisTestName { get; set; }
        public string EquipmentName { get; set; }
        public int ReportStatus { get; set; }
        public string ReportStatusLabel { get; set; }
        public DateTime? ResultDate { get; set; }
        public bool CanEdit { get; set; }
        public IList<TestResultEditParameterDto> Parameters { get; set; }
    }

    public class TestResultEditParameterDto
    {
        public long DetailId { get; set; }
        public string ParameterCode { get; set; }
        public string ParameterName { get; set; }
        public string ResultValue { get; set; }
        public string Unit { get; set; }
        public string ReferenceRange { get; set; }
        public string Flag { get; set; }
        public bool IsAbnormal { get; set; }
        public string Method { get; set; }
        public bool IsEditable { get; set; }
    }

    public class TestResultEditSaveRequest
    {
        public long TestResultId { get; set; }
        public long TestRequestId { get; set; }
        public IList<TestResultEditParameterSaveDto> Parameters { get; set; }
    }

    public class TestResultEditParameterSaveDto
    {
        public long DetailId { get; set; }
        public string ResultValue { get; set; }
        public string Remark { get; set; }
    }

    public class TestResultEditSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ReportStatus { get; set; }
        public string ReportStatusLabel { get; set; }
    }
}
