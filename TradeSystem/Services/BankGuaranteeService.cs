using Microsoft.EntityFrameworkCore;

using TradeSystem.Interfaces;

using TradeSystem.Data;
using TradeSystem.Models;

namespace TradeSystem.Services
{
    public class BankGuaranteeService : IBankGuaranteeService
    {
        private readonly TFMSDbContext _context;

        public BankGuaranteeService(TFMSDbContext context)
        {
            this._context = context;
        }

        public IEnumerable<BankGuarantee> GetAll()
        {
            var list = _context.BankGuarantees.Include(bg => bg.LetterOfCredit).ToList();
            foreach (var bg in list)
            {
                CheckAndUpdateExpiry(bg);
            }
            return list;
        }

        public BankGuarantee? GetById(int id)
        {
            var bg = _context.BankGuarantees
                     .Include(b => b.LetterOfCredit)
                     .FirstOrDefault(b => b.GuaranteeId == id);
            if (bg != null)
            {
                CheckAndUpdateExpiry(bg);
            }
            return bg;
        }

        public bool RequestGuarantee(BankGuarantee bg)
        {
            try
            {
                bg.Status = BgStatus.Pending;
                _context.BankGuarantees.Add(bg);
                _context.SaveChanges();
                return true;
            }
            catch { return false; }

        }

        public bool RequestGuaranteeFromLC(int lcId, DateTime validityPeriod, decimal? customAmount = null)
        {
            var lc = _context.LetterOfCredits.Find(lcId);
            if (lc == null) return false;

            var bg = new BankGuarantee
            {
                LcId = lc.LcId,
                ApplicantName = lc.ApplicantName,
                BeneficiaryName = lc.BeneficiaryName,
                GuaranteeAmount = customAmount ?? lc.Amount,
                Currency = lc.Currency,
                ValidityPeriod = validityPeriod,
                Status = BgStatus.Pending
            };

            try
            {
                _context.BankGuarantees.Add(bg);
                _context.SaveChanges();
                return true;
            }
            catch { return false; }

        }

        public bool IssueGuarantee(BankGuarantee bg)
        {
            try
            {
                var existing = _context.BankGuarantees.Find(bg.GuaranteeId);
                if (existing == null || existing.Status == BgStatus.Expired) return false;

                // Only update fields that can change during Issue
                existing.GuaranteeAmount = bg.GuaranteeAmount;
                existing.Currency = bg.Currency;
                existing.ValidityPeriod = bg.ValidityPeriod;
                existing.Status = BgStatus.Issued;

                _context.SaveChanges();
                return true;
            }
            catch { return false; }
        }

        public BgStatus TrackGuaranteeStatus(int id)
        {
            var bg = _context.BankGuarantees.Find(id);
            if (bg == null) throw new System.Collections.Generic.KeyNotFoundException("Guarantee not found");
            return bg.Status;

        }

        private void CheckAndUpdateExpiry(BankGuarantee bg)
        {
            if (bg.Status != BgStatus.Expired && bg.ValidityPeriod.Date < DateTime.UtcNow.Date)
            {
                bg.Status = BgStatus.Expired;
                _context.SaveChanges();
            }
        }
    }
}
