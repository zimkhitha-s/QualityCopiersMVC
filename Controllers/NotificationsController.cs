using INSY7315_ElevateDigitalStudios_POE.Helper;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    [SessionAuthorize]
    public class NotificationsController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public NotificationsController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }
        public async Task<IActionResult> Notifications()
        {
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            var recentNotifications = await _firebaseService.GetRecentNotificationsAsync();
            return View(recentNotifications);
        }

    }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//