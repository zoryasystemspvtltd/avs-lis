using Newtonsoft.Json;
using System;
using System.Security.Cryptography;

namespace Lis.Api.Providers
{
    public class Helper
    {
        public static string GetHash(string input)
        {
            HashAlgorithm hashAlgorithm = new SHA256CryptoServiceProvider();

            byte[] byteValue = System.Text.Encoding.UTF8.GetBytes(input);

            byte[] byteHash = hashAlgorithm.ComputeHash(byteValue);

            return Convert.ToBase64String(byteHash);
        }

        public static string GetSerilizedObject<T>(T value) where T: class
        {
            return JsonConvert.SerializeObject(value);
        }
    }
}