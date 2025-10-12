using System.Diagnostics;
using INSY7315_ElevateDigitalStudios_POE.Models;
using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Services()
        {
            ViewData["Title"] = "Services";
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // For now: just a dummy check
            if (email == "admin@test.com" && password == "12345")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Error = "Invalid login attempt.";
            return View();
        }

        /*public IActionResult Profile()
        {
            return View();
        }*/
    }
}
