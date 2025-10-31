using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Models.Requests;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class InvoicesController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public InvoicesController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
        public async Task<IActionResult> Invoices()
        {
            try
            {
                var invoices = await _firebaseService.GetInvoicesAsync();

                if (invoices == null || !invoices.Any())
                {
                    ViewBag.NoInvoices = true;
                    return View(new List<Invoice>());
                }

                // sanitize invoice data to prevent XSS
                foreach (var inv in invoices)
                {
                    inv.ClientName = WebUtility.HtmlEncode((inv.ClientName ?? string.Empty).Trim());
                    inv.CompanyName = WebUtility.HtmlEncode((inv.CompanyName ?? string.Empty).Trim());
                    inv.Email = WebUtility.HtmlEncode((inv.Email ?? string.Empty).Trim().ToLowerInvariant());
                    inv.Phone = WebUtility.HtmlEncode((inv.Phone ?? string.Empty).Trim());
                    inv.Address = WebUtility.HtmlEncode((inv.Address ?? string.Empty).Trim());
                    inv.InvoiceNumber = WebUtility.HtmlEncode((inv.InvoiceNumber ?? string.Empty).Trim());
                    inv.Status = WebUtility.HtmlEncode((inv.Status ?? string.Empty).Trim());
                }

                return View(invoices);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading invoices: {WebUtility.HtmlEncode(ex.Message)}";
                return View(new List<Invoice>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveInvoice([FromBody] InvoiceRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.InvoiceId))
                return BadRequest(new { message = "Invalid invoice ID" });

            var invoices = await _firebaseService.GetInvoicesAsync();
            var invoice = invoices.FirstOrDefault(i => i.Id == request.InvoiceId);

            if (invoice == null)
                return NotFound(new { message = "Invoice not found" });

            try
            {
                // The Firebase service now handles everything: naming, generation, and byte retrieval
                (byte[] pdfBytes, string pdfFileName) = await _firebaseService.GenerateInvoicePdfBytesAsync(invoice);
                return File(pdfBytes, "application/pdf", pdfFileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error generating invoice: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus([FromBody] InvoiceRequest request)
        {
            Console.WriteLine($"[DEBUG] InvoiceId={request.InvoiceId}, Status={request.Status}");

            if (request == null || string.IsNullOrEmpty(request.InvoiceId))
                return BadRequest("Invalid request");

            try
            {
                // Call your Firebase service to update the invoice
                await _firebaseService.UpdateInvoiceStatusAsync(request.InvoiceId, request.Status);

                return Ok(new { message = "Status updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInvoice([FromBody] InvoiceRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.InvoiceId))
                return BadRequest(new { message = "Invalid invoice ID" });

            try
            {
                // retrieve the invoice by document ID
                var invoices = await _firebaseService.GetInvoicesAsync();
                var invoice = invoices.FirstOrDefault(i => i.Id == request.InvoiceId);

                if (invoice == null)
                    return NotFound(new { message = "Invoice not found" });

                // generate pdf and send email
                bool success = await _firebaseService.GenerateAndSendInvoiceAsync(invoice);

                if (!success)
                    return StatusCode(500, new { message = "Failed to generate or send invoice" });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteInvoice([FromBody] InvoiceRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.InvoiceId))
                return BadRequest(new { message = "Invalid invoice ID" });


            await _firebaseService.DeleteInvoiceAsync(request.InvoiceId);

            return Json(new { success = true });
        }
    }
}
