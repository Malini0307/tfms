using TradeSystem.Data;
using TradeSystem.Interfaces;
using TradeSystem.Models;

namespace TradeSystem.Services
{
    public class LetterOfCreditService : ILetterOfCreditService
    {
        private readonly TFMSDbContext _context;
        public LetterOfCreditService(TFMSDbContext context)
        {
            _context = context;
        }

        public IEnumerable<LetterOfCredit> GetAll()
        {
            return _context.LetterOfCredits.ToList();
        }

        public IEnumerable<LetterOfCredit> GetByUserId(string userId)
        {
            return _context.LetterOfCredits
                .Where(lc => lc.CreatedByUserId == userId)
                .ToList();
        }

        public LetterOfCredit? GetById(int id)
        {
            return _context.LetterOfCredits.Find(id);
        }

        public LetterOfCredit? GetByIdAndUserId(int id, string userId)
        {
            return _context.LetterOfCredits
                .FirstOrDefault(lc => lc.LcId == id && lc.CreatedByUserId == userId);
        }

        public bool CreateLetterOfCredit(LetterOfCredit lc, string userId)
        {
            try
            {
                lc.Status = LCStatus.Open;
                lc.CreatedByUserId = userId;
                lc.CreatedDate = DateTime.UtcNow;
                _context.LetterOfCredits.Add(lc);
                _context.SaveChanges();
                return true;
            }
            catch { return false; }
        }

        public bool AmendLetterOfCredit(LetterOfCredit lc, string userId)
        {
            try
            {
                var existing = _context.LetterOfCredits
                    .FirstOrDefault(l => l.LcId == lc.LcId && l.CreatedByUserId == userId);
                if (existing == null || existing.Status == LCStatus.Closed) return false;
                existing.ApplicantName = lc.ApplicantName;
                existing.BeneficiaryName = lc.BeneficiaryName;
                existing.Amount = lc.Amount;
                existing.Currency = lc.Currency;
                existing.ExpiryDate = lc.ExpiryDate;
                existing.Status = LCStatus.Amended;
                _context.SaveChanges();
                return true;
            }
            catch { return false; }
        }

        public bool CloseLetterOfCredit(int id)
        {
            var lc = _context.LetterOfCredits.Find(id);
            if (lc == null) return false;
            try
            {
                lc.Status = LCStatus.Closed;
                _context.SaveChanges();
                return true;
            }
            catch { return false; }
        }
    }

}
