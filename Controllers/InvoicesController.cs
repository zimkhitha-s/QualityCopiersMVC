using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class InvoicesController : Controller
    {
        public IActionResult Invoices()
        {
            return View();
        }

        public IActionResult CreateInvoice()
        {
            return View();
        }
    }
}
