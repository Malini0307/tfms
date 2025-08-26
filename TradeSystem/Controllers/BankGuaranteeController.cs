using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TradeSystem.Data;
using TradeSystem.Interfaces;
using TradeSystem.Models;

namespace TradeSystem.Controllers
{
    [Authorize]
    public class BankGuaranteeController : Controller
    {
        private readonly TFMSDbContext _context;
        private readonly IBankGuaranteeService _bgService;


        public BankGuaranteeController(IBankGuaranteeService bgService, TFMSDbContext context)
        {
            _bgService = bgService;
            _context = context;
        }

        public IActionResult Index() => View(_bgService.GetAll());

        [Authorize(Roles = "User")]
        public IActionResult Create()
        {
            var lcs = _context.LetterOfCredits
                              .Where(l => l.Status != LCStatus.Closed)
                              .OrderByDescending(l => l.LcId)
                              .Select(l => new SelectListItem
                              {
                                  Value = l.LcId.ToString(),
                                  Text = $"LC #{l.LcId} — {l.ApplicantName} → {l.BeneficiaryName} ({l.Currency} {l.Amount})"
                              })
                              .ToList();
            ViewBag.LCs = lcs;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "User")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int lcId, System.DateTime validityPeriod, decimal? customAmount)
        {
            if (lcId <= 0)
            {
                ModelState.AddModelError("", "Please select a Letter of Credit.");
            }

            if (!ModelState.IsValid)
            {
                return Create(); // reload LCs & view
            }

            var ok = _bgService.RequestGuaranteeFromLC(lcId, validityPeriod, customAmount);
            if (!ok)
            {
                ModelState.AddModelError("", "Unable to create Bank Guarantee from the selected LC.");
                return Create();
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Issue(int id)
        {
            var bg = _bgService.GetById(id);
            if (bg == null) return NotFound();
            return View(bg);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult Issue(BankGuarantee bg)
        {
            if (!ModelState.IsValid)
            {
                // Reload original BG to repopulate view
                var existingBg = _bgService.GetById(bg.GuaranteeId);
                return View(existingBg ?? bg);
            }

            // Service now handles fetching from DB internally
            _bgService.IssueGuarantee(bg);

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Track(int id)
        {
            var bg = _bgService.GetById(id);
            if (bg == null) return NotFound();
            return View(bg);
        }

    }
}
