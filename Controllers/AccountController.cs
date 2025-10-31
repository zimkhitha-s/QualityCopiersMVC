using INSY7315_ElevateDigitalStudios_POE.Models.Requests;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    
    public class AccountController : Controller
    {

        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly FirebaseService _firebaseService;
        private readonly IWebHostEnvironment _env;
        private readonly string _firebaseApiKey;

        public AccountController(FirebaseAuthService firebaseAuthService, FirebaseService firebaseService, IWebHostEnvironment env, IConfiguration config)
        {
            _firebaseAuthService = firebaseAuthService;
            _firebaseService = firebaseService;
            _env = env;
            _firebaseApiKey = config["Firebase:ApiKey"];
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var idToken = await _firebaseAuthService.SignInWithEmailPasswordAsync(email, password);

            if (string.IsNullOrEmpty(idToken))
            {
                ViewBag.Error = "Invalid login attempt.";
                return View();
            }

            try
            {
                // 1️⃣ Decode Firebase ID token to extract UID
                var firebaseAuth = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance;
                var decodedToken = await firebaseAuth.VerifyIdTokenAsync(idToken);
                var userId = decodedToken.Uid;
                var userDetails = new Dictionary<string, object>() ;

                // 2️⃣ Fetch user details (decrypted) from Firestore
                if(email == "craig.diedericks@gmail.com")
                {
                    userDetails = await _firebaseService.GetManagerDataAsync(userId);
                }
                else
                {
                    userDetails = await _firebaseService.GetUserDetailsAsync(userId);
                }
                

                if (userDetails == null || !userDetails.ContainsKey("role"))
                {
                    ViewBag.Error = "User details not found.";
                    return View();
                }

                // 3️⃣ Extract decrypted details
                string role = userDetails["role"]?.ToString() ?? "";
                string name = userDetails.ContainsKey("name") ? userDetails["name"]?.ToString() ?? "" : "";
                string surname = userDetails.ContainsKey("surname") ? userDetails["surname"]?.ToString() ?? "" : "";
                string fullname = $"{name} {surname}";

                // 4️⃣ Store info in session
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserId", userId);
                HttpContext.Session.SetString("UserRole", role);
                HttpContext.Session.SetString("FullName", fullname);
                HttpContext.Session.SetString("UserName", name);
                HttpContext.Session.SetString("UserSurname", surname);

                /*// 5️⃣ Redirect based on role (optional)
                if (role.Equals("admin", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("AdminDashboard", "Dashboard");
                else if (role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("ManagerDashboard", "Dashboard");*/

                // Default fallback redirect
                return RedirectToAction("Index", "Dashboard");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying token or fetching role: {ex.Message}");
                ViewBag.Error = "An error occurred while logging in.";
                return View();
            }
        }

        // ======= FORGOT PASSWORD =======
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Please enter your email address.";
                return View();
            }

            try
            {
                var client = new HttpClient();
                var apiKey = _firebaseApiKey;

                var requestPayload = new
                {
                    requestType = "PASSWORD_RESET",
                    email = email
                };

                var response = await client.PostAsJsonAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={apiKey}",
                    requestPayload);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ViewBag.Error = $"Failed to send reset email. Details: {error}";
                    return View();
                }

                ViewBag.Message = "A password reset email has been sent. Please check your inbox.";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"An error occurred: {ex.Message}";
                return View();
            }
        }


        [HttpGet]
        public IActionResult Profile()
        {
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            return View();
        }

         [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not logged in or session expired." });

            var userData = await _firebaseService.GetUserDetailsAsync(userId);//change this function name to GetUserDetails
            return Json(userData);
        }

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

            var (success, message) = await _firebaseService.UpdateManagerDataAsync(userId, data); //change this function name to UpdateUserDetails

            if (!success)
                return StatusCode(500, new { message });

            return Ok(new { message });
        }
        
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
                // 1️⃣ Re-authenticate user with current password using Firebase REST API
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

                // 2️⃣ Update password via Firebase REST API
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
