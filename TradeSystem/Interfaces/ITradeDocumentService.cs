using TradeSystem.Models;

namespace TradeSystem.Interfaces
{
    public interface ITradeDocumentService
    {
        bool UploadDocument(TradeDocument doc, string userId);
        TradeDocument? ViewDocument(int id);
        TradeDocument? ViewDocumentByUserId(int id, string userId);
        bool UpdateDocumentDetails(TradeDocument updatedDoc, string userId);
        IEnumerable<TradeDocument> GetAllDocumentsById();
        IEnumerable<TradeDocument> GetDocumentsByUserId(string userId);
    }
}
