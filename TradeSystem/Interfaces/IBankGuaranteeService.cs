
using TradeSystem.Models;

namespace TradeSystem.Interfaces
{
    public interface IBankGuaranteeService
    {
        IEnumerable<BankGuarantee> GetAll();
        IEnumerable<BankGuarantee> GetByUserId(string userId);
        BankGuarantee? GetById(int id);
        BankGuarantee? GetByIdAndUserId(int id, string userId);
        // Manual request (kept for flexibility)
        bool RequestGuarantee(BankGuarantee bg);
        // Preferred: from LC (auto-fill)
        bool RequestGuaranteeFromLC(int lcId, System.DateTime validityPeriod, decimal? customAmount = null, string userId = null);
        bool IssueGuarantee(BankGuarantee bg);
        BgStatus TrackGuaranteeStatus(int id);
    }
}
