using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Text.Json;
using TradeSystem.Data;
using TradeSystem.Interfaces;
using TradeSystem.Models;

namespace TradeSystem.Services
{
    public class ComplianceService : IComplianceService
    {
        private readonly TFMSDbContext _context;

        public ComplianceService(TFMSDbContext context)
        {
            _context = context;
        }

        public List<int> GetAvailableLcIds()
        {
            var used = _context.Compliances
                               .Where(c => c.LcId.HasValue)
                               .Select(c => c.LcId!.Value)
                               .ToList();

            return _context.LetterOfCredits
                           .Where(l => !used.Contains(l.LcId))
                           .Select(l => l.LcId)
                           .OrderByDescending(x => x)
                           .ToList();
        }

        public List<int> GetAvailableLcIdsByUserId(string userId)
        {
            var used = _context.Compliances
                               .Where(c => c.LcId.HasValue)
                               .Select(c => c.LcId!.Value)
                               .ToList();

            return _context.LetterOfCredits
                           .Where(l => !used.Contains(l.LcId) && l.CreatedByUserId == userId)
                           .Select(l => l.LcId)
                           .OrderByDescending(x => x)
                           .ToList();
        }

        public bool GenerateComplianceReport(Compliance compliance, string webRootPath, string userId)
        {
            if (compliance == null) return false;

            // -------- Resolve LC hub (LC preferred, or BG -> LC, or TD -> LC) --------
            LetterOfCredit? lc = null;

            if (compliance.LcId.HasValue)
            {
                lc = _context.LetterOfCredits.FirstOrDefault(l => l.LcId == compliance.LcId.Value);
            }
            else if (compliance.GuaranteeId.HasValue)
            {
                var bg = _context.BankGuarantees.FirstOrDefault(b => b.GuaranteeId == compliance.GuaranteeId.Value);
                if (bg != null)
                {
                    compliance.LcId = bg.LcId;
                    lc = _context.LetterOfCredits.FirstOrDefault(l => l.LcId == bg.LcId);
                }
            }
            else if (!string.IsNullOrWhiteSpace(compliance.TransactionReference))
            {
                var td = _context.TradeDocuments.FirstOrDefault(t => t.ReferenceNumber == compliance.TransactionReference);
                if (td?.LcId != null)
                {
                    compliance.LcId = td.LcId;
                    lc = _context.LetterOfCredits.FirstOrDefault(l => l.LcId == td.LcId);
                }
            }

            if (lc == null) return false;

            // -------- Enforce uniqueness: one report per LC --------
            bool alreadyExists = _context.Compliances.Any(c => c.LcId == lc.LcId);
            if (alreadyExists)
                return false;

            // -------- Related BGs & TDs (by LC) --------
            var bgs = _context.BankGuarantees.Where(b => b.LcId == lc.LcId).ToList();
            var tds = _context.TradeDocuments.Where(t => t.LcId == lc.LcId).ToList();

            // -------- Findings & risk logic (your original style) --------
            var findings = new List<object>();
            int redFlags = 0;

            if (lc.Amount >= 1_000_000m)
            {
                findings.Add(new { code = "AMOUNT_HIGH", sev = "high", msg = "LC amount >= 1,000,000" });
                redFlags++;
            }

            var daysToExpiry = (lc.ExpiryDate - DateTime.UtcNow).TotalDays;   // << fix: ExpiryDate (PascalCase)
            if (daysToExpiry <= 30)
                findings.Add(new { code = "LC_EXP_SOON", sev = "medium", msg = "LC expires in <=30 days" });

            if (lc.Status == LCStatus.Amended)
                findings.Add(new { code = "LC_AMENDED", sev = "medium", msg = "LC amended" });

            if (lc.Status == LCStatus.Closed)
            {
                findings.Add(new { code = "LC_CLOSED", sev = "high", msg = "LC closed" });
                redFlags++;
            }

            if (bgs.Count > 0)
            {
                var totalBg = bgs.Sum(x => x.GuaranteeAmount);
                if (totalBg > lc.Amount)
                {
                    findings.Add(new { code = "BG_GT_LC", sev = "high", msg = "Total BG > LC amount" });
                    redFlags++;
                }
                if (bgs.Any(x => x.Status == BgStatus.Expired))
                {
                    findings.Add(new { code = "BG_EXPIRED", sev = "high", msg = "At least one BG expired" });
                    redFlags++;
                }
                var minDaysLeft = bgs.Min(x => (x.ValidityPeriod - DateTime.UtcNow).TotalDays);
                if (minDaysLeft <= 30)
                    findings.Add(new { code = "BG_EXP_SOON", sev = "medium", msg = "BG validity ends <=30 days" });
            }

            if (tds.Count < 2)
                findings.Add(new { code = "DOCS_FEW", sev = "medium", msg = "Low number of trade documents" });

            if (tds.Any(d => d.Status == TdStatus.Archived))
                findings.Add(new { code = "DOCS_ARCH", sev = "low", msg = "Some trade docs archived" });

            // -------- Risk score (normalized) --------
            decimal score = 0;
            score += Math.Min((lc.Amount / 1_000_000m) * 10m, 30m);
            if (daysToExpiry <= 30) score += 15;
            if (lc.Status == LCStatus.Amended) score += 10;
            if (lc.Status == LCStatus.Closed) score += 25;

            if (bgs.Count > 0)
            {
                score += Math.Min((bgs.Sum(x => x.GuaranteeAmount) / 1_000_000m) * 10m, 30m);
                var minDays = bgs.Min(x => (x.ValidityPeriod - DateTime.UtcNow).TotalDays);
                if (minDays <= 30) score += 15;
                if (bgs.Any(x => x.Status == BgStatus.Expired)) score += 30;
            }
            if (tds.Count < 2) score += 10;

            var normalized = Math.Min((score / 165m) * 100m, 100m);
            var status = (redFlags > 0 || normalized >= 60m) ? Status4.Non_Compliant : Status4.Compliant;
            var remarks = status == Status4.Compliant ? "Auto: Compliant" : "Auto: Non-Compliant";

            // -------- Fill and save --------
            compliance.TransactionReference = string.IsNullOrWhiteSpace(compliance.TransactionReference)
                ? $"LC:{lc.LcId}" : compliance.TransactionReference;
            compliance.ComplianceStatus = status;
            compliance.Remarks = remarks;
            compliance.ReportDate = DateTime.UtcNow;
            compliance.OverallRiskScore = normalized;
            compliance.FindingsJson = JsonSerializer.Serialize(findings);
            compliance.CreatedByUserId = userId;
            compliance.CreatedDate = DateTime.UtcNow;

            // If your model has IsFinalized, set it; otherwise remove this line.
            compliance.IsFinalized = false;

            _context.Compliances.Add(compliance);
            _context.SaveChanges();

            // -------- Generate PDF --------
            var pdfRelPath = GeneratePdfAndReturnRelativePath(compliance, webRootPath, lc, bgs, tds, findings);
            compliance.PdfPath = pdfRelPath;

            _context.Compliances.Update(compliance);
            _context.SaveChanges();

            return true;
        }

        public bool SubmitComplianceReport(Compliance compliance)
        {
            var existing = _context.Compliances
                .Include(x => x.LetterOfCredit)
                .FirstOrDefault(c => c.ComplianceId == compliance.ComplianceId);

            if (existing == null) return false;

            // Update fields that Admin can change
            existing.ComplianceStatus = compliance.ComplianceStatus;
            existing.Remarks = string.IsNullOrWhiteSpace(compliance.Remarks) ? existing.Remarks : compliance.Remarks;
            existing.ReportDate = DateTime.UtcNow;

            // If your model has IsFinalized, set it; otherwise remove this line.
            existing.IsFinalized = true;

            // Rebuild PDF to reflect edits
            var webroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var lc = existing.LetterOfCredit;
            var bgs = _context.BankGuarantees.Where(b => b.LcId == existing.LcId).ToList();
            var tds = _context.TradeDocuments.Where(t => t.LcId == existing.LcId).ToList();

            var findings = TryDeserializeFindings(existing.FindingsJson);
            existing.PdfPath = GeneratePdfAndReturnRelativePath(existing, webroot, lc, bgs, tds, findings);

            return _context.SaveChanges() > 0;
        }

        public Compliance? GetComplianceById(int id)
            => _context.Compliances
                       .Include(c => c.LetterOfCredit)
                       .FirstOrDefault(c => c.ComplianceId == id);

        public Compliance? GetComplianceByIdAndUserId(int id, string userId)
            => _context.Compliances
                       .Include(c => c.LetterOfCredit)
                       .FirstOrDefault(c => c.ComplianceId == id && c.CreatedByUserId == userId);

        public List<Compliance> GetAllCompliances()
            => _context.Compliances
                       .Include(c => c.LetterOfCredit)
                       .OrderByDescending(c => c.ReportDate)
                       .ToList();

        public List<Compliance> GetCompliancesByUserId(string userId)
            => _context.Compliances
                       .Include(c => c.LetterOfCredit)
                       .Where(c => c.CreatedByUserId == userId)
                       .OrderByDescending(c => c.ReportDate)
                       .ToList();

        // ================= helpers =================

        private static List<object> TryDeserializeFindings(string? json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return new List<object>();
                var doc = JsonSerializer.Deserialize<List<object>>(json);
                return doc ?? new List<object>();
            }
            catch { return new List<object>(); }
        }

        private static string GeneratePdfAndReturnRelativePath(
            Compliance comp,
            string webRootPath,
            LetterOfCredit? lc,
            List<BankGuarantee> bgs,
            List<TradeDocument> tds,
            List<object> findings)
        {
            // Ensure folder
            var folder = Path.Combine(webRootPath, "compliance_reports");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = $"ComplianceReport_{comp.ComplianceId}.pdf";
            var filePath = Path.Combine(folder, fileName);
            var relPath = "/compliance_reports/" + fileName;

            // Build PDF (QuestPDF)
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text("Compliance Report")
                                 .FontSize(22).Bold().AlignCenter();

                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Report ID: {comp.ComplianceId}");
                        col.Item().Text($"Transaction Reference: {comp.TransactionReference}");
                        col.Item().Text($"Status: {comp.ComplianceStatus}");
                        col.Item().Text($"Remarks: {comp.Remarks}");
                        col.Item().Text($"Date: {comp.ReportDate:yyyy-MM-dd HH:mm}");
                        col.Item().Text($"Risk Score: {comp.OverallRiskScore ?? 0:0.##}%");

                        if (lc != null)
                        {
                            col.Item().Text("");
                            col.Item().Text("— Letter of Credit —").Bold();
                            col.Item().Text($"LC Id: {lc.LcId}");
                            col.Item().Text($"Applicant: {lc.ApplicantName}");
                            col.Item().Text($"Beneficiary: {lc.BeneficiaryName}");
                            col.Item().Text($"Amount: {lc.Currency} {lc.Amount:0.##}");
                            col.Item().Text($"Expiry: {lc.ExpiryDate:yyyy-MM-dd}");
                            col.Item().Text($"LC Status: {lc.Status}");
                        }

                        if (bgs.Any())
                        {
                            col.Item().Text("");
                            col.Item().Text("— Bank Guarantees —").Bold();
                            foreach (var bg in bgs)
                            {
                                col.Item().Text(
                                    $"BG#{bg.GuaranteeId} - {bg.Currency} {bg.GuaranteeAmount:0.##} - " +
                                    $"Status: {bg.Status} - Validity: {bg.ValidityPeriod:yyyy-MM-dd}"
                                );
                            }
                        }

                        if (tds.Any())
                        {
                            col.Item().Text("");
                            col.Item().Text("— Trade Documents —").Bold();
                            foreach (var td in tds)
                            {
                                col.Item().Text(
                                    $"Doc#{td.DocumentId} [{td.DocumentType}] Ref:{td.ReferenceNumber} - " +
                                    $"{td.Status} (Uploaded:{td.UploadDate:yyyy-MM-dd})"
                                );
                            }
                        }

                        col.Item().Text("");
                        col.Item().Text("— Findings —").Bold();
                        if (findings.Any())
                        {
                            foreach (var f in findings)
                                col.Item().Text(JsonSerializer.Serialize(f));
                        }
                        else
                        {
                            col.Item().Text("No findings.");
                        }
                    });

                    page.Footer().AlignCenter()
                        .Text($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm}");
                });
            }).GeneratePdf(filePath);

            return relPath;
        }


    }
}

