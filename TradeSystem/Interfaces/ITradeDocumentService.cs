using TradeSystem.Models;

namespace TradeSystem.Interfaces
{
    public interface ITradeDocumentService
    {
        bool UploadDocument(TradeDocument doc);
        TradeDocument ViewDocument(int id);
        bool UpdateDocumentDetails(TradeDocument updatedDoc);
        IEnumerable<TradeDocument> GetAllDocumentsById();
    }
}
