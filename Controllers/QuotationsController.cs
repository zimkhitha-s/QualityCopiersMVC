using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Models.Requests;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class QuotationsController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public QuotationsController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
        public async Task<IActionResult> Quotations()
        {
            try
            {
                var quotations = await _firebaseService.GetQuotationsAsync();

                if (quotations == null || quotations.Count == 0)
                {
                    ViewBag.Message = "No Quotations Yet.";
                }

                return View(quotations);
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error loading quotations. Please try again later.";
                Console.WriteLine($"Error: {ex.Message}");
                return View(new List<Quotation>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateQuotation()
        {
            string userId = "ypqdjnU59xfE6cdE4NoKPAoWPfA2";
            var clients = await _firebaseService.GetClientsAsync(userId);
            ViewBag.Clients = clients;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateQuotation(Quotation quotation)
        {
            if (quotation == null || quotation.Items.Count == 0)
            {
                ModelState.AddModelError("", "Please add at least one quotation item.");
                return View(quotation);
            }

            await _firebaseService.AddQuotationAsync(quotation);
            TempData["SuccessMessage"] = "Quotation created successfully!";
            return RedirectToAction("Quotations", "Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuotationStatus([FromBody] Quotation statusUpdate)
        {
            if (statusUpdate == null || string.IsNullOrEmpty(statusUpdate.id))
                return BadRequest();

            try
            {
                string userId = "vz4maSc0vOgouOGPhtdkFzBlceK2";
                await _firebaseService.UpdateQuotationStatusAsync(userId, statusUpdate.id, statusUpdate.status);

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "Failed to update status");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQuotation([FromBody] DeleteQuotationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.QuotationId))
                return BadRequest(new { message = "Invalid quotation ID" });

            string userId = "vz4maSc0vOgouOGPhtdkFzBlceK2";
            await _firebaseService.DeleteQuotationAsync(userId, request.QuotationId);

            return Json(new { success = true });
        }
    }
}
