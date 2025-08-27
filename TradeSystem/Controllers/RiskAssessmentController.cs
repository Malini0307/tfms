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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TFMSDbContext _db;

        public RiskAssessmentController(
            IRiskAssessmentService riskService,
            ILetterOfCreditService lcService,
            UserManager<ApplicationUser> userManager,
            TFMSDbContext db)
        {
            _riskService = riskService;
            _lcService = lcService;
            _userManager = userManager;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await LoadLcDropdown();
            return View(model: null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(int lcId)
        {
            if (lcId <= 0)
            {
                await LoadLcDropdown();
                ModelState.AddModelError("", "Please select a Letter of Credit.");
                return View(model: null);
            }

            if (!User.IsInRole("Admin"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var lcOwn = _db.LetterOfCredits.Any(l => l.LcId == lcId && l.UserId == currentUser!.Id);
                if (!lcOwn) return Forbid();
            }

            var assessment = _riskService.AnalyzeCollectiveByLcId(lcId);
            ViewBag.RiskStatus = GetStatus(assessment.RiskScore);
            await LoadLcDropdown(selectedLcId: lcId);
            return View(assessment);
        }

        private async Task LoadLcDropdown(int? selectedLcId = null)
        {
            var query = _lcService.GetAll()
                                    .Where(l => l.Status == LCStatus.Open || l.Status == LCStatus.Amended);

            if (!User.IsInRole("Admin"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                query = query.Where(l => l.UserId == currentUser!.Id);
            }

            var lcItems = query
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
