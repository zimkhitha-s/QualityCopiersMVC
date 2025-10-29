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
        [RegularExpression(@"^[a-zA-Z\s'-]{1,50}$", ErrorMessage = "First name contains invalid characters")]
        [FirestoreProperty]
        public string name { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [RegularExpression(@"^[a-zA-Z\s'-]{1,50}$", ErrorMessage = "Last name contains invalid characters")]
        [FirestoreProperty]
        public string surname { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [FirestoreProperty]
        public string email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Phone number must be 10-15 digits and can start with +")]
        [FirestoreProperty]
        public string phoneNumber { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [RegularExpression(@"^[a-zA-Z0-9\s,'-]{3,100}$", ErrorMessage = "Address contains invalid characters")]
        [FirestoreProperty]
        public string address { get; set; }

        [Required(ErrorMessage = "Company is required")]
        [RegularExpression(@"^[a-zA-Z0-9\s,'-]{2,100}$", ErrorMessage = "Company name contains invalid characters")]
        [FirestoreProperty]
        public string companyName { get; set; }

        [FirestoreProperty]
        public object createdAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public DateTime createdAtDateTime { get; set; }

    }
}
