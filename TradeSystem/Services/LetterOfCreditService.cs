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

        public LetterOfCredit? GetById(int id)
        {
            return _context.LetterOfCredits.Find(id);
        }

        public bool CreateLetterOfCredit(LetterOfCredit lc)
        {
            try
            {
                lc.Status = LCStatus.Open;
                _context.LetterOfCredits.Add(lc);
                _context.SaveChanges();
                return true;
            }
            catch { return false; }
        }

        public bool AmendLetterOfCredit(LetterOfCredit lc)
        {
            try
            {
                var existing = _context.LetterOfCredits.Find(lc.LcId);
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
