using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace INSY7315_ElevateDigitalStudios_POE.Models
{
    // invoice model with Firestore mapping and validation
    [FirestoreData]
    public class Invoice
    {
        // invoice id
        [FirestoreDocumentId]
        public string? Id { get; set; }

        // client name - only letters, spaces, apostrophes, hyphens, max 50 chars
        [Required(ErrorMessage = "Client name is required")]
        [RegularExpression(@"^[a-zA-Z\s'-]{1,50}$", ErrorMessage = "Client name contains invalid characters or exceeds 50 characters.")]
        [FirestoreProperty("clientName")]
        public string ClientName { get; set; }

        // email - valid email format, max 100 chars
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(100, ErrorMessage = "Email address cannot exceed 100 characters")]
        [FirestoreProperty("email")]
        public string Email { get; set; }

        // phone - digits only, may start with +, length 10-15
        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Phone number must contain only digits and may start with '+'")]
        [FirestoreProperty("phone")]
        public string Phone { get; set; }

        // invoice number - uppercase letters, digits, #, hyphens, max 20 chars
        [Required(ErrorMessage = "Invoice number is required")]
        [RegularExpression(@"^[#A-Z0-9\-]{1,20}$", ErrorMessage = "Invoice number contains invalid characters")]
        [FirestoreProperty("invoiceNumber")]
        public string InvoiceNumber { get; set; }

        // total amount - must be greater than zero
        [Required(ErrorMessage = "Total amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than zero")]
        [FirestoreProperty("totalAmount")]
        public double TotalAmount { get; set; }

        // company name - only letters, spaces, apostrophes, hyphens, length 2-100
        [Required(ErrorMessage = "Company name is required")]
        [RegularExpression(@"^[a-zA-Z\s'-]{2,100}$", ErrorMessage = "The company name contains invalid characters")]
        [FirestoreProperty("companyName")]
        public string CompanyName { get; set; }

        // status - must be either 'Unpaid' or 'Paid'
        [Required(ErrorMessage = "Status is required")]
        [RegularExpression(@"^(Unpaid|Paid)$", ErrorMessage = "Status must be either 'Unpaid' or 'Paid'")]
        [FirestoreProperty("status")]
        public string Status { get; set; } = "Unpaid";

        // quote number - max 100 chars
        [FirestoreProperty("quoteNumber")]
        [MaxLength(100, ErrorMessage = "Quote number cannot exceed 100 characters")]
        public string? QuoteNumber { get; set; }

        // address - max 200 chars
        [FirestoreProperty("address")]
        [MaxLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string? Address { get; set; }

        // pdf file name
        [FirestoreProperty("pdfFileName")]
        public string? PdfFileName { get; set; }

        // month
        [FirestoreProperty("month")]
        public string? Month { get; set; }

        // year
        [FirestoreProperty("year")]
        public string? Year { get; set; }

        // creation timestamp
        [FirestoreProperty("createdAt")]
        public Timestamp? CreatedAt { get; set; }

        // list of invoice items
        [FirestoreProperty("items")]
        public List<InvoiceItem> Items { get; set; } = new();
    }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//