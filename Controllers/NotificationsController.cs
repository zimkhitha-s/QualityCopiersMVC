using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class NotificationsController : Controller
    {
        public IActionResult Notifications()
        {
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            return View();
        }

    }
}