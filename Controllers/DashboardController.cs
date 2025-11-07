using INSY7315_ElevateDigitalStudios_POE.Helper;
using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    [SessionAuthorize]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            return View();
        }
    }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//