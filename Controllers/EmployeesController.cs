using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public EmployeesController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
        public async Task<IActionResult> Employees()
        {
            try
            {
                var employees = await _firebaseService.GetAllEmployeesAsync();

                return View(employees);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading employees: {ex.Message}";
                return View(new List<Employee>());
            }
        }


        [HttpGet]
        public IActionResult AddEmployees()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployees(Employee employee)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return View(employee);
            }

            try
            {
                // Sanitize and trim inputs
                employee.Name = System.Net.WebUtility.HtmlEncode(employee.Name.Trim());
                employee.Surname = System.Net.WebUtility.HtmlEncode(employee.Surname.Trim());
                employee.Email = System.Net.WebUtility.HtmlEncode(employee.Email.Trim().ToLower());
                employee.PhoneNumber = System.Net.WebUtility.HtmlEncode(employee.PhoneNumber.Trim());
                employee.IdNumber = System.Net.WebUtility.HtmlEncode(employee.IdNumber.Trim());
                employee.FullName = $"{employee.Name} {employee.Surname}";

                // Set timestamp
                employee.CreatedAtDateTime = DateTime.UtcNow;

                // Call Firebase service to add employee
                var result = await _firebaseService.AddEmployeeAsync(employee);

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return View(employee);
                }

                TempData["SuccessMessage"] = "Employee added successfully!";
                return RedirectToAction("Employees", "Employees");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error adding employee: {ex.Message}";
                return View(employee);
            }
        }
    }
}
