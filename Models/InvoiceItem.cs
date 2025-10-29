using Google.Cloud.Firestore;

namespace INSY7315_ElevateDigitalStudios_POE.Models
{
    [FirestoreData]
    public class InvoiceItem
    {
        [FirestoreProperty("description")]
        public string Description { get; set; }

        [FirestoreProperty("quantity")]
        public int Quantity { get; set; }

        [FirestoreProperty("unitPrice")]
        public double UnitPrice { get; set; }

        [FirestoreProperty("amount")]
        public double Amount { get; set; }
    }
}
