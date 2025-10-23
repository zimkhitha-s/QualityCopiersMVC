using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class PaymentsController : Controller
    {
        public IActionResult Payments()
        {
            return View();
        }
    }
}