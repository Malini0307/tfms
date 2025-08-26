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
        public LetterOfCreditController(ILetterOfCreditService lcService)
        {
            _lcService = lcService;
        }

        public IActionResult Index()
        {
            var list = _lcService.GetAll();
            return View(list);
        }

        [Authorize(Roles = "User")]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize(Roles = "User")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(LetterOfCredit lc)
        {
            if (ModelState.IsValid)
            {
                _lcService.CreateLetterOfCredit(lc);
                return RedirectToAction(nameof(Index));
            }
            return View(lc);
        }

        [Authorize(Roles = "User")]
        public IActionResult Amend(int id)
        {
            var lc = _lcService.GetById(id);
            if (lc == null) 
                return NotFound();
            return View(lc);
        }

        [HttpPost]
        [Authorize(Roles = "User")]
        [ValidateAntiForgeryToken]
        public IActionResult Amend(LetterOfCredit lc)
        {
            if (ModelState.IsValid)
            {
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

        public IActionResult Details(int id)
        {
            var lc = _lcService.GetById(id);
            if (lc == null) return NotFound();
            return View(lc);
        }

    }
}
