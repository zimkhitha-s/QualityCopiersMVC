using System.ComponentModel.DataAnnotations;

namespace INSY7315_ElevateDigitalStudios_POE.Models.Dtos
{
    public class ClientUpdateDto
    {
        [Required]
        public string Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [RegularExpression(@"^[a-zA-Z\s'-]{1,100}$", ErrorMessage = "Full name contains invalid characters")]
        [StringLength(100, ErrorMessage = "Full name must be less than 100 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Company name is required")]
        [RegularExpression(@"^[a-zA-Z0-9\s,'-]{2,100}$", ErrorMessage = "Company name contains invalid characters")]
        [StringLength(100, ErrorMessage = "Company name must be less than 100 characters")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Phone number must be 10-15 digits and can start with +")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [RegularExpression(@"^[a-zA-Z0-9\s,'-]{0,200}$", ErrorMessage = "Address contains invalid characters")]
        [StringLength(200, ErrorMessage = "Address must be less than 200 characters")]
        public string Address { get; set; }
    }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//