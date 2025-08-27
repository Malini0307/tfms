using TradeSystem.Models;

namespace TradeSystem.Interfaces
{
    public interface IRiskAssessmentService
    {
        IEnumerable<RiskAssessment> GetAll();
        IEnumerable<RiskAssessment> GetByUserId(string userId);
        RiskAssessment? GetById(int id);
        RiskAssessment? GetByIdAndUserId(int id, string userId);
        RiskAssessment AnalyzeByLcId(int lcId, string userId);
        RiskAssessment AnalyzeByBgId(int guaranteeId, string userId);
        RiskAssessment AnalyzeByReference(string referenceNumber, string userId);
        RiskAssessment AnalyzeCollectiveByLcId(int lcId, string userId);
        decimal GetRiskScore(int riskId);
    }
}
