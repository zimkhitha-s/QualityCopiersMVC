using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;
using System.Linq;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public PaymentsController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        // ✅ 1️⃣ Load all invoices (Unpaid + Paid)
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

                // Sanitize for HTML safety
                foreach (var inv in invoices)
                {
                    inv.ClientName = WebUtility.HtmlEncode(inv.ClientName ?? "");
                    inv.CompanyName = WebUtility.HtmlEncode(inv.CompanyName ?? "");
                    inv.Email = WebUtility.HtmlEncode(inv.Email ?? "");
                    inv.Phone = WebUtility.HtmlEncode(inv.Phone ?? "");
                    inv.Address = WebUtility.HtmlEncode(inv.Address ?? "");
                    inv.InvoiceNumber = WebUtility.HtmlEncode(inv.InvoiceNumber ?? "");
                }

                return View(invoices);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading invoices: {WebUtility.HtmlEncode(ex.Message)}";
                return View(new List<Invoice>());
            }
        }

        // ✅ 2️⃣ Mark as Paid (Triggers Cloud Function)
        [HttpPost("Payments/MarkAsPaid/{invoiceId}")]
        public async Task<IActionResult> MarkAsPaid(string invoiceId)
        {
            try
            {
                await _firebaseService.UpdateInvoiceStatusAsync(invoiceId, "Paid");

                var invoice = await _firebaseService.GetInvoiceDetailsAsync(invoiceId);
                if (invoice != null)
                    await _firebaseService.GenerateAndSendInvoiceAsync(invoice);

                return Json(new { success = true, message = "Invoice marked as Paid! Payment record will auto-generate." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // ✅ 3️⃣ Get Invoice Details (used by modal)
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

        // ✅ 4️⃣ Download PDF Report (Last 3/6/12 months)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadPaymentsReport([FromBody] DateRangeRequest request)
        {
            if (request == null || request.Months <= 0)
                return BadRequest(new { message = "Invalid date range selection." });

            try
            {
                var invoices = await _firebaseService.GetPaidInvoicesByDateRangeAsync(request.Months);
                if (!invoices.Any())
                    return NotFound(new { message = "No paid invoices found in this range." });

                var (pdfBytes, fileName) = await _firebaseService.GeneratePaymentsReportPdfAsync(invoices, request.Months);
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error generating report: {ex.Message}" });
            }
        }

        // ✅ 5️⃣ Delete Invoice (from modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInvoice([FromBody] DeleteInvoiceRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.InvoiceId))
                return BadRequest(new { success = false, message = "Invalid invoice ID." });

            try
            {
                await _firebaseService.DeleteInvoiceAsync(request.InvoiceId);
                return Json(new { success = true, message = "Invoice deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting invoice: {ex.Message}" });
            }
        }

        public class DateRangeRequest
        {
            public int Months { get; set; }
        }

        public class DeleteInvoiceRequest
        {
            public string InvoiceId { get; set; }
        }
    }
}