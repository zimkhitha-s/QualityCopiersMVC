namespace INSY7315_ElevateDigitalStudios_POE.Models.Requests;

public class ChangePasswordRequest
{
    public string Email { get; set; }
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//