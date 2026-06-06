using LIS.DtoModel;
using LIS.DtoModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LIS.Com.Businesslogic
{
    public class EMA200_TCPIPASTMCommand : TCPIPASTMCommand
    {
        public EMA200_TCPIPASTMCommand(TCPIPSettings _settings) : base(_settings)
        {

        }

        public override async Task CreateMessageAsync(string message)
        {
            //Remove <CHK1>,<CHK2> character from raw message
            message = message.Replace("<CHK1>", "9");
            message = message.Replace("<CHK2>", "D");
            Logger.Logger.LogInstance.LogDebug("EMA200 CreateMessage method started. '{0}'", message);
            string formattedmessage = "";
            string[] segments;
            try
            {
                segments = message.Split((char)10);  // Chr(10)
                for (int i = 0; i <= segments.Length - 1; i++)
                {
                    for (int j = 2; j <= segments[i].Length - 5; j++)
                    {
                        if (j != segments[i].Length - 5 | segments[i].ToString()[j + 1] != (char)23)
                            formattedmessage += segments[i][j];
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.LogInstance.LogException("EMA200 CreateMessage method exception:", ex);
            }
            await Identify(formattedmessage);
            Logger.Logger.LogInstance.LogDebug("EMA200 CreateMessage method completed");
        }
        public async Task Identify(string message)
        {
            Logger.Logger.LogInstance.LogDebug("EMA200 Identify method started");
            Logger.Logger.LogInstance.LogDebug("EMA200 Identify method Data:" + message);
            List<string> sampleList = new List<string>();

            string[] segments = message.Split((char)13); // <CR>
            try
            {
                if (segments.Length > 1)
                {
                    if (segments[1].Substring(0, 1).ToUpper() == "Q")
                    {
                        string[] queryFields = segments[1].Split('|');
                        string sampleID = queryFields[2];
                        await SendOrderData(sampleID);
                    }
                    else if (segments[1].Substring(0, 1).ToUpper() == "P")
                    {
                        Logger.Logger.LogInstance.LogDebug("Patient Info started");
                        for (int i = 0; i <= segments.Length - 2; i++)
                        {
                            if (segments[i].Substring(0, 1).ToUpper() == "O")
                            {
                                string sSpecimenId = segments[i].Split('|')[2];
                                sampleList.Add(sSpecimenId.Split('^')[0]);
                            }
                        }
                        await ParseMessageAsync(message);
                    }
                }
                Logger.Logger.LogInstance.LogDebug("EMA200 Identify method completed");
            }
            catch (Exception ex)
            {
                Logger.Logger.LogInstance.LogException("EMA200 Identify method exception:", ex);
            }
        }

        public async Task SendOrderData(string sampleId)
        {
            try
            {
                Logger.Logger.LogInstance.LogDebug("EMA200 SendOrderData method started for SampleNo: " + sampleId);

                string datetime = DateTime.Now.AddMinutes(-30).ToString("yyyyMMddhhmmss");
                string specialchar = @"\^&";
                string headerSegment = $"1H|{specialchar}|||ZoryaLIS|||||||E-1394-97|{datetime}{(char)13}";
                string orderSegment = $"3O|1|{sampleId}||";
                var trailerSegment = "";
                IEnumerable<TestRequestDetail> testlist = await LisContext.LisDOM.GetTestRequestDetails(sampleId);

                if (testlist.Count() > 0)
                {
                    string firstName;
                    string lastName;
                    string middleName;
                    string collectiondate;
                    string specimen;
                    string dob;
                    string sex;
                    string patientId;
                    var testname = string.Empty;
                    trailerSegment = $"4L|1|N{(char)13}";

                    var firstTest = testlist.First();

                    specimen = firstTest.SpecimenName.ToUpper();
                    patientId = firstTest.Patient?.Id.ToString();
                    collectiondate = firstTest.SampleCollectionDate.ToString("yyyyMMddhhmmss");
                    dob = firstTest.Patient.DateOfBirth.ToString("yyyyMMddhhmmss");
                    sex = firstTest.Patient.Gender;
                    var fullName = firstTest.Patient?.Name;
                    (firstName, lastName, middleName) = GetName(fullName);
                    if (firstName.Length > 20)
                    {
                        firstName = firstName.Substring(0, 19);
                    }

                    for (int i = 0; i < testlist.Count();)
                    {
                        var test = testlist.ElementAt(i);
                        var ackSent = await LisContext.LisDOM.AcknowledgeSample(test.Id);
                        testname += "^^^" + test.LISTestCode;
                        i++;
                        if (testlist.Count() == i)
                            break;
                        else
                            testname += @"`";
                    }

                    string patientSegment = $"2P|1|{patientId}|||{lastName}^{firstName}^{middleName}||{dob}|{sex}|{(char)13}";
                    orderSegment += $"{testname}|R||{collectiondate}||||N||||{specimen}||||||||||O|{(char)13}";

                    output[0] = ((char)5).ToString();
                    output[1] = headerSegment;
                    Logger.Logger.LogInstance.LogDebug("EMA200 Header Segment {0}", headerSegment);
                    output[2] = patientSegment;
                    Logger.Logger.LogInstance.LogDebug("EMA200 Patient Segment {0}", patientSegment);
                    output[3] = orderSegment;
                    Logger.Logger.LogInstance.LogDebug("EMA200 Order Segment {0}", orderSegment);
                    output[4] = trailerSegment;
                    Logger.Logger.LogInstance.LogDebug("EMA200 Trailer Segment {0}", trailerSegment);

                    index = 0;
                }
                else//no test order
                {
                    output[2] = ((char)5).ToString();
                    output[3] = headerSegment;
                    Logger.Logger.LogInstance.LogDebug("EMA200 Header Segment {0}", headerSegment);
                    trailerSegment = $"2L|1|N{(char)13}";
                    output[4] = trailerSegment;
                    Logger.Logger.LogInstance.LogDebug("EMA200 Trailer Segment {0}", trailerSegment);

                    index = 2;
                }

                WriteResponseSafe("" + (char)5, false);
                Logger.Logger.LogInstance.LogDebug("EMA200 SendOrderData method completed for SampleNo: " + sampleId);
            }
            catch (Exception ex)
            {
                Logger.Logger.LogInstance.LogException("EMA200 SendOrderData method exception:", ex);
            }
        }

        private (string, string, string) GetName(string fullName)
        {
            string firstName = string.Empty;
            string lastName = string.Empty;
            string middleName = string.Empty;

            if (string.IsNullOrWhiteSpace(fullName))
                return (string.Empty, string.Empty, string.Empty);

            var nameParts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (nameParts.Length == 4)
            {
                firstName = nameParts[1];
                middleName = nameParts[2];
                lastName = nameParts[3];
            }
            else if (nameParts.Length == 3)
            {
                firstName = nameParts[0];
                middleName = nameParts[1];
                lastName = nameParts[2];
            }
            else if (nameParts.Length == 2)
            {
                firstName = nameParts[0];
                lastName = nameParts[1];
            }
            else if (nameParts.Length > 0)
            {
                firstName = nameParts[0];

                if (nameParts.Length > 1)
                    middleName = nameParts[1];

                if (nameParts.Length > 2)
                    lastName = nameParts[2];
            }
            else
            {
                firstName = string.Empty;
            }

            // Truncate to max 20 chars
            firstName = Truncate(firstName, 20);
            middleName = Truncate(middleName, 20);
            lastName = Truncate(lastName, 20);

            return (firstName, lastName, middleName);
        }
        static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length > maxLength
                ? value.Substring(0, maxLength)
                : value;
        }
        private async Task ParseMessageAsync(string message)
        {
            try
            {
                Logger.Logger.LogInstance.LogDebug("EMA200 ParseMessage method started");
                Result result = new Result();
                List<TestResultDetails> lsResult = new List<TestResultDetails>();

                string[] record = message.Split((char)13); // <CR>
                string sampleNo = string.Empty, listTestCode = string.Empty;
                for (int index = 0; index <= record.Length - 1; index++)
                {
                    if (record[index].Length < 5) continue;

                    string[] field = record[index].Split('|');
                    switch (field[0].Trim())
                    {
                        case "O":
                            {
                                string sampleField = field[3];
                                sampleNo = sampleField.Trim();
                                break;
                            }

                        case "R":
                            {
                                string[] parameter = field[2].Split('^');
                                string paramCode = parameter[3];
                                if (paramCode != "")
                                {
                                    listTestCode = paramCode;
                                    TestResultDetails resultDetails = new TestResultDetails
                                    {
                                        LISParamCode = paramCode,
                                        LISParamValue = field[3],
                                        LISParamUnit = field[4]
                                    };
                                    Logger.Logger.LogInstance.LogDebug("EMA200 Result processed for SampleNo " + sampleNo + " and Parameter " + paramCode);
                                    lsResult.Add(resultDetails);
                                }
                                else
                                    continue;

                                break;
                            }
                    }
                }
                TestResult testResult = new TestResult
                {
                    ResultDate = DateTime.Now,
                    SampleNo = sampleNo,
                    LISTestCode = listTestCode
                };

                result.TestResult = testResult;
                result.ResultDetails = lsResult;

                Logger.Logger.LogInstance.LogDebug("EMA200 Result posted to API for SampleNo: " + testResult.SampleNo);
                await LisContext.LisDOM.SaveTestResult(result);


                Logger.Logger.LogInstance.LogDebug("EMA200 ParseMessage method completed");
            }
            catch (Exception ex)
            {
                Logger.Logger.LogInstance.LogException("EMA200 ParseMessage method exception:", ex);
            }
        }

    }
}