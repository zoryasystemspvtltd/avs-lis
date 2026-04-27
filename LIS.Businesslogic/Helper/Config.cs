using System;
using System.Configuration;

namespace LIS.BusinessLogic.Helper
{
    public static class Config
    {
        public static string ExternalAPIBaseUri = "ExternalAPIBaseUri";
        public static string TestRequestUri = "TestRequestUri";
        public static string TestAckUri = "TestAckUri";
        public static string TestResultUri = "TestResultUri";
        public static string ExternalAPIUserId = "ExternalAPIUserId";
        public static string ExternalAPIPassword = "ExternalAPIPassword";
        public static string OrderParameters = "OrderParameters";

        public static string GetConfigValue(string Key)
        {
            return Convert.ToString(ConfigurationManager.AppSettings[Key]);
        }
    }
}
