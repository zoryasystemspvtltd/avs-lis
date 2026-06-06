using LIS.BusinessLogic.Helper;
using LIS.DtoModel.Interfaces;
using LIS.Logger;
using System;
using System.Linq;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    public class BarCodeController : ApiController
    {
        private IPatientDetailsManager patientManager;
        private ITestRequestDetailsManager testRequestDetails;
        private ILogger logger;
        private IResponseManager responseMgr;

        public BarCodeController(
            IPatientDetailsManager patientDetailsManager,
            ITestRequestDetailsManager testRequestDetailsManager,
            IResponseManager responseManager,
            ILogger Logger)
        {
            patientManager = patientDetailsManager;
            testRequestDetails = testRequestDetailsManager;
            responseMgr = responseManager;
            logger = Logger;
        }

        /// <summary>
        /// Get Patient name and test details
        /// </summary>
        /// <param name="Id">Sample Number</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public string Get(string Id)
        {
            try
            {
                var testRequests = testRequestDetails.GetTestRequestsBySampleNo(Id);
                if (testRequests == null)
                {
                    return string.Empty;
                }

                var sample = testRequests.FirstOrDefault();
                var sampleNumber = sample.SampleNo;


                var patientDetail = sample.Patient;
                if (patientDetail == null)
                {
                    return string.Empty;
                }

                string annotationText = string.Empty;
                string patient = $"{patientDetail.Name} {Convert.ToString(patientDetail.Age.ToString("0"))} {patientDetail.Gender.Substring(0, 1)}<br />";

                //var groupname = Helper.GetGroupName(sample.SampleNo);

                //var tests = string.Empty;
                //foreach (var test in testRequests)
                //{
                //    tests = $"{tests},{test.HISTestName}";
                //}
                //tests = tests.Trim(',');
                var test = testRequests.FirstOrDefault();
                annotationText = $"{patient}#{test?.BedNo}#{test?.IPNo}#{test.MRNo}";

                //var image = Helper.GeneratedBarcode(sampleNumber, annotationText);

                return annotationText;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }
    }
}
