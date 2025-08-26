using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TradeSystem.Models;

namespace TradeSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(SignInManager<ApplicationUser> signInManager,
                                 UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Login(string? role)
        {
            ViewBag.Role = role; // "Admin" or "User"

            // If already authenticated, prevent role switching without logout
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentRole = User.IsInRole("Admin") ? "Admin" : (User.IsInRole("User") ? "User" : null);
                if (string.IsNullOrEmpty(role) || string.Equals(currentRole, role, StringComparison.OrdinalIgnoreCase))
                {
                    // Already signed into the requested role (or no role specified) -> go to dashboard
                    if (currentRole == "Admin") return RedirectToAction("Index", "AdminDashboard");
                    if (currentRole == "User") return RedirectToAction("Index", "UserDashboard");
                }
                else
                {
                    // Prompt to logout first
                    ViewBag.SwitchPrompt = $"You are currently signed in as {currentRole}. To sign in as {role}, please logout first.";
                    ViewBag.ForceLogoutUrl = Url.Action("Logout", "Account", new { returnUrl = Url.Action("Login", "Account", new { role }) });
                }
            }

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string role)
        {
            // If already authenticated with another role, block and ask to logout first
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentRole = User.IsInRole("Admin") ? "Admin" : (User.IsInRole("User") ? "User" : null);
                if (!string.IsNullOrEmpty(currentRole) && !string.Equals(currentRole, role, StringComparison.OrdinalIgnoreCase))
                {
                    ViewBag.Error = $"You are currently signed in as {currentRole}. Please logout first to sign in as {role}.";
                   
                }
            }
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentRole = User.IsInRole("Admin") ? "Admin" : (User.IsInRole("User") ? "User" : null);
                if (!string.IsNullOrEmpty(currentRole) && !string.Equals(currentRole, role, StringComparison.OrdinalIgnoreCase))
                {
                    ViewBag.Error = $"You are currently signed in as {currentRole}. Please logout first to sign in as {role}.";
                    // ... show logout prompt
                    ViewBag.Role = role;
                    ViewBag.ForceLogoutUrl = Url.Action("Logout", "Account", new { returnUrl = Url.Action("Login", "Account", new { role }) });
                    return View();
                }
            }

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
            {
                ViewBag.Error = "All fields are required.";
                ViewBag.Role = role;
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null || !await _userManager.IsInRoleAsync(user, role))
            {
                ViewBag.Error = "Invalid credentials or role.";
                ViewBag.Role = role;
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                ViewBag.Error = "Invalid credentials.";
                ViewBag.Role = role;
                return View();
            }

            return role == "Admin"
                ? RedirectToAction("Index", "AdminDashboard")
                : RedirectToAction("Index", "UserDashboard");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout(string? returnUrl)
        {
            await _signInManager.SignOutAsync();
            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register(string? role)
        {
            ViewBag.Role = role;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string email, string password, string confirmPassword, string role)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                ViewBag.Role = role;
                return View();
            }
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
            {
                ViewBag.Error = "All fields are required.";
                ViewBag.Role = role;
                return View();
            }

            var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName };
            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                ViewBag.Error = string.Join("<br/>", result.Errors.Select(e => e.Description));
                ViewBag.Role = role;
                return View();
            }

            await _userManager.AddToRoleAsync(user, role);
            TempData["Msg"] = "Registration successful. Please login.";
            return RedirectToAction("Login", new { role });
        }

        public IActionResult AccessDenied() => View();


    }
}
