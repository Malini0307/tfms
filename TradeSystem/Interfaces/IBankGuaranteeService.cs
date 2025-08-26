
using TradeSystem.Models;

namespace TradeSystem.Interfaces
{
    public interface IBankGuaranteeService
    {
        IEnumerable<BankGuarantee> GetAll();
        BankGuarantee? GetById(int id);
        // Manual request (kept for flexibility)
        bool RequestGuarantee(BankGuarantee bg);
        // Preferred: from LC (auto-fill)
        bool RequestGuaranteeFromLC(int lcId, System.DateTime validityPeriod, decimal? customAmount = null);
        bool IssueGuarantee(BankGuarantee bg);
        BgStatus TrackGuaranteeStatus(int id);
    }
}
