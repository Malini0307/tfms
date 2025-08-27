using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TradeSystem.Data;
using TradeSystem.Interfaces;
using TradeSystem.Models;

namespace TradeSystem.Services
{
    public class RiskAssessmentService : IRiskAssessmentService
    {
        private readonly TFMSDbContext _context;
        public RiskAssessmentService(TFMSDbContext context)
        {
            this._context = context;
        }

        public IEnumerable<RiskAssessment> GetAll()
        {
            return _context.RiskAssessments
                .Include(r => r.LetterOfCredit)
                .Include(r => r.BankGuarantee)
                .OrderByDescending(r => r.AssessmentDate)
                .ToList();
        }

        public IEnumerable<RiskAssessment> GetByUserId(string userId)
        {
            return _context.RiskAssessments
                .Include(r => r.LetterOfCredit)
                .Include(r => r.BankGuarantee)
                .Where(r => r.CreatedByUserId == userId)
                .OrderByDescending(r => r.AssessmentDate)
                .ToList();
        }

        public RiskAssessment? GetById(int id)
        {
            return _context.RiskAssessments
                .Include(r => r.LetterOfCredit)
                .Include(r => r.BankGuarantee)
                .FirstOrDefault(r => r.RiskId == id);
        }

        public RiskAssessment? GetByIdAndUserId(int id, string userId)
        {
            return _context.RiskAssessments
                .Include(r => r.LetterOfCredit)
                .Include(r => r.BankGuarantee)
                .FirstOrDefault(r => r.RiskId == id && r.CreatedByUserId == userId);
        }

        public RiskAssessment AnalyzeByLcId(int lcId, string userId)
        {
            var lc = _context.LetterOfCredits.FirstOrDefault(l => l.LcId == lcId);
            if (lc == null) 
                throw new ArgumentException("Letter of Credit not found.");
            var bg = _context.BankGuarantees.FirstOrDefault(b => b.LcId == lcId);
            return ComputeRisk(lc, bg, null, userId);
        }

        public RiskAssessment AnalyzeByBgId(int guaranteeId, string userId)
        {
            var bg = _context.BankGuarantees.FirstOrDefault(b => b.GuaranteeId == guaranteeId);
            if (bg == null) 
                throw new ArgumentException("Bank Guarantee not found.");
            var lc = _context.LetterOfCredits.FirstOrDefault(l => l.LcId == bg.LcId);
            return ComputeRisk(lc, bg, null, userId);
        }

        public RiskAssessment AnalyzeByReference(string referenceNumber, string userId)
        {
            if (string.IsNullOrWhiteSpace(referenceNumber)) 
                throw new ArgumentException("Reference number is required.");
            var doc = _context.TradeDocuments.FirstOrDefault(t => t.ReferenceNumber == referenceNumber);
            if (doc == null) 
                throw new ArgumentException("Trade Document with the given reference was not found.");
            var lc = doc.LcId.HasValue ? _context.LetterOfCredits.FirstOrDefault(l => l.LcId == doc.LcId) : null;
            var bg = doc.GuaranteeId.HasValue ? _context.BankGuarantees.FirstOrDefault(b => b.GuaranteeId == doc.GuaranteeId) : null;
            return ComputeRisk(lc, bg, doc, userId);
        }

        // Collective analysis: LC + all related BGs + all related Trade Documents
        public RiskAssessment AnalyzeCollectiveByLcId(int lcId, string userId)
        {
            var lc = _context.LetterOfCredits.FirstOrDefault(l => l.LcId == lcId);
            if (lc == null) 
                throw new ArgumentException("Letter of Credit not found.");
            var bgs = _context.BankGuarantees.Where(b => b.LcId == lcId).ToList();
            var tds = _context.TradeDocuments.Where(t => t.LcId == lcId).ToList();

            var factors = new Dictionary<string, object>();
            decimal score = 0;

            // LC factors
            var lcAmtScore = Math.Min((lc.Amount / 1_000_000m) * 10m, 30m);
            score += lcAmtScore; 
            factors["LCAmountScore"] = lcAmtScore; 
            factors["LCAmount"] = lc.Amount;

            var daysToExpiry = (lc.ExpiryDate - DateTime.UtcNow).TotalDays;
            decimal lcExpiryScore = daysToExpiry <= 30 ? 25 : daysToExpiry <= 90 ? 15 : 5;
            score += lcExpiryScore; 
            factors["LCExpiryScore"] = lcExpiryScore; 
            factors["LCDaysToExpiry"] = (int)daysToExpiry;

            decimal lcStatusScore = lc.Status switch { LCStatus.Amended => 15, LCStatus.Closed => 20, _ => 5 };
            score += lcStatusScore; 
            factors["LCStatusScore"] = lcStatusScore; 
            factors["LCStatus"] = lc.Status.ToString();

            // Aggregate BG factors
            if (bgs.Count > 0)
            {
                var totalBgAmount = bgs.Sum(b => b.GuaranteeAmount);
                var bgAmtScore = Math.Min((totalBgAmount / 1_000_000m) * 10m, 30m);
                score += bgAmtScore; 
                factors["BGTotalAmountScore"] = bgAmtScore; 
                factors["BGTotalAmount"] = totalBgAmount;

                var minDaysLeft = bgs.Min(b => (b.ValidityPeriod - DateTime.UtcNow).TotalDays);
                decimal bgValidityScore = minDaysLeft <= 30 ? 25 : minDaysLeft <= 90 ? 15 : 5;
                score += bgValidityScore; 
                factors["BGMinDaysToValidityEndScore"] = bgValidityScore; 
                factors["BGMinDaysToValidityEnd"] = (int)minDaysLeft;

                // Penalize for any risky statuses among BGs
                decimal bgStatusScore = bgs.Max(b => b.Status switch { 
                    BgStatus.Pending => 20, BgStatus.Expired => 40, _ => 5 
                });
                score += bgStatusScore; 
                factors["BGStatusScore"] = bgStatusScore; 
                factors["BGStatuses"] = bgs.Select(b => b.Status.ToString()).ToArray();
            }

            // Trade Document factors (aggregate)
            if (tds.Count > 0)
            {
                var docCountScore = Math.Min(tds.Count * 2m, 20m);
                score += docCountScore; 
                factors["TradeDocCountScore"] = docCountScore; 
                factors["TradeDocCount"] = tds.Count;

                var activeDocs = tds.Count(t => t.Status == TdStatus.Active);
                var docStatusScore = activeDocs > 0 ? 10m : 25m; // Penalize if no active docs
                score += docStatusScore; 
                factors["TradeDocStatusScore"] = docStatusScore; 
                factors["ActiveTradeDocs"] = activeDocs;
            }

            // Normalize to 0-100 scale
            var task = Task.Run(() => Math.Min((score / 165m) * 100m, 100m));
            decimal normalized = 0;
            if (task.IsCompleted)
            {
                normalized = task.Result;
            }

            var assessment = new RiskAssessment
            {
                TransactionReference = $"LC:{lc.LcId}",
                RiskFactors = System.Text.Json.JsonSerializer.Serialize(factors),
                RiskScore = normalized,
                AssessmentDate = DateTime.UtcNow,
                LcId = lc.LcId,
                CreatedByUserId = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.RiskAssessments.Add(assessment);
            _context.SaveChanges();
            return assessment;
        }

        public decimal GetRiskScore(int riskId)
        {
            var assessment = _context.RiskAssessments.Find(riskId);
            if (assessment == null)
                throw new ArgumentException("Risk assessment not found.");

            return assessment.RiskScore;
        }

        // Point-in-time risk from a single LC/BG/Doc (kept for API completeness)
        private RiskAssessment ComputeRisk(LetterOfCredit? lc, BankGuarantee? bg, TradeDocument? doc, string userId)
        {
            var factors = new Dictionary<string, object>();
            decimal score = 0;

            // LC
            if (lc != null)
            {
                var lcAmtScore = Math.Min((lc.Amount / 1_000_000m) * 10m, 30m);
                score += lcAmtScore; 
                factors["LCAmountScore"] = lcAmtScore; 
                factors["LCAmount"] = lc.Amount;

                var daysToExpiry = (lc.ExpiryDate - DateTime.UtcNow).TotalDays;
                decimal lcExpiryScore = daysToExpiry <= 30 ? 25 : daysToExpiry <= 90 ? 15 : 5;
                score += lcExpiryScore; 
                factors["LCExpiryScore"] = lcExpiryScore; 
                factors["LCDaysToExpiry"] = (int)daysToExpiry;

                decimal lcStatusScore = lc.Status switch { LCStatus.Amended => 15, LCStatus.Closed => 20, _ => 5 };
                score += lcStatusScore; 
                factors["LCStatusScore"] = lcStatusScore; 
                factors["LCStatus"] = lc.Status.ToString();
            }

            // BG
            if (bg != null)
            {
                var bgAmtScore = Math.Min((bg.GuaranteeAmount / 1_000_000m) * 10m, 30m);
                score += bgAmtScore; 
                factors["BGAmountScore"] = bgAmtScore; 
                factors["BGAmount"] = bg.GuaranteeAmount;

                var daysLeft = (bg.ValidityPeriod - DateTime.UtcNow).TotalDays;
                decimal bgValidityScore = daysLeft <= 30 ? 25 : daysLeft <= 90 ? 15 : 5;
                score += bgValidityScore; 
                factors["BGValidityScore"] = bgValidityScore; 
                factors["BGDaysToValidityEnd"] = (int)daysLeft;

                decimal bgStatusScore = bg.Status switch { BgStatus.Pending => 20, BgStatus.Expired => 40, _ => 5 };
                score += bgStatusScore; 
                factors["BGStatusScore"] = bgStatusScore; 
                factors["BGStatus"] = bg.Status.ToString();
            }

            if (doc != null) { factors["TradeDocumentLinked"] = true; }

            var normalized = Math.Min((score / 165m) * 100m, 100m);

            var assessment = new RiskAssessment
            {
                TransactionReference = lc != null ? $"LC:{lc.LcId}" : (bg != null ? $"BG:{bg.GuaranteeId}" : (doc != null ? doc.ReferenceNumber : "N/A")),
                RiskFactors = System.Text.Json.JsonSerializer.Serialize(factors),
                RiskScore = normalized,
                AssessmentDate = DateTime.UtcNow,
                LcId = lc?.LcId,
                GuaranteeId = bg?.GuaranteeId,
                CreatedByUserId = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.RiskAssessments.Add(assessment);
            _context.SaveChanges();
            return assessment;
        }
    }
}




