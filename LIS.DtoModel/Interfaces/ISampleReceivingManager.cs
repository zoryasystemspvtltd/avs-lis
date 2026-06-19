using LIS.DtoModel.Models;

namespace LIS.DtoModel.Interfaces
{
    public interface ISampleReceivingManager
    {
        ItemList<SampleWorkflowQueueRow> GetReceivingQueue(SampleWorkflowSearchOptions options);
        SampleWorkflowQueueRow GetByBarcode(string barcode);
        void ReceiveSample(SampleReceivingAction action);
        void RejectSample(SampleRejectionAction action);
        void TriggerRecollection(long testRequestId);
    }
}
