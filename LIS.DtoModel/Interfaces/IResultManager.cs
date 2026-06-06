using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface IResultManager
    {
        long Add(Result result);
        void Update(Result result);        
        Result Get(string ResultId);
        void Delete(Result result);
        Result Get(long TestRequestId, string SampleNo);
    }
}
