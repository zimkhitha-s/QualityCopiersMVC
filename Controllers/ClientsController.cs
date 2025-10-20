using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class ClientsController : Controller
    {

        private readonly FirebaseService _firebaseService;

        public ClientsController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
        public async Task<IActionResult> Clients()
        {
            try
            {
                // Replace with the actual userId document that holds the clients
                string userId = "ypqdjnU59xfE6cdE4NoKPAoWPfA2";

                var clients = await _firebaseService.GetClientsAsync(userId);

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
            // Double-check server-side validation
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all required fields before submitting.";
                return View(client);
            }

            try
            {
                // Generate unique ID
                client.id = Guid.NewGuid().ToString();

                // Try to save client in Firebase
                await _firebaseService.AddClientAsync(client);

                // Notify user of success
                TempData["SuccessMessage"] = $"{client.name} {client.surname} has been successfully added!";
                return RedirectToAction("Clients", "Clients");
            }
            catch (Exception ex)
            {
                // Catch and log Firebase or network errors
                // Optionally log ex.Message for debugging if you have logging enabled

                TempData["ErrorMessage"] = "An error occurred while adding the client. Please try again.";
                return View(client);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteClient([FromBody] string clientId)
        {

            if (string.IsNullOrEmpty(clientId))
                return BadRequest("Client ID is missing.");

            try
            {
                string userId = "ypqdjnU59xfE6cdE4NoKPAoWPfA2"; // your user ID
                await _firebaseService.DeleteClientAsync(userId, clientId);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

    }
}

