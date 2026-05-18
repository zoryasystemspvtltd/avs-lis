using LIS.DtoModel.Models;
using System;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface ITestRateMasterManager : IMasterCrudManager<TestRateMaster>
    {
        TestRateMaster GetEffectiveRate(int testId, int rateType, int? corporateId, int? referralDoctorId, int? profileId, DateTime? effectiveOn = null);

        /// <summary>Resolves rate by priority: Corporate → Referral Doctor → Profile → Emergency → Standard.</summary>
        TestRateMaster GetEffectiveRateForInvoice(int testId, DateTime invoiceDate, int? corporateId, int? referralDoctorId, int? profileId = null, bool useEmergency = false);

        IEnumerable<TestRateMaster> GetByTestId(int testId);
    }
}
