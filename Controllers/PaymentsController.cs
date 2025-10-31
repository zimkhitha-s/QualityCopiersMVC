using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Models.Requests;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public PaymentsController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
        public async Task<IActionResult> Payments()
        {
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
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
                }

                return View(invoices);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading invoices: {WebUtility.HtmlEncode(ex.Message)}";
                return View(new List<Invoice>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoiceDetails(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return BadRequest(new { error = "Missing invoice ID." });

                var invoice = await _firebaseService.GetInvoiceDetailsAsync(id);

                if (invoice == null)
                    return NotFound(new { error = "Invoice not found." });

                return Json(invoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error fetching invoice: {ex.Message}" });
            }
        }
        
        [HttpPost("MarkAsPaid/{invoiceId}")]
        public async Task<IActionResult> MarkAsPaid(string invoiceId)
        {
            try
            {
                await _firebaseService.MarkInvoiceAsPaidAsync(invoiceId);
                return Ok("Invoice marked as paid ✅");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Error updating invoice: {ex.Message}");
                return BadRequest($"Error: {ex.Message}");
            }
        }
    }
}