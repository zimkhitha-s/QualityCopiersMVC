using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace INSY7315_ElevateDigitalStudios_POE.Models
{
    // payment model
    [FirestoreData]
    public class Payment
    {

        [FirestoreProperty("clientName")]
        public string ClientName { get; set; }

        [FirestoreProperty("email")]
        public string Email { get; set; }

        [FirestoreProperty("phone")]
        public string Phone { get; set; }


        [FirestoreProperty("totalAmount")]

        [FirestoreProperty("status")]



        // payment method
        [FirestoreProperty("month")]

        // payment year
        [FirestoreProperty("year")]
    }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//