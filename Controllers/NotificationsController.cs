using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class NotificationsController : Controller
    {
        public IActionResult Notifications()
        {
            return View();
        }

    }
}