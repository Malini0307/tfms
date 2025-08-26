using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TradeSystem.Models;

namespace TradeSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
        public IActionResult Contact() => View();
    }
}
