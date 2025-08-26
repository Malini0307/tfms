using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TradeSystem.Data;
using TradeSystem.Interfaces;
using TradeSystem.Models;

namespace TradeSystem.Controllers
{
    public class RiskAssessmentController : Controller
    {
        private readonly IRiskAssessmentService _riskService;
        private readonly ILetterOfCreditService _lcService;

        public RiskAssessmentController(
            IRiskAssessmentService riskService,
            ILetterOfCreditService lcService)
        {
            _riskService = riskService;
            _lcService = lcService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            LoadLcDropdown();
            return View(model: null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(int lcId)
        {
            if (lcId <= 0)
            {
                LoadLcDropdown();
                ModelState.AddModelError("", "Please select a Letter of Credit.");
                return View(model: null);
            }

            var assessment = _riskService.AnalyzeCollectiveByLcId(lcId);
            ViewBag.RiskStatus = GetStatus(assessment.RiskScore);
            LoadLcDropdown(selectedLcId: lcId);
            return View(assessment);
        }

        private void LoadLcDropdown(int? selectedLcId = null)
        {
            var lcItems = _lcService.GetAll()
                                    .Where(l => l.Status == LCStatus.Open || l.Status == LCStatus.Amended)
                                    .OrderByDescending(l => l.LcId)
                                    .Select(l => new SelectListItem
                                    {
                                        Value = l.LcId.ToString(),
                                        Text = $"LC #{l.LcId} — {l.ApplicantName} → {l.BeneficiaryName} ({l.Currency} {l.Amount})",
                                        Selected = selectedLcId.HasValue && selectedLcId.Value == l.LcId
                                    })
                                    .ToList();
            ViewBag.LCs = new SelectList(lcItems, "Value", "Text");
        }

        private (string Label, string Css) GetStatus(decimal score)
        {
            if (score >= 80) return ("High", "bg-danger");
            if (score >= 40) return ("Medium", "bg-warning text-dark");
            return ("Low", "bg-success");
        }
    }
}
