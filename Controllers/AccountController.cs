using INSY7315_ElevateDigitalStudios_POE.Models.Requests;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    
    public class AccountController : Controller
    {
        // Dependencies - firebase auth, firestore service, hosting env, configuration
        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly FirebaseService _firebaseService;
        private readonly IWebHostEnvironment _env;
        private readonly string _firebaseApiKey;
        private readonly IConfiguration _configuration;

        // Constructor to inject dependencies
        public AccountController(FirebaseAuthService firebaseAuthService, FirebaseService firebaseService, IWebHostEnvironment env, IConfiguration config, IConfiguration configuration)
        {
            _firebaseAuthService = firebaseAuthService;
            _firebaseService = firebaseService;
            _env = env;
            _firebaseApiKey = config["Firebase:ApiKey"];
            _configuration = configuration;
        }

        // login endpoints - GET
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // login endpoints - POST
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            // authenticate with firebase
            var idToken = await _firebaseAuthService.SignInWithEmailPasswordAsync(email, password);

            // handle invalid login
            if (string.IsNullOrEmpty(idToken))
            {
                ViewBag.Error = "Invalid login attempt.";
                return View();
            }

            try
            {
                // decode firebase using token to extract uid
                var firebaseAuth = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance;
                var decodedToken = await firebaseAuth.VerifyIdTokenAsync(idToken);
                var userId = decodedToken.Uid;
                var userDetails = new Dictionary<string, object>() ;

                // fetch user role from firestore
                string managerEmail = _configuration["AppSettings:ManagerEmail"];

                // check if the email matches manager email
                if (email == managerEmail)
                {
                    // fetch manager details
                    userDetails = await _firebaseService.GetManagerDataAsync(userId);
                }
                else
                {
                    // fetch regular user details - employees
                    userDetails = await _firebaseService.GetUserDetailsAsync(userId);
                }

                // validate the users details
                if (userDetails == null || !userDetails.ContainsKey("role"))
                {
                    ViewBag.Error = "User details not found.";
                    return View();
                }

                // extract the decrypted details
                string role = userDetails["role"]?.ToString() ?? "";
                string name = userDetails.ContainsKey("name") ? userDetails["name"]?.ToString() ?? "" : "";
                string surname = userDetails.ContainsKey("surname") ? userDetails["surname"]?.ToString() ?? "" : "";
                string fullname = $"{name} {surname}";

                // store the information in the session
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserId", userId);
                HttpContext.Session.SetString("UserRole", role);
                HttpContext.Session.SetString("FullName", fullname);
                HttpContext.Session.SetString("UserName", name);
                HttpContext.Session.SetString("UserSurname", surname);

                // setting the default fallback redirect
                return RedirectToAction("Index", "Dashboard");

            }
            catch (Exception ex)
            {
                // log the exception for debugging
                Console.WriteLine($"Error verifying token or fetching role: {ex.Message}");
                ViewBag.Error = "An error occurred while logging in.";
                return View();
            }
        }

        // logout endpoint
        [HttpGet]
        public IActionResult Profile()
        {
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            return View();
        }

        // profile endpoints - get profile details
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not logged in or session expired." });

            var userData = await _firebaseService.GetUserDetailsAsync(userId);
            return Json(userData);
        }

        // profile endpoints - update profile details
        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest updatedData)
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not logged in or session expired." });

            if (updatedData == null)
                return BadRequest(new { message = "Invalid request body." });

            var data = new Dictionary<string, object>
            {
                { "firstName", updatedData.FullName },
                { "role", updatedData.Role },
                { "surname", updatedData.Surname },
                { "mobile", updatedData.Mobile },
                { "email", updatedData.Email },
                { "language", updatedData.Language }
            };

            var (success, message) = await _firebaseService.UpdateManagerDataAsync(userId, data);

            if (!success)
                return StatusCode(500, new { message });

            return Ok(new { message });
        }

        // Upload Profile Image
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No image selected.");

            // Save path inside wwwroot/uploads
            var uploadsFolder = Path.Combine(_env.WebRootPath, "profileImages");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            // Return the relative path to update the <img> src
            var relativePath = Url.Content("~/profileImages/" + fileName);
            return Content(relativePath);
        }
        
        //Change Password
        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeRequest request)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("User not logged in.");

            if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
                return BadRequest("Missing password fields.");

            try
            {
                // Re-authenticate user with current password using Firebase REST API
                var client = new HttpClient();
                var apiKey = _firebaseApiKey; 

                var reauthPayload = new
                {
                    email = userEmail,
                    password = request.CurrentPassword,
                    returnSecureToken = true
                };

                var reauthResponse = await client.PostAsJsonAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}",
                    reauthPayload);

                if (!reauthResponse.IsSuccessStatusCode)
                {
                    return BadRequest("Current password is incorrect.");
                }

                var reauthData = await reauthResponse.Content.ReadFromJsonAsync<FirebaseSignInResponse>();

                // Update password via Firebase REST API
                var updatePayload = new
                {
                    idToken = reauthData.idToken,
                    password = request.NewPassword,
                    returnSecureToken = false
                };

                var updateResponse = await client.PostAsJsonAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={apiKey}",
                    updatePayload);

                if (!updateResponse.IsSuccessStatusCode)
                {
                    var errorDetails = await updateResponse.Content.ReadAsStringAsync();
                    return BadRequest($"Failed to update password. Details: {errorDetails}");
                }

                return Ok("Password updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DTOs
        public class PasswordChangeRequest
        {
            public string CurrentPassword { get; set; }
            public string NewPassword { get; set; }
        }

        public class FirebaseSignInResponse
        {
            public string idToken { get; set; }
            public string email { get; set; }
            public string refreshToken { get; set; }
            public string expiresIn { get; set; }
            public string localId { get; set; }
        }
   }
}
