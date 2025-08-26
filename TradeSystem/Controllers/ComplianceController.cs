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

        public ComplianceController(IComplianceService service, ILetterOfCreditService lcService, IWebHostEnvironment env)
        {
            _service = service;
            _lcService = lcService;
            _env = env;
        }

        // List reports (Admin & User)
        [Authorize(Roles = "Admin,User")]
        public IActionResult Index()
        {
            return View(_service.GetAllCompliances());
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
        public IActionResult Details(int id)
        {
            var comp = _service.GetComplianceById(id);
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
