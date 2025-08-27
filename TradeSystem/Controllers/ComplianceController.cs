using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TradeSystem.Interfaces;
using TradeSystem.Models;

namespace TradeSystem.Controllers
{
    public class ComplianceController : Controller
    {
        private readonly IComplianceService _service;
        private readonly ILetterOfCreditService _lcService;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TFMSDbContext _db;

        public ComplianceController(IComplianceService service, ILetterOfCreditService lcService, IWebHostEnvironment env, UserManager<ApplicationUser> userManager, TFMSDbContext db)
        {
            _service = service;
            _lcService = lcService;
            _env = env;
            _userManager = userManager;
            _db = db;
        }

        // List reports (Admin & User)
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Admin"))
                return View(_service.GetAllCompliances());

            var currentUser = await _userManager.GetUserAsync(User);
            var list = _service.GetAllCompliances()
                .Where(c => (c.LcId != null && _db.LetterOfCredits.Any(l => l.LcId == c.LcId && l.UserId == currentUser!.Id))
                         || (c.GuaranteeId != null && _db.BankGuarantees.Any(b => b.GuaranteeId == c.GuaranteeId && b.UserId == currentUser!.Id))
                ).ToList();
            return View(list);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Run()
        {
            var availableLcIds = _service.GetAvailableLcIds();
            if (!User.IsInRole("Admin"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                availableLcIds = availableLcIds
                    .Where(id => _db.LetterOfCredits.Any(l => l.LcId == id && l.UserId == currentUser!.Id))
                    .ToList();
            }

            if (!availableLcIds.Any())
            {
                TempData["Msg"] = "All LCs already have compliance reports.";
                return RedirectToAction("Index");
            }

            ViewBag.AvailableLcIds = availableLcIds;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Run(int lcId)
        {
            if (lcId <= 0)
            {
                ModelState.AddModelError("", "Please select a Letter of Credit.");
                return RebuildRunView();
            }

            var comp = new Compliance { LcId = lcId };
            var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var ok = _service.GenerateComplianceReport(comp, webRoot);
            if (!ok)
            {
                ModelState.AddModelError("", "Compliance report already exists for this LC or generation failed.");
                return RebuildRunView();
            }

            TempData["Msg"] = "Report generated successfully.";
            return RedirectToAction("Index");
        }

        private IActionResult RebuildRunView()
        {
            ViewBag.AvailableLcIds = _service.GetAvailableLcIds();
            return View("Run");
        }


        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Details(int id)
        {
            var comp = _service.GetComplianceById(id);
            if (comp == null) return NotFound();
            if (!User.IsInRole("Admin"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var ownsLc = comp.LcId != null && _db.LetterOfCredits.Any(l => l.LcId == comp.LcId && l.UserId == currentUser!.Id);
                var ownsBg = comp.GuaranteeId != null && _db.BankGuarantees.Any(b => b.GuaranteeId == comp.GuaranteeId && b.UserId == currentUser!.Id);
                if (!ownsLc && !ownsBg) return Forbid();
            }
            return View(comp);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var comp = _service.GetComplianceById(id);
            if (comp == null) return NotFound();
            return View(comp);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(Compliance compliance)
        {
            if (!ModelState.IsValid)
                return View(compliance);

            var ok = _service.SubmitComplianceReport(compliance);
            if (!ok)
            {
                ModelState.AddModelError("", "Failed to update compliance report.");
                return View(compliance);
            }

            TempData["Msg"] = "Changes applied successfully. Now you can download and view the updated report.";
            return RedirectToAction(nameof(Index));
        }



    }
}
