using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INSY7315_ElevateDigitalStudios_POE.Models
{
    [FirestoreData]
    public class Client
    {
        [FirestoreProperty]
        public string? id { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [FirestoreProperty]
        public string name { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [FirestoreProperty]
        public string surname { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [FirestoreProperty]
        public string email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [FirestoreProperty]
        public string phoneNumber { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [FirestoreProperty]
        public string address { get; set; }

        [Required(ErrorMessage = "Company is required")]
        [FirestoreProperty]
        public string companyName { get; set; }

        [FirestoreProperty]
        public object createdAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public DateTime createdAtDateTime { get; set; }
    }
}
