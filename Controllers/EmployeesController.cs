using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class EmployeesController : Controller
    {
        [HttpGet]
        public IActionResult Employees()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AddEmployees()
        {
            return View();
        }
    }
}
