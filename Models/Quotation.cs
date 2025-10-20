using Google.Cloud.Firestore;

namespace INSY7315_ElevateDigitalStudios_POE.Models
{
    [FirestoreData]
    public class Quotation
    {
        [FirestoreDocumentId]
        public string id { get; set; }

        [FirestoreProperty("clientName")]
        public string clientName { get; set; }

        [FirestoreProperty("companyName")]
        public string companyName { get; set; }

        [FirestoreProperty("email")]
        public string email { get; set; }

        [FirestoreProperty("phone")]
        public string phone { get; set; }

        [FirestoreProperty("address")]
        public string address { get; set; }

        [FirestoreProperty("amount")]
        public string amount { get; set; }

        [FirestoreProperty("quoteNumber")]
        public string quoteNumber { get; set; }

        [FirestoreProperty("notes")]
        public string notes { get; set; }

        [FirestoreProperty("pdfFileName")]
        public string pdfFileName { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp createdAt { get; set; }

        [FirestoreProperty("status")]
        public string status { get; set; } = "In Progress";

        [FirestoreProperty("items")]
        public List<QuotationItem> Items { get; set; } = new();
    }
}
