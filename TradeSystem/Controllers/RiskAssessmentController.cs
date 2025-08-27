using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TradeSystem.Data;
using TradeSystem.Interfaces;
using TradeSystem.Models;

namespace TradeSystem.Controllers
{
    [Authorize]
    public class RiskAssessmentController : Controller
    {
        private readonly IRiskAssessmentService _riskService;
        private readonly ILetterOfCreditService _lcService;
        private readonly UserManager<ApplicationUser> _userManager;

        public RiskAssessmentController(
            IRiskAssessmentService riskService,
            ILetterOfCreditService lcService,
            UserManager<ApplicationUser> userManager)
        {
            _riskService = riskService;
            _lcService = lcService;
            _userManager = userManager;
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

            var currentUser = await _userManager.GetUserAsync(User);
            var assessment = _riskService.AnalyzeCollectiveByLcId(lcId, currentUser.Id);
            ViewBag.RiskStatus = GetStatus(assessment.RiskScore);
            await LoadLcDropdown(selectedLcId: lcId);
            return View(assessment);
        }

        public async Task<IActionResult> List()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            IEnumerable<RiskAssessment> assessments;
            if (isAdmin)
            {
                assessments = _riskService.GetAll();
            }
            else
            {
                assessments = _riskService.GetByUserId(currentUser.Id);
            }
            
            return View(assessments);
        }

        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            RiskAssessment? assessment;
            if (isAdmin)
            {
                assessment = _riskService.GetById(id);
            }
            else
            {
                assessment = _riskService.GetByIdAndUserId(id, currentUser.Id);
            }
            
            if (assessment == null) return NotFound();
            return View(assessment);
        }

        private async Task LoadLcDropdown(int? selectedLcId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            IEnumerable<LetterOfCredit> lcs;
            if (isAdmin)
            {
                lcs = _lcService.GetAll();
            }
            else
            {
                lcs = _lcService.GetByUserId(currentUser.Id);
            }
            
            var lcItems = lcs.Where(l => l.Status == LCStatus.Open || l.Status == LCStatus.Amended)
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
