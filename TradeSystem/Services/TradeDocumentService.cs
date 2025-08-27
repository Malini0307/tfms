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

        public bool UploadDocument(TradeDocument doc, string userId)
        {
            // Ensure unique ReferenceNumber (generate if missing or duplicate)
            if (string.IsNullOrWhiteSpace(doc.ReferenceNumber) || _context.TradeDocuments.Any(d => d.ReferenceNumber == doc.ReferenceNumber))
            {
                doc.ReferenceNumber = GenerateUniqueReference();
            }

            // Always set server timestamps and defaults
            doc.UploadDate = DateTime.Now;
            doc.CreatedByUserId = userId;
            doc.CreatedDate = DateTime.UtcNow;
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

        public TradeDocument? ViewDocumentByUserId(int id, string userId)
        {
            return _context.TradeDocuments
                .Include(t => t.LetterOfCredit)
                .Include(t => t.BankGuarantee)
                .FirstOrDefault(t => t.DocumentId == id && t.CreatedByUserId == userId);
        }

        public IEnumerable<TradeDocument> GetAllDocumentsById()
        {
            return _context.TradeDocuments
                .Include(t => t.LetterOfCredit)
                .Include(t => t.BankGuarantee)
                .OrderByDescending(t => t.UploadDate)
                .ToList();
        }

        public IEnumerable<TradeDocument> GetDocumentsByUserId(string userId)
        {
            return _context.TradeDocuments
                .Include(t => t.LetterOfCredit)
                .Include(t => t.BankGuarantee)
                .Where(t => t.CreatedByUserId == userId)
                .OrderByDescending(t => t.UploadDate)
                .ToList();
        }

        public bool UpdateDocumentDetails(TradeDocument updatedDoc, string userId)
        {
            var existing = _context.TradeDocuments
                .FirstOrDefault(t => t.DocumentId == updatedDoc.DocumentId && t.CreatedByUserId == userId);
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












