using TradeSystem.Models;

namespace TradeSystem.Interfaces
{
    public interface IComplianceService
    {
        bool GenerateComplianceReport(Compliance compliance, string webRootPath, string userId);
        bool SubmitComplianceReport(Compliance compliance);
        Compliance? GetComplianceById(int complianceId);
        Compliance? GetComplianceByIdAndUserId(int complianceId, string userId);
        List<Compliance> GetAllCompliances();
        List<Compliance> GetCompliancesByUserId(string userId);
        List<int> GetAvailableLcIds(); // Only LC without compliance
        List<int> GetAvailableLcIdsByUserId(string userId); // Only LC without compliance for specific user
    }
}
