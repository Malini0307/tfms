using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TradeSystem.Models;

namespace TradeSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public AdminDashboardController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.UserName = (user != null && !string.IsNullOrWhiteSpace(user.FullName)) ? user.FullName : (User.Identity?.Name ?? "Admin");
            return View();
        }
    }

}
