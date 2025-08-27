using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TradeSystem.Interfaces;
using TradeSystem.Models;

namespace TradeSystem.Controllers
{
    [Authorize]
    public class ComplianceController : Controller
    {
        private readonly IComplianceService _service;
        private readonly ILetterOfCreditService _lcService;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public ComplianceController(
            IComplianceService service, 
            ILetterOfCreditService lcService, 
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager)
        {
            _service = service;
            _lcService = lcService;
            _env = env;
            _userManager = userManager;
        }

        // List reports (Admin & User)
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            if (isAdmin)
            {
                return View(_service.GetAllCompliances());
            }
            else
            {
                return View(_service.GetCompliancesByUserId(currentUser.Id));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Run()
        {
            var availableLcIds = _service.GetAvailableLcIds();

            if (!availableLcIds.Any())
            {
                TempData["Msg"] = "All LCs already have compliance reports.";
                return RedirectToAction("Index");
            }

            ViewBag.AvailableLcIds = availableLcIds;
            return View();
        }

        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> RunUser()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var availableLcIds = _service.GetAvailableLcIdsByUserId(currentUser.Id);

            if (!availableLcIds.Any())
            {
                TempData["Msg"] = "All your LCs already have compliance reports.";
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

            var ok = _service.GenerateComplianceReport(comp, webRoot, "admin"); // Admin user ID
            if (!ok)
            {
                ModelState.AddModelError("", "Compliance report already exists for this LC or generation failed.");
                return RebuildRunView();
            }

            TempData["Msg"] = "Report generated successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> RunUser(int lcId)
        {
            if (lcId <= 0)
            {
                ModelState.AddModelError("", "Please select a Letter of Credit.");
                return await RebuildRunUserView();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var comp = new Compliance { LcId = lcId };
            var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var ok = _service.GenerateComplianceReport(comp, webRoot, currentUser.Id);
            if (!ok)
            {
                ModelState.AddModelError("", "Compliance report already exists for this LC or generation failed.");
                return await RebuildRunUserView();
            }

            TempData["Msg"] = "Report generated successfully.";
            return RedirectToAction("Index");
        }

        private IActionResult RebuildRunView()
        {
            ViewBag.AvailableLcIds = _service.GetAvailableLcIds();
            return View("Run");
        }

        private async Task<IActionResult> RebuildRunUserView()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.AvailableLcIds = _service.GetAvailableLcIdsByUserId(currentUser.Id);
            return View("RunUser");
        }

        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            Compliance? comp;
            if (isAdmin)
            {
                comp = _service.GetComplianceById(id);
            }
            else
            {
                comp = _service.GetComplianceByIdAndUserId(id, currentUser.Id);
            }
            
            if (comp == null) return NotFound();
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
