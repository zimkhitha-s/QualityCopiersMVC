using Google.Cloud.Firestore;

namespace INSY7315_ElevateDigitalStudios_POE.Models
{
    // payment model
    [FirestoreData]
    public class Payment
    {
        // payment id
        [FirestoreDocumentId()]
        public string Id { get; set; }

        // invoice number
        [FirestoreProperty("invoiceNumber")]
        public string InvoiceNumber { get; set; }

        // quotation number
        [FirestoreProperty("quotationNumber")]
        public string QuoteNumber { get; set; }

        // client details
        [FirestoreProperty("clientName")]
        public string ClientName { get; set; }

        // company details
        [FirestoreProperty("companyName")]
        public string CompanyName { get; set; }

        // contact details
        [FirestoreProperty("email")]
        public string Email { get; set; }

        // phone number
        [FirestoreProperty("phone")]
        public string Phone { get; set; }

        // address
        [FirestoreProperty("address")]
        public string Address { get; set; }

        // list of items
        [FirestoreProperty]
        public List<Dictionary<string, object>> Items { get; set; } = new List<Dictionary<string, object>>();

        // amounts
        [FirestoreProperty("amount")]
        public double? Amount { get; set; }

        // tax amount
        [FirestoreProperty("totalAmount")]
        public double? TotalAmount { get; set; }

        // status
        [FirestoreProperty("status")]
        public string Status { get; set; } = "Paid";

        // timestamps
        [FirestoreProperty("createdAt")]
        public Timestamp? CreatedAt { get; set; }

        // payment date
        [FirestoreProperty("paidAt")]
        public Timestamp? PaidAt { get; set; }

        // payment method
        [FirestoreProperty("month")]
        public string Month { get; set; }

        // payment year
        [FirestoreProperty("year")]
        public string Year { get; set; }
    }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//