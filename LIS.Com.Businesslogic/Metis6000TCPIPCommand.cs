using LIS.DtoModel;
using LIS.DtoModel.Models;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LIS.Com.Businesslogic
{
    public class Metis6000TCPIPCommand : TCPIPHL7Command
    {
        public Metis6000TCPIPCommand(TCPIPSettings _settings) : base(_settings)
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
                    string sampleNo = field[2];

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
                SampleNo = sampleNo
            };
            string lisTestCode = string.Empty;
            for (int i = 0; i < resultMesgSegments.Length; i++)
            {
                string[] field = resultMesgSegments[i].Split('|');

                if (field[0].Trim() == "OBX" && field[2] == "NM")
                {
                    var resultDetails = new TestResultDetails();
                    var paramCode = field[3].ToString();
                    var paramValue = field[5].ToString();
                    lisTestCode = paramCode;
                    resultDetails.LISParamCode = paramCode;
                    resultDetails.LISParamValue = paramValue;
                    resultDetails.LISParamUnit = field[6];
                    lsResult.Add(resultDetails);
                }
            }
            testResult.LISTestCode = lisTestCode;
            result.TestResult = testResult;
            result.ResultDetails = lsResult;
            Logger.Logger.LogInstance.LogDebug("CL1200i Result posted to API for SampleNo: " + testResult.SampleNo);
            await LisContext.LisDOM.SaveTestResult(result);
        }

        public override async Task<OrderHL7Response> SendOrderData(string sampleNo, string messageControlId)
        {
            Logger.Logger.LogInstance.LogDebug("CL1200i generateORMField method started for SampleNo: " + sampleNo);
            string datetime = DateTime.Now.ToString("yyyyMMddhhmmss");
            string specialchar = @"^~\&";
            string message_MSH = $"MSH|{specialchar}|||||{datetime}||DSR^Q03|{messageControlId}|P|2.3.1||||||UTF8|||{(char)13}";
            string message_MSA = $"MSA|AA|{messageControlId}|Message accepted|||0|{(char)13}";
            string message_err = $"ERR|0|{(char)13}";
            string message_qak = string.Empty;
            string message_DSP = string.Empty;
            string DSRMessage, QRYMessage;
            var response = new OrderHL7Response();

            IEnumerable<TestRequestDetail> testlist = await LisContext.LisDOM.GetTestRequestDetails(sampleNo);
            if (testlist != null && testlist.Count() > 0)
            {

                var firstTest = testlist.First();
                var specimen = firstTest.SpecimenName.ToLower();
                var name = firstTest.Patient?.Name;
                string gender = "";
                switch (firstTest.Patient.Gender.ToUpperInvariant())
                {
                    case "MALE":
                        gender = "M";
                        break;
                    case "FEMALE":
                        gender = "F";
                        break;
                    default:
                        gender = "O";
                        break;
                }
                var dob = firstTest.Patient.DateOfBirth.ToString("yyyyMMddhhmmss");

                if (name.Length > 40)
                {
                    name = name.Substring(0, 39);
                }
                for (int i = 1; i <= 28; i++)
                {
                    switch (i)
                    {
                        case 3:
                            message_DSP += $"DSP|{i}||{name}|||{(char)13}";
                            break;
                        case 4:
                            message_DSP += $"DSP|{i}||{dob}|||{(char)13}";
                            break;
                        case 5:
                            message_DSP += $"DSP|{i}||{gender}|||{(char)13}";
                            break;
                        case 21:
                            message_DSP += $"DSP|{i}||{sampleNo}|||{(char)13}";
                            break;
                        case 24:
                            message_DSP += $"DSP|{i}||N|||{(char)13}";
                            break;
                        case 26:
                            message_DSP += $"DSP|{i}||{specimen}|||{(char)13}";
                            break;
                        default:
                            message_DSP += $"DSP|{i}|||||{(char)13}";
                            break;
                    }
                }
                for (int i = 0; i < testlist.Count(); i++)
                {
                    int j = 29 + i;
                    var test = testlist.ElementAt(i);
                    var testname = test.LISTestCode + "^^^";
                    var ackSent = await LisContext.LisDOM.AcknowledgeSample(test.Id);
                    message_DSP += $"DSP|{j}||{testname}|||{(char)13}";
                }
                message_qak = $"QAK|SR|OK|{(char)13}";
                string message_QRD = $"QRD|{datetime}|R|D|2|||RD|{sampleNo}|OTH|||T|{(char)13}";
                string message_QRF = $"QRF||{datetime}|{datetime}|||RCT|COR|ALL||{(char)13}";
                string message_DSC = $"DSC||{(char)13}";

                DSRMessage = message_MSH + message_MSA + message_err + message_qak + message_QRD + message_QRF +
                    message_DSP + message_DSC;

                QRYMessage = SendResponse("OK", messageControlId);
                response.QRYResponse = QRYMessage;
                response.DSRResponse = DSRMessage;
                return response;
            }
            else
            {
                QRYMessage = SendResponse("NF", messageControlId);
                response.QRYResponse = QRYMessage;
                response.DSRResponse = null;
                return response;
            }
        }

        public override string SendResponse(string qak, string messageControlId)
        {
            string datetime = DateTime.Now.ToString("yyyyMMddhhmmss");
            string specialchar = @"^~\&";
            string message_MSH = $"MSH|{specialchar}|||||{datetime}||QCK^Q02|{messageControlId}|P|2.3.1||||||UTF8|||{(char)13}";
            string message_MSA = $"MSA|AA|{messageControlId}|Message accepted|||0|{(char)13}";
            string message_err = $"ERR|0|{(char)13}";
            string message_qak = $"QAK|SR|{qak}|{(char)13}";

            var response = message_MSH + message_MSA + message_err + message_qak;
            return response;
        }
    }
}