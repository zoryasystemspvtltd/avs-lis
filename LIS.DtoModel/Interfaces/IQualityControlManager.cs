using LIS.DtoModel.Models;
using System.Collections.Generic;

namespace LIS.DtoModel.Interfaces
{
    public interface IQualityControlManager
    {

        dynamic Get(string ResultId);
        ItemList<dynamic> Get(ListOptions options);

        List<ControlResultDetails> GetQualityMonthwiseData(string paramCode);
    }
}
