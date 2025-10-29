using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace INSY7315_ElevateDigitalStudios_POE.Models
{
    [FirestoreData]
    public class Invoice
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [Required(ErrorMessage = "Client name is required")]
        [RegularExpression(@"^[a-zA-Z\s'-]{1,50}$", ErrorMessage = "Client name contains invalid characters or exceeds 50 characters.")]
        [FirestoreProperty("clientName")]
        public string ClientName { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(100, ErrorMessage = "Email address cannot exceed 100 characters")]
        [FirestoreProperty("email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Phone number must contain only digits and may start with '+'")]
        [FirestoreProperty("phone")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Invoice number is required")]
        [RegularExpression(@"^[#A-Z0-9\-]{1,20}$", ErrorMessage = "Invoice number contains invalid characters")]
        [FirestoreProperty("invoiceNumber")]
        public string InvoiceNumber { get; set; }

        [Required(ErrorMessage = "Total amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than zero")]
        [FirestoreProperty("totalAmount")]
        public double TotalAmount { get; set; }

        [Required(ErrorMessage = "Company name is required")]
        [RegularExpression(@"^[a-zA-Z\s'-]{2,100}$", ErrorMessage = "The company name contains invalid characters")]
        [FirestoreProperty("companyName")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [RegularExpression(@"^(Unpaid|Paid)$", ErrorMessage = "Status must be either 'Unpaid' or 'Paid'")]
        [FirestoreProperty("status")]
        public string Status { get; set; } = "Unpaid";

        [FirestoreProperty("quoteNumber")]
        [MaxLength(100, ErrorMessage = "Quote number cannot exceed 100 characters")]
        public string? QuoteNumber { get; set; }

        [FirestoreProperty("address")]
        [MaxLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string? Address { get; set; }

        [FirestoreProperty("pdfFileName")]
        public string? PdfFileName { get; set; }

        [FirestoreProperty("month")]
        public string? Month { get; set; }

        [FirestoreProperty("year")]
        public string? Year { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp? CreatedAt { get; set; }

        [FirestoreProperty("items")]
        public List<InvoiceItem> Items { get; set; } = new();
    }
}
