using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace INSY7315_ElevateDigitalStudios_POE.Models
{
    [FirestoreData]
    public class Quotation
    {
        [FirestoreDocumentId]
        public string? id { get; set; }

        [Required(ErrorMessage = "Client name is required")]
        [RegularExpression(@"^[a-zA-Z\s'-]{1,50}$", ErrorMessage = "The client name contains invalid characters")]
        [FirestoreProperty("clientName")]
        public string clientName { get; set; }

        [Required(ErrorMessage = "Company name is required")]
        [RegularExpression(@"^[a-zA-Z\s'-]{2,100}$", ErrorMessage = "The company name contains invalid characters")]
        [FirestoreProperty("companyName")]
        public string companyName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [FirestoreProperty("email")]
        public string email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Phone number must be 10-15 digits and can start with +")]
        [FirestoreProperty("phone")]
        public string phone { get; set; }

        [FirestoreProperty("amount")]
        public double amount { get; set; } = 0.00;

        [RegularExpression(@"^[#A-Z0-9\-]{1,20}$", ErrorMessage = "The quote number contains invalid characters")]
        [FirestoreProperty("quoteNumber")]
        public string? quoteNumber { get; set; }

        [FirestoreProperty("notes")]
        public string? notes { get; set; }

        [FirestoreProperty("pdfFileName")]
        public string? pdfFileName { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp createdAt { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [RegularExpression(@"^(Pending|Accepted|Declined)$", ErrorMessage = "Status must be Pending, Accepted or Declined")]
        [FirestoreProperty("status")]
        public string status { get; set; } = "Pending";

        [FirestoreProperty("secureToken")]
        public string? secureToken { get; set; }

        [FirestoreProperty("items")]
        public List<QuotationItem> Items { get; set; } = new();
    }
}
