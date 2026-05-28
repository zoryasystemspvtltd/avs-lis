using LIS.DtoModel;
using LIS.DtoModel.Models;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LIS.Com.Businesslogic
{
    public class ErbaH560_TCPIPHL7Command : TCPIPHL7Command
    {
        public ErbaH560_TCPIPHL7Command(TCPIPSettings _settings) : base(_settings)
        { }

        public override async Task ResultProcess(string message, string messageControlId)
        {
            Logger.Logger.LogInstance.LogDebug("Result process method excuted.");

            string[] resultMesgSegments = message.TrimEnd((char)13).Split((char)13); // vbCr<CR>
            if (resultMesgSegments.Length > 1)
            {
                string[] field = resultMesgSegments[1].Split('|');
                if (field[0].Trim() == "OBR")
                {
                    string sampleNo = field[3];
                    if (resultMesgSegments.Length > 2)
                    {
                        await SaveResult(sampleNo, resultMesgSegments);
                    }
                }
            }
        }

        private async Task SaveResult(string sampleNo, string[] resultMesgSegments)
        {
            Result result = new Result();
            List<TestResultDetails> lsResult = new List<TestResultDetails>();
            TestResult testResult = new TestResult
            {
                ResultDate = DateAndTime.Now,
                SampleNo = sampleNo,
                LISTestCode = "CBC"
            };
            for (int i = 0; i < resultMesgSegments.Length; i++)
            {
                string[] field = resultMesgSegments[i].Split('|');

                if (field[0].Trim() == "OBX" && field[2] == "NM")
                {
                    switch (field[3].Split('^')[1])
                    {
                        case "WBC":
                        case "BAS#":
                        case "BAS%":
                        case "NEU#":
                        case "NEU%":
                        case "EOS#":
                        case "EOS%":
                        case "LYM#":
                        case "LYM%":
                        case "MON#":
                        case "MON%":
                        case "RBC":
                        case "HGB":
                        case "MCV":
                        case "MCH":
                        case "MCHC":
                        case "HCT":
                        case "PLT":
                        case "MPV":
                        case "PDW":
                        case "PCT":
                        case "RET#":
                        case "RET%":
                        case "RDW-CV":
                        case "NRBC#":
                        case "NRBC%":
                            var resultDetails = GetParameterResult(field);
                            lsResult.Add(resultDetails);
                            break;
                    }
                }
            }

            result.TestResult = testResult;
            result.ResultDetails = lsResult;
            Logger.Logger.LogInstance.LogDebug("ErabH560 Result posted to API for SampleNo: " + testResult.SampleNo);
            await LisContext.LisDOM.SaveTestResult(result);
        }
        private TestResultDetails GetParameterResult(string[] field)
        {
            var resdt = new TestResultDetails();

            var paramCode = field[3].Split('^')[1];
            string paramValue;
            switch (paramCode)
            {
                case "WBC":
                case "RET#":
                case "NRBC#":
                case "MON#":
                case "EOS#":
                case "BAS#":
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(field[5]))
                        {
                            paramValue = (Convert.ToDecimal(field[5]) * 1000).ToString();
                        }
                        else
                        {
                            paramValue = "";
                        }
                    }
                    catch (Exception)
                    {
                        paramValue = "";
                    }
                    break;

                default:
                    paramValue = field[5];
                    break;
            }

            resdt.LISParamCode = paramCode;
            resdt.LISParamValue = paramValue;
            resdt.LISParamUnit = field[6];

            return resdt;
        }
        public override async Task<OrderHL7Response> SendOrderData(string sampleNo, string messageControlId)
        {
            Logger.Logger.LogInstance.LogDebug("ErbaH560 generateORMField method started for SampleNo: " + sampleNo);
            string datetime = DateTime.Now.ToString("yyyyMMddhhmmss");
            string ORMMessage = string.Empty;
            string specialchar = @"^~\&";
            string message_MSH = $"MSH|{specialchar}|LIS||||{datetime}||ORR^O02|{messageControlId}|P|2.3.1||||||UNICODE{(char)13}";
            string message_MSA = $"MSA|AA|{messageControlId}{(char)13}";
            string DSRMessage = string.Empty;
            var response = new OrderHL7Response();

            IEnumerable<TestRequestDetail> testlist = await LisContext.LisDOM.GetTestRequestDetails(sampleNo);
            if (testlist != null && testlist.Count() > 0)
            {
                string testname = "";
                string patientClass = "MedicalInsurance";
                var firstTest = testlist.First();
                string patientLocation = "Pathology";
                string patientId = firstTest.Patient.Id + "^^^^MR";
                string gender = "";
                switch (firstTest.Patient.Gender)
                {
                    case "M":
                    case "MALE":
                        gender = "Male";
                        break;
                    case "F":
                    case "FEMALE":
                        gender = "Female";
                        break;
                }

                string DOB = firstTest.Patient.DateOfBirth.ToString("yyyyMMddhhmmss");
                var name = firstTest.Patient?.Name;
                if (name.Length > 48)
                {
                    name = name.Substring(0, 48);
                }


                for (int i = 0; i < testlist.Count(); i++)
                {
                    var test = testlist.ElementAt(i);
                    await LisContext.LisDOM.AcknowledgeSample(test.Id);
                    testname = test.LISTestCode;
                }

                string message_PID = $"PID|1||{patientId}||^{name}||{DOB}|{gender}{(char)13}";
                string message_PV1 = $"PV1|1|{patientClass}|{patientLocation}|||||||||||||||||{(char)13}";
                string message_ORC = $"ORC|AF|{sampleNo}||{(char)13}";
                string message_OBR = $"OBR|1|{sampleNo}|||||{datetime}|{datetime}|||||||{datetime}|||||||||HM|||||||{(char)13}";
                string message_OBX1 = $"OBX|1|IS|08001^Take Mode^99MRC||O||||||F{(char)13}";
                string message_OBX2 = $"OBX|2|IS|08002^Blood Mode^99MRC||W||||||F{(char)13}";
                string message_OBX3 = $"OBX|3|IS|08003^Test Mode^99MRC||{testname}||||||F{(char)13}";
                string message_OBX4 = $"OBX|4|IS|01002^Ref Group^99MRC||XXXX||||||F{(char)13}";
                string message_OBX5 = $"OBX|5|NM|30525-0^Age^LN||1|hr|||||F{(char)13}";
                string message_OBX6 = $"OBX|6|ST|01001^Remark^99MRC||20170809SYS0066||||||F{(char)13}";
                ORMMessage = message_MSH + message_MSA + message_PID + message_PV1 + message_ORC + message_OBR +
                    message_OBX1 + message_OBX2 + message_OBX3 + message_OBX4 + message_OBX5 + message_OBX6;

                response.QRYResponse = ORMMessage;
                response.DSRResponse = null;
                return response;
            }
            else
            {
                ORMMessage = message_MSH + message_MSA;
                response.QRYResponse = ORMMessage;
                response.DSRResponse = null;
                return response;
            }
        }
    }
}