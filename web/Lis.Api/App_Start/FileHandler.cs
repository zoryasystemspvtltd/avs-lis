using LIS.DtoModel;
using LIS.DtoModel.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;

namespace Lis.Api
{
    public class FileHandler : IFileHandler
    {
        public List<TestNameItem> GetJsonMappings(string model)
        {
            List<TestNameItem> testnames = new List<TestNameItem>();
            var path = HostingEnvironment.MapPath($"~/App_Data/Models/{model}.json");
            using (StreamReader sr = new StreamReader(path))
            {
                testnames = JsonConvert.DeserializeObject<List<TestNameItem>>(sr.ReadToEnd());
            }

            return testnames;
        }

        public string[] GetModels()
        {
            List<string> models = new List<string>();
            var path = HostingEnvironment.MapPath("~/App_Data/Models/");
            var availableFiles =  Directory.GetFiles(path, "*.json");
            foreach(var model in availableFiles)
            {
                models.Add(model.Substring(model.LastIndexOf(@"\")+1).Replace(".json",""));
            }
            return models.ToArray();
        }
    }
}