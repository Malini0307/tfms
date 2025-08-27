using Microsoft.EntityFrameworkCore;
using TradeSystem.Data;
using TradeSystem.Interfaces;
using TradeSystem.Models;


namespace TradeSystem.Services
{
    public class TradeDocumentService : ITradeDocumentService
    {
        private readonly TFMSDbContext _context;
        private readonly ILogger<TradeDocumentService> _logger;

        public TradeDocumentService(TFMSDbContext context, ILogger<TradeDocumentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string GenerateUniqueReference()
        {
            // GUID-based unique reference with short length, prefixed
            return "ABC" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
        }

        public bool UploadDocument(TradeDocument doc)
        {
            // Ensure unique ReferenceNumber (generate if missing or duplicate)
            if (string.IsNullOrWhiteSpace(doc.ReferenceNumber) || _context.TradeDocuments.Any(d => d.ReferenceNumber == doc.ReferenceNumber))
            {
                doc.ReferenceNumber = GenerateUniqueReference();
            }

            // Always set server timestamps and defaults
            doc.UploadDate = DateTime.Now;
            if (doc.Status == 0) doc.Status = TdStatus.Active;

            _context.TradeDocuments.Add(doc);
            const int maxRetries = 3;
            DbUpdateException? lastDbUpdateException = null;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    _context.SaveChanges();
                    return true;
                }
                catch (DbUpdateException ex)
                {
                    lastDbUpdateException = ex;
                    _logger.LogWarning(ex, "Save failed, retrying with new ReferenceNumber (attempt {attempt})", attempt + 1);
                    doc.ReferenceNumber = GenerateUniqueReference();
                }
            }
            // Surface the last database exception so the UI can show the real cause
            if (lastDbUpdateException != null)
            {
                throw lastDbUpdateException;
            }
            throw new InvalidOperationException("Could not save Trade Document after multiple attempts. Please try again.");
        }

        public TradeDocument? ViewDocument(int id)
        {
            return _context.TradeDocuments
                .Include(t => t.LetterOfCredit)
                .Include(t => t.BankGuarantee)
                .FirstOrDefault(t => t.DocumentId == id);
        }

        public IEnumerable<TradeDocument> GetAllDocuments(string? userId, bool isAdmin)
        {
            var query = _context.TradeDocuments
                .Include(t => t.LetterOfCredit)
                .Include(t => t.BankGuarantee)
                .AsQueryable();

            if (!isAdmin && !string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(t => t.UserId == userId
                    || (t.LcId != null && _context.LetterOfCredits.Any(l => l.LcId == t.LcId && l.UserId == userId))
                    || (t.GuaranteeId != null && _context.BankGuarantees.Any(b => b.GuaranteeId == t.GuaranteeId && b.UserId == userId))
                );
            }

            return query.OrderByDescending(t => t.UploadDate).ToList();
        }

        public bool UpdateDocumentDetails(TradeDocument updatedDoc)
        {
            var existing = _context.TradeDocuments.Find(updatedDoc.DocumentId);
            if (existing == null) return false;

            existing.DocumentType = updatedDoc.DocumentType;
            existing.Status = updatedDoc.Status;
            existing.LcId = updatedDoc.LcId;
            existing.GuaranteeId = updatedDoc.GuaranteeId;

            try
            {
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {docId}", updatedDoc.DocumentId);
                return false;
            }
        }

    }
}












