using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Quotations()
        {
            return View();
        }

        public IActionResult CreateQuotation()
        {
            return View();
        }

        public IActionResult Invoices()
        {
            return View();
        }

        public IActionResult CreateInvoice()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Employees()
        {
            return View();
        }

        public IActionResult Clients()
        {
            return View();
        }
    }
}
