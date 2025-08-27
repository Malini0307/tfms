using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;

        public BankGuaranteeController(IBankGuaranteeService bgService, TFMSDbContext context, UserManager<ApplicationUser> userManager)
        {
            _bgService = bgService;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            
            if (isAdmin)
            {
                return View(_bgService.GetAll());
            }
            else
            {
                return View(_bgService.GetByUserId(user.Id));
            }
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            
            // Users can only see their own LCs, admins can see all
            var lcs = _context.LetterOfCredits
                              .Where(l => l.Status != LCStatus.Closed && 
                                        (isAdmin || l.CreatedByUserId == user.Id))
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
        public async Task<IActionResult> Create(int lcId, System.DateTime validityPeriod, decimal? customAmount)
        {
            if (lcId <= 0)
            {
                ModelState.AddModelError("", "Please select a Letter of Credit.");
            }

            if (!ModelState.IsValid)
            {
                return await Create(); // reload LCs & view
            }

            var user = await _userManager.GetUserAsync(User);
            var ok = _bgService.RequestGuaranteeFromLC(lcId, validityPeriod, customAmount, user.Id);
            if (!ok)
            {
                ModelState.AddModelError("", "Unable to create Bank Guarantee from the selected LC.");
                return await Create();
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

        public async Task<IActionResult> Track(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            
            BankGuarantee? bg;
            if (isAdmin)
            {
                bg = _bgService.GetById(id);
            }
            else
            {
                bg = _bgService.GetByIdAndUserId(id, user.Id);
            }
            
            if (bg == null) return NotFound();
            return View(bg);
        }

    }
}
