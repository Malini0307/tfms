using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TradeSystem.Interfaces;
using TradeSystem.Models;

namespace TradeSystem.Controllers
{
    [Authorize]
    public class LetterOfCreditController : Controller
    {
        private readonly ILetterOfCreditService _lcService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LetterOfCreditController(ILetterOfCreditService lcService, UserManager<ApplicationUser> userManager)
        {
            _lcService = lcService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            
            if (isAdmin)
            {
                var list = _lcService.GetAll();
                return View(list);
            }
            else
            {
                var list = _lcService.GetByUserId(user.Id);
                return View(list);
            }
        }

        [Authorize(Roles = "User")]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize(Roles = "User")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LetterOfCredit lc)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var success = _lcService.CreateLetterOfCredit(lc, user.Id);
                if (success)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Failed to create Letter of Credit. Please try again.");
            }
            return View(lc);
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Amend(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var lc = _lcService.GetByIdAndUserId(id, user.Id);
            if (lc == null) 
                return NotFound();
            return View(lc);
        }

        [HttpPost]
        [Authorize(Roles = "User")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Amend(LetterOfCredit lc)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var success = _lcService.AmendLetterOfCredit(lc, user.Id);
                if (success)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Failed to amend Letter of Credit. Please try again.");
            }
            return View(lc);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Close(int id)
        {
            var lc = _lcService.GetById(id);
            if (lc == null) 
                return NotFound();
            return View(lc);
        }

        [HttpPost, ActionName("Close")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult CloseConfirmed(int id)
        {
            _lcService.CloseLetterOfCredit(id);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            
            LetterOfCredit? lc;
            if (isAdmin)
            {
                lc = _lcService.GetById(id);
            }
            else
            {
                lc = _lcService.GetByIdAndUserId(id, user.Id);
            }
            
            if (lc == null) return NotFound();
            return View(lc);
        }

    }
}
