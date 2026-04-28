using LIS.DtoModel;
using LIS.DtoModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LIS.Com.Businesslogic
{
    public class BS430TCPIPHL7Command : TCPIPHL7Command
    {
        public BS430TCPIPHL7Command(TCPIPSettings _settings) : base(_settings)
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
            List<LisTestValue> lsResult = new List<LisTestValue>();
            for (int i = 0; i < resultMesgSegments.Length; i++)
            {
                string[] field = resultMesgSegments[i].Split('|');

                if (field[0].Trim() == "OBX" && field[2] == "NM")
                {
                    var resultDetails = new LisTestValue();
                    var paramCode = field[3].ToString();
                    var paramValue = field[5].ToString();
                    resultDetails.PARAMCODE = paramCode;
                    resultDetails.Value = paramValue;
                    resultDetails.REF_VISITNO = sampleNo;
                    lsResult.Add(resultDetails);
                }
            }

            Logger.Logger.LogInstance.LogDebug("BS430 Result posted to API for SampleNo: " + lsResult[0].REF_VISITNO);
            await LisContext.LisDOM.SaveTestResult(lsResult);
        }

        public override async Task<OrderHL7Response> SendOrderData(string sampleNo, string messageControlId)
        {
            Logger.Logger.LogInstance.LogDebug("BS430 generateORMField method started for SampleNo: " + sampleNo);
            string datetime = DateTime.Now.ToString("yyyyMMddhhmmss");
            string specialchar = @"^~\&";
            string message_MSH = $"MSH|{specialchar}|||||{datetime}||DSR^Q03|{messageControlId}|P|2.3.1||||||ASCII|||{(char)13}";
            string message_MSA = $"MSA|AA|{messageControlId}|Message accepted|||0|{(char)13}";
            string message_err = $"ERR|0|{(char)13}";
            string message_qak = string.Empty;
            string message_DSP = string.Empty;
            string DSRMessage, QRYMessage;
            var response = new OrderHL7Response();
            IEnumerable<AccuHealthSample> testlist = await LisContext.LisDOM.GetTestRequestDetails(sampleNo);
            if (testlist != null && testlist.Count() > 0)
            {

                var firstTest = testlist.First();
                var specimen = firstTest.SPECIMEN.ToLower();
                var name = firstTest.PATFNAME;
                var gender = firstTest.GENDER;
                var dob = firstTest.PAT_DOB;
                if (dob != null)
                {
                    dob = Convert.ToDateTime(dob).ToString("yyyyMMddhhmmss");
                }
                else
                {
                    dob = "";
                }
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
                    var testname = test.LisParamCode + "^^^";
                    message_DSP += $"DSP|{j}||{testname}|||{(char)13}";
                }
                message_qak = $"QAK|SR|OK|{(char)13}";
                string message_QRD = $"QRD|{datetime}|R|D|54|||RD|{sampleNo}|OTH|||T|{(char)13}";
                string message_QRF = $"QRF||{datetime}|{datetime}|||RCT|COR|ALL||{(char)13}";
                string message_DSC = $"DSC||{(char)13}";

                DSRMessage = message_MSH + message_MSA + message_err + message_qak + message_QRD + message_QRF + message_DSP + message_DSC;

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
            string message_MSH = $"MSH|{specialchar}|||Mindray|BS430|{datetime}||QCK^Q02|{messageControlId}|P|2.3.1||||||ASCII|||{(char)13}";
            string message_MSA = $"MSA|AA|{messageControlId}|Message accepted|||0|{(char)13}";
            string message_qak = $"QAK|SR|{qak}|{(char)13}";
            string message_err = $"ERR|0|{(char)13}";
            var response = message_MSH + message_MSA + message_err + message_qak;
            return response;
        }
    }
}