using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TradeSystem.Models;

namespace TradeSystem.Controllers
{
    [Authorize(Roles = "User")]
    public class UserDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public UserDashboardController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.UserName = (user != null && !string.IsNullOrWhiteSpace(user.FullName)) ? user.FullName : (User.Identity?.Name ?? "User");
            return View();
        }
    }


}
