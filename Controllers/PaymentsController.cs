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
        // dependencies injection - firebase service
        private readonly FirebaseService _firebaseService;

        public PaymentsController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        // load all invoices - Unpaid and Paid
        [HttpGet]
        public async Task<IActionResult> Payments()
        {
            // set session variables for view
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

            try
            {
                // fetch invoices from firebase
                var invoices = await _firebaseService.GetInvoicesAsync();
                if (invoices == null || !invoices.Any())
                {
                    // no invoices found
                    ViewBag.NoInvoices = true;
                    return View(new List<Invoice>());
                }

                // sanitize for html safety
                foreach (var inv in invoices)
                {
                    inv.ClientName = WebUtility.HtmlEncode(inv.ClientName ?? "");
                    inv.CompanyName = WebUtility.HtmlEncode(inv.CompanyName ?? "");
                    inv.Email = WebUtility.HtmlEncode(inv.Email ?? "");
                    inv.Phone = WebUtility.HtmlEncode(inv.Phone ?? "");
                    inv.Address = WebUtility.HtmlEncode(inv.Address ?? "");
                    inv.InvoiceNumber = WebUtility.HtmlEncode(inv.InvoiceNumber ?? "");
                }

                // return view with invoices
                return View(invoices);
            }
            catch (Exception ex)
            {
                // handle errors
                ViewBag.ErrorMessage = $"Error loading invoices: {WebUtility.HtmlEncode(ex.Message)}";
                return View(new List<Invoice>());
            }
        }

        // mark as paid triggers the cloud function
        [HttpPost("Payments/MarkAsPaid/{invoiceId}")]
        public async Task<IActionResult> MarkAsPaid(string invoiceId)
        {
            try
            {
                // update invoice status to paid
                await _firebaseService.UpdateInvoiceStatusAsync(invoiceId, "Paid");

                // fetch invoice details
                var invoice = await _firebaseService.GetInvoiceDetailsAsync(invoiceId);
                if (invoice != null)
                    await _firebaseService.GenerateAndSendInvoiceAsync(invoice);

                // return success response
                return Json(new { success = true, message = "Invoice marked as Paid! Payment record will auto-generate." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // get invoice details
        [HttpGet]
        public async Task<IActionResult> GetInvoiceDetails(string id)
        {
            try
            {
                // validate id
                if (string.IsNullOrEmpty(id))
                    return BadRequest(new { error = "Missing invoice ID." });

                // fetch invoice details
                var invoice = await _firebaseService.GetInvoiceDetailsAsync(id);
                if (invoice == null)
                    return NotFound(new { error = "Invoice not found." });

                // return invoice as json
                return Json(invoice);
            }
            catch (Exception ex)
            {
                // handle errors
                return StatusCode(500, new { error = $"Error fetching invoice: {ex.Message}" });
            }
        }

        // download pdf report last - 3/6/12 months
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

        // delete invoice
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
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//