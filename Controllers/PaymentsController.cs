using INSY7315_ElevateDigitalStudios_POE.Models;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;
using INSY7315_ElevateDigitalStudios_POE.Models.Requests;

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
                var payments = await _firebaseService.GetPaymentsAsync();
                if (payments == null || !payments.Any())
                {
                    ViewBag.NoPayments = true;
                    return View(new List<Payment>());
                }

                // optional: sanitize strings
                foreach (var p in payments)
                {
                    p.ClientName = WebUtility.HtmlEncode(p.ClientName ?? string.Empty);
                    p.CompanyName = WebUtility.HtmlEncode(p.CompanyName ?? string.Empty);
                    p.Email = WebUtility.HtmlEncode(p.Email ?? string.Empty).ToLowerInvariant();
                    p.Phone = WebUtility.HtmlEncode(p.Phone ?? string.Empty);
                    p.Address = WebUtility.HtmlEncode(p.Address ?? string.Empty);
                    p.InvoiceNumber = WebUtility.HtmlEncode(p.InvoiceNumber ?? string.Empty);
                }

                return View(payments);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading payments: {WebUtility.HtmlEncode(ex.Message)}";
                return View(new List<Payment>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadPaymentsReport([FromBody] PaymentReportRequest request)
        {
            if (request == null || request.Months <= 0)
                return BadRequest("Invalid request.");

            try
            {
                // 1Get payments from Firebase within the specified period
                var payments = await _firebaseService.GetPaymentsForPeriodAsync(request.Months);

                if (payments == null || !payments.Any())
                    return BadRequest("No payments found for this period.");

                // Generate the payment report PDF using the Firebase service
                string pdfPath = await _firebaseService.GeneratePaymentReportPdfAsync(payments, request.Months);

                if (!System.IO.File.Exists(pdfPath))
                    return StatusCode(500, "Failed to generate payment report.");

                // Return the PDF as a downloadable file
                var fileBytes = await System.IO.File.ReadAllBytesAsync(pdfPath);
                var fileName = $"Payments_Report_Last_{request.Months}_Months.pdf";

                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating payments report: {ex.Message}");
                return StatusCode(500, "An error occurred while generating the payments report.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SavePayment([FromBody] PaymentsRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.PaymentId))
                return BadRequest(new { message = "Invalid payment ID" });

            // fetch payments and find the requested one
            var payments = await _firebaseService.GetPaymentsAsync();
            var payment = payments.FirstOrDefault(p => p.Id == request.PaymentId);

            if (payment == null)
                return NotFound(new { message = "Payment not found" });

            try
            {
                // generate pdf bytes for payment
                (byte[] pdfBytes, string pdfFileName) = await _firebaseService.GeneratePaymentPdfBytesAsync(payment);
                return File(pdfBytes, "application/pdf", pdfFileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error generating payment PDF: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPayment([FromBody] PaymentsRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.PaymentId))
                return BadRequest(new { message = "Invalid payment ID" });

            try
            {
                // Fetch all payments
                var payments = await _firebaseService.GetPaymentsAsync();
                var payment = payments.FirstOrDefault(p => p.Id == request.PaymentId);

                if (payment == null)
                    return NotFound(new { message = "Payment not found" });

                // Generate and send payment email
                bool success = await _firebaseService.GenerateAndSendPaymentAsync(payment);

                if (!success)
                    return StatusCode(500, new { message = "Failed to generate or send payment" });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePayment([FromBody] PaymentsRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.PaymentId))
                return BadRequest(new { message = "Invalid payment ID" });

            try
            {
                await _firebaseService.DeletePaymentAsync(request.PaymentId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // log if needed
                Console.WriteLine($"[ERROR] Failed to delete payment {request.PaymentId}: {ex}");
                return StatusCode(500, new { success = false, message = "Failed to delete payment." });
            }
        }
    }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//