using LIS.DtoModel.Models;

namespace LIS.DtoModel.Interfaces
{
    public interface ISampleCollectionManager
    {
        ItemList<SampleWorkflowQueueRow> GetPendingQueue(SampleWorkflowSearchOptions options);
        SampleWorkflowQueueRow GetByBarcode(string barcode);
        void CollectSample(SampleCollectionAction action);
        void RejectCollection(SampleRejectionAction action);
        void TriggerRecollection(long testRequestId);
        string EnsureBarcode(long testRequestId);
    }
}
