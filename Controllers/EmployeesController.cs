using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Models.Dtos;
using INSY7315_ElevateDigitalStudios_POE.Models.Requests;
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

        [HttpGet]
        public async Task<IActionResult> GetEmployee(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Employee ID is required.");

            Employee employee = await _firebaseService.GetEmployeeByIdAsync(id);
            if (employee == null) return NotFound();

            return Json(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmployee([FromBody] EmployeeUpdateDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Id))
                return BadRequest("Invalid employee data.");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { message = "Validation failed", errors });
            }

            try
            {
                // Sanitize inputs
                dto.FullName = System.Net.WebUtility.HtmlEncode(dto.FullName?.Trim());
                dto.Email = System.Net.WebUtility.HtmlEncode(dto.Email?.Trim().ToLower());
                dto.PhoneNumber = System.Net.WebUtility.HtmlEncode(dto.PhoneNumber?.Trim());

                await _firebaseService.UpdateEmployeeAsync(dto);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating employee: {ex.Message}");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee([FromBody] DeleteEmployeeRequest request)
        {
            if (string.IsNullOrEmpty(request?.EmployeeId))
                return BadRequest("Employee ID is missing.");

            try
            {
                await _firebaseService.DeleteEmployeeAsync(request.EmployeeId);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
