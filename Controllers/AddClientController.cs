using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class AddClientController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(string clientName, string email, string phone, string address)
        {
            // TODO: Add client creation logic here
            // For now, just redirect back to dashboard
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
