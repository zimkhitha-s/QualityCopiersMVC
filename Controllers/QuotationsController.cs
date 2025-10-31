using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Models.Requests;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;

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
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
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
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            try
            {
                // Controller does not pass a userId — firebase service handles that.
                var clients = await _firebaseService.GetClientsAsync();
                ViewBag.Clients = clients;
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading clients: {ex.Message}";
                ViewBag.Clients = Enumerable.Empty<object>();
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuotation(Quotation quotation)
        {
            // Basic server-side ModelState checks first
            if (quotation == null)
            {
                ModelState.AddModelError("", "Invalid request.");
                return View(quotation);
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return View(quotation);
            }

            if (quotation.Items == null || !quotation.Items.Any())
            {
                ModelState.AddModelError("", "Please add at least one quotation item.");
                TempData["ErrorMessage"] = "Please add at least one quotation item.";
                return View(quotation);
            }

            // Sanitize and normalize top-level fields
            try
            {
                quotation.clientName = WebUtility.HtmlEncode((quotation.clientName ?? string.Empty).Trim());
                quotation.companyName = WebUtility.HtmlEncode((quotation.companyName ?? string.Empty).Trim());
                quotation.email = WebUtility.HtmlEncode((quotation.email ?? string.Empty).Trim().ToLowerInvariant());
                quotation.phone = WebUtility.HtmlEncode((quotation.phone ?? string.Empty).Trim());
                quotation.notes = WebUtility.HtmlEncode((quotation.notes ?? string.Empty).Trim());

                // Validate and sanitize items
                for (int i = 0; i < quotation.Items.Count; i++)
                {
                    var it = quotation.Items[i];
                    if (it == null)
                    {
                        ModelState.AddModelError("", $"Quotation item #{i + 1} is invalid.");
                        TempData["ErrorMessage"] = $"Quotation item #{i + 1} is invalid.";
                        return View(quotation);
                    }

                    // trim text fields
                    it.description = WebUtility.HtmlEncode((it.description ?? string.Empty).Trim());

                    // ensure sensible numeric values (prevent negative or null)
                    if (it.quantity < 0) it.quantity = 0;
                    if (it.unitPrice < 0) it.unitPrice = 0.0;

                    // ensure amount is derived only on the server in the service, but keep it zero here
                    it.amount = 0;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error preparing data: {ex.Message}";
                return View(quotation);
            }

            // Pass to firebase service — service handles encryption and DB specifics
            try
            {
                var result = await _firebaseService.AddQuotationAsync(quotation);

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to save quotation.";
                    return View(quotation);
                }

                TempData["SuccessMessage"] = "Quotation created successfully!";
                return RedirectToAction("Quotations", "Quotations");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Unexpected error: {ex.Message}";
                return View(quotation);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DownloadQuotation([FromBody] QuotationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.QuotationId))
                return BadRequest(new { message = "Invalid quotation ID" });

            var quotations = await _firebaseService.GetQuotationsAsync();
            var quotation = quotations.FirstOrDefault(q => q.id == request.QuotationId);

            if (quotation == null)
                return NotFound(new { message = "Quotation not found" });

            try
            {
                var (pdfBytes, pdfFileName) = await _firebaseService.GenerateQuotationPdfBytesAsync(quotation);
                return File(pdfBytes, "application/pdf", pdfFileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error generating quotation: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQuotation([FromBody] QuotationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.QuotationId))
                return BadRequest(new { message = "Invalid quotation ID" });

            
            await _firebaseService.DeleteQuotationAsync(request.QuotationId);

            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult Confirmation(string status)
        {
            ViewBag.Status = status;
            return View();
        }
    }
}
