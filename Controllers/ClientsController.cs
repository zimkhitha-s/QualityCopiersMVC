using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Models.Dtos;
using INSY7315_ElevateDigitalStudios_POE.Models.Requests;
using INSY7315_ElevateDigitalStudios_POE.Security;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class ClientsController : Controller
    {

        private readonly FirebaseService _firebaseService;
        private readonly EncryptionHelper _encryptionHelper;

        public ClientsController(FirebaseService firebaseService, EncryptionHelper encryptionHelper)
        {
            _firebaseService = firebaseService;
            _encryptionHelper = encryptionHelper;
        }

        [HttpGet]
        public async Task<IActionResult> Clients()
        {
            try
            {
                var clients = await _firebaseService.GetClientsAsync();
                return View(clients);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unable to fetch clients.";
                return View(new List<Client>());
            }
        }

        [HttpGet]
        public IActionResult AddClients()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddClients(Client client)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all required fields before submitting.";
                return View(client);
            }

            try
            {
                // Input sanitization
                client.name = System.Net.WebUtility.HtmlEncode(client.name.Trim());
                client.surname = System.Net.WebUtility.HtmlEncode(client.surname.Trim());
                client.email = System.Net.WebUtility.HtmlEncode(client.email.Trim().ToLower());
                client.phoneNumber = System.Net.WebUtility.HtmlEncode(client.phoneNumber.Trim());
                client.address = System.Net.WebUtility.HtmlEncode(client.address.Trim());
                client.companyName = System.Net.WebUtility.HtmlEncode(client.companyName.Trim());

                // Additional business logic validation
                if (!client.email.Contains("@") || !client.email.Contains("."))
                {
                    ModelState.AddModelError("email", "Email format is invalid.");
                    return View(client);
                }

                

                // Save to Firebase (encryption handled inside the service)
                await _firebaseService.AddClientAsync(client);

                TempData["SuccessMessage"] = $"{client.name} {client.surname} has been successfully added!";
                return RedirectToAction("Clients", "Clients");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to add client: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while adding the client. Please try again.";
                return View(client);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetClient(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Client ID is required.");

            Client client = await _firebaseService.GetClientByIdAsync(id);
            if (client == null) return NotFound();

            return Json(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateClient([FromBody] ClientUpdateDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Id))
                return BadRequest("Invalid client data.");

            if (!ModelState.IsValid)
            {
                // Log the validation errors
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { message = "Validation failed", errors });
            }

            try
            {
                // Sanitize inputs
                dto.FullName = System.Net.WebUtility.HtmlEncode(dto.FullName?.Trim());
                dto.CompanyName = System.Net.WebUtility.HtmlEncode(dto.CompanyName?.Trim());
                dto.Email = System.Net.WebUtility.HtmlEncode(dto.Email?.Trim().ToLower());
                dto.PhoneNumber = System.Net.WebUtility.HtmlEncode(dto.PhoneNumber?.Trim());
                dto.Address = System.Net.WebUtility.HtmlEncode(dto.Address?.Trim());

                await _firebaseService.UpdateClientAsync(dto);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating client: {ex.Message}");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClient([FromBody] DeleteClientRequest request)
        {
            if (string.IsNullOrEmpty(request?.ClientId))
                return BadRequest("Client ID is missing.");

            try
            {
                await _firebaseService.DeleteClientAsync(request.ClientId);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}

