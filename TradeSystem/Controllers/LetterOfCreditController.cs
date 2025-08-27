using Microsoft.AspNetCore.Authorization;
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
        private readonly TFMSDbContext _db;
        public LetterOfCreditController(ILetterOfCreditService lcService, UserManager<ApplicationUser> userManager, TFMSDbContext db)
        {
            _lcService = lcService;
            _userManager = userManager;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            bool isAdmin = User.IsInRole("Admin");
            var list = isAdmin
                ? _lcService.GetAll()
                : _db.LetterOfCredits.Where(l => l.UserId == currentUser!.Id).ToList();
            return View(list);
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
                var currentUser = await _userManager.GetUserAsync(User);
                lc.UserId = currentUser?.Id;
                _lcService.CreateLetterOfCredit(lc);
                return RedirectToAction(nameof(Index));
            }
            return View(lc);
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Amend(int id)
        {
            var lc = _lcService.GetById(id);
            if (lc == null) 
                return NotFound();
            if (!User.IsInRole("Admin"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (lc.UserId != currentUser?.Id) return Forbid();
            }
            return View(lc);
        }

        [HttpPost]
        [Authorize(Roles = "User")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Amend(LetterOfCredit lc)
        {
            if (ModelState.IsValid)
            {
                if (!User.IsInRole("Admin"))
                {
                    var current = _lcService.GetById(lc.LcId);
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (current?.UserId != currentUser?.Id) return Forbid();
                }
                _lcService.AmendLetterOfCredit(lc);
                return RedirectToAction(nameof(Index));
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
            var lc = _lcService.GetById(id);
            if (lc == null) return NotFound();
            if (!User.IsInRole("Admin"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (lc.UserId != currentUser?.Id) return Forbid();
            }
            return View(lc);
        }

    }
}
