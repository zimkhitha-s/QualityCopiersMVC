using Google.Cloud.Firestore;

namespace INSY7315_ElevateDigitalStudios_POE.Models
{
    [FirestoreData]
    public class QuotationItem
    {
        [FirestoreProperty("description")]
        public string description { get; set; }

        [FirestoreProperty("quantity")]
        public int quantity { get; set; }

        [FirestoreProperty("unitPrice")]
        public double unitPrice { get; set; }

        [FirestoreProperty("amount")]
        public double amount { get; set; }
    }
}
