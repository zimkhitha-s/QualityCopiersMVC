using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INSY7315_ElevateDigitalStudios_POE.Models
{
    [FirestoreData]
    public class Employee
    {
        [FirestoreProperty("uid")]
        public string? Uid { get; set; }

        [Required(ErrorMessage = "ID Number is required")]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "ID Number must be exactly 13 digits")]
        [FirestoreProperty("idNumber")]
        public string IdNumber { get; set; }

        [FirestoreProperty("name")]
        [Required(ErrorMessage = "First name is required")]
        [RegularExpression(@"^[a-zA-Z\-']+$", ErrorMessage = "First name can only contain letters, hyphens, and apostrophes")]
        public string Name { get; set; }

        [FirestoreProperty("surname")]
        [Required(ErrorMessage = "Last name is required")]
        [RegularExpression(@"^[a-zA-Z\-']+$", ErrorMessage = "Last name can only contain letters, hyphens, and apostrophes")]
        public string Surname { get; set; }

        [FirestoreProperty("fullName")]
        public string? FullName { get; set; }

        [FirestoreProperty("email")]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [FirestoreProperty("phoneNumber")]
        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Phone number must be 10-15 digits and can start with +")]
        public string PhoneNumber { get; set; }

        [FirestoreProperty("role")]
        [RegularExpression(@"^(Employee|Manager)$", ErrorMessage = "Role must be Employee or Manager")]
        public string Role { get; set; } = "Employee";

        [NotMapped]
        [Required(ErrorMessage = "Password is required")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must be at least 8 characters long, include one uppercase letter, one lowercase letter, one number, and one special character."
        )]
        public string Password { get; set; }

        [FirestoreProperty("createdAt")]
        public object CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public DateTime CreatedAtDateTime { get; set; }
    }

}
