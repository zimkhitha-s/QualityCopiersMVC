using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Models.Requests;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class InvoicesController : Controller
    {
        // dependency injection of the firebase service
        private readonly FirebaseService _firebaseService;

        // constructor - inject firebase service
        public InvoicesController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        // get endpoint - invoices endpoint - load all invoices
        [HttpGet]
        public async Task<IActionResult> Invoices()
        {
            // get user info from session
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            try
            {
                // fetch invoices from firebase
                var invoices = await _firebaseService.GetInvoicesAsync();

                // check if no invoices found
                if (invoices == null || !invoices.Any())
                {
                    ViewBag.NoInvoices = true;
                    return View(new List<Invoice>());
                }

                // sanitize invoice data to prevent xss - cross site scripting
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

                // return the invoices view with sanitized data
                return View(invoices);
            }
            catch (Exception ex)
            {
                // handle errors and display message
                ViewBag.ErrorMessage = $"Error loading invoices: {WebUtility.HtmlEncode(ex.Message)}";
                return View(new List<Invoice>());
            }
        }

        // post endpoint - generate and download invoice pdf
        [HttpPost]
        public async Task<IActionResult> SaveInvoice([FromBody] InvoiceRequest request)
        {
            // validate request - check for null or empty invoice id
            if (request == null || string.IsNullOrEmpty(request.InvoiceId))
                return BadRequest(new { message = "Invalid invoice ID" });

            // fetch invoices and find the requested invoice
            var invoices = await _firebaseService.GetInvoicesAsync();
            var invoice = invoices.FirstOrDefault(i => i.Id == request.InvoiceId);

            // check if invoice exists
            if (invoice == null)
                return NotFound(new { message = "Invoice not found" });

            try
            {
                // generate pdf bytes and return as file download
                (byte[] pdfBytes, string pdfFileName) = await _firebaseService.GenerateInvoicePdfBytesAsync(invoice);
                return File(pdfBytes, "application/pdf", pdfFileName);
            }
            catch (Exception ex)
            {
                // handle errors during pdf generation
                return StatusCode(500, new { message = $"Error generating invoice: {ex.Message}" });
            }
        }

        // post endpoint - update invoice status and validate against CSRF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus([FromBody] InvoiceRequest request)
        {
            // validate request
            if (request == null || string.IsNullOrEmpty(request.InvoiceId))
                return BadRequest("Invalid request");

            try
            {
                // update invoice status in firebase
                await _firebaseService.UpdateInvoiceStatusAsync(request.InvoiceId, request.Status);

                return Ok(new { message = "Status updated successfully" });
            }
            catch (Exception ex)
            {
                // handle errors during status update
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // post endpoint - send invoice via email and validate against CSRF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInvoice([FromBody] InvoiceRequest request)
        {
            // validate request
            if (request == null || string.IsNullOrEmpty(request.InvoiceId))
                return BadRequest(new { message = "Invalid invoice ID" });

            try
            {
                // fetch invoices and find the requested invoice
                var invoices = await _firebaseService.GetInvoicesAsync();
                var invoice = invoices.FirstOrDefault(i => i.Id == request.InvoiceId);

                // check if invoice exists
                if (invoice == null)
                    return NotFound(new { message = "Invoice not found" });

                // generate and send invoice email
                bool success = await _firebaseService.GenerateAndSendInvoiceAsync(invoice);

                // check if email sending was successful
                if (!success)
                    return StatusCode(500, new { message = "Failed to generate or send invoice" });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // handle errors during email sending
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // post endpoint - delete invoice
        [HttpPost]
        public async Task<IActionResult> DeleteInvoice([FromBody] InvoiceRequest request)
        {
            // validate request
            if (request == null || string.IsNullOrEmpty(request.InvoiceId))
                return BadRequest(new { message = "Invalid invoice ID" });

            // delete invoice from firebase
            await _firebaseService.DeleteInvoiceAsync(request.InvoiceId);

            return Json(new { success = true });
        }
    }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//