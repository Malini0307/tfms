using TradeSystem.Models;

namespace TradeSystem.Interfaces
{
    public interface IComplianceService
    {
        bool GenerateComplianceReport(Compliance compliance, string webRootPath);
        bool SubmitComplianceReport(Compliance compliance);
        Compliance GetComplianceById(int complianceId);
        List<Compliance> GetAllCompliances();
        List<int> GetAvailableLcIds(); // Only LC without compliance

    }
}
