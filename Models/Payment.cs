using Google.Cloud.Firestore;

namespace INSY7315_ElevateDigitalStudios_POE.Models
{
    [FirestoreData]
    public class Payment
    {
        [FirestoreDocumentId()]
        public string Id { get; set; }

        [FirestoreProperty("invoiceNumber")]
        public string InvoiceNumber { get; set; }

        [FirestoreProperty("quotationNumber")]
        public string QuoteNumber { get; set; }

        [FirestoreProperty("clientName")]
        public string ClientName { get; set; }

        [FirestoreProperty("companyName")]
        public string CompanyName { get; set; }

        [FirestoreProperty("email")]
        public string Email { get; set; }

        [FirestoreProperty("phone")]
        public string Phone { get; set; }

        [FirestoreProperty("address")]
        public string Address { get; set; }

        [FirestoreProperty]
        public List<Dictionary<string, object>> Items { get; set; } = new List<Dictionary<string, object>>();

        [FirestoreProperty("amount")]
        public double? Amount { get; set; }

        [FirestoreProperty("totalAmount")]
        public double? TotalAmount { get; set; }

        [FirestoreProperty("status")]
        public string Status { get; set; } = "Paid";

        [FirestoreProperty("createdAt")]
        public Timestamp? CreatedAt { get; set; }

        [FirestoreProperty("paidAt")]
        public Timestamp? PaidAt { get; set; }

        [FirestoreProperty("month")]
        public string Month { get; set; }

        [FirestoreProperty("year")]
        public string Year { get; set; }
    }
}
