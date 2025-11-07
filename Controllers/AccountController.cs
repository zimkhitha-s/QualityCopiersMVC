using Google.Cloud.SecretManager.V1;
using INSY7315_ElevateDigitalStudios_POE.Helper;
using INSY7315_ElevateDigitalStudios_POE.Models.Dtos;
using INSY7315_ElevateDigitalStudios_POE.Models.Requests;
using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    
    public class AccountController : Controller
    {
        // Dependencies - firebase auth, firestore service, hosting env, configuration
        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly FirebaseService _firebaseService;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        // Secrets
        private readonly string _firebaseApiKey;
        private readonly string _managerEmail;

        // Constructor to inject dependencies
        public AccountController(
            FirebaseAuthService firebaseAuthService,
            FirebaseService firebaseService,
            IWebHostEnvironment env,
            IConfiguration configuration)
        {
            _firebaseAuthService = firebaseAuthService;
            _firebaseService = firebaseService;
            _env = env;
            _configuration = configuration;

            // Fetch Google Cloud project ID
            var projectId = Environment.GetEnvironmentVariable("GCP_PROJECT_ID");

            // Retrieve secrets via centralized helper
            _firebaseApiKey = SecretManagerHelper.GetSecret(projectId, "firebase-web-API-key");
            _managerEmail = SecretManagerHelper.GetSecret(projectId, "manager-email");
        }

        // login endpoints - GET

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        // logout endpoint
        [HttpGet]
        public IActionResult Logout()
        {
            // Clear all session data
            HttpContext.Session.Clear();
            HttpContext.Session.Remove("UserEmail");
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("UserRole");
            HttpContext.Session.Remove("FullName");
            HttpContext.Session.Remove("UserName");
            HttpContext.Session.Remove("UserSurname");

            // End the session completely
            HttpContext.Session.CommitAsync();

            // Redirect user to Login page
            return RedirectToAction("Login", "Account");
        }

        // login endpoints - POST

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string email, string password)
        {
            // Authenticate with Firebase
            var idToken = await _firebaseAuthService.SignInWithEmailPasswordAsync(email, password);

            // Handle invalid login
            if (string.IsNullOrEmpty(idToken))
            {
                ViewBag.Error = "Invalid login attempt.";
                return View();
            }

            try
            {
                // Decode Firebase token to extract UID
                var firebaseAuth = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance;
                var decodedToken = await firebaseAuth.VerifyIdTokenAsync(idToken);
                var userId = decodedToken.Uid;
                Dictionary<string, object> userDetails;

                // Lazy-load Google Cloud project ID and manager email
                var projectId = Environment.GetEnvironmentVariable("GCP_PROJECT_ID");
                if (string.IsNullOrEmpty(projectId))
                {
                    ViewBag.Error = "Server configuration error: GCP_PROJECT_ID not set.";
                    return View();
                }

                string managerEmail;
                try
                {
                    managerEmail = SecretManagerHelper.GetSecret(projectId, "manager-email");
                }
                catch (Grpc.Core.RpcException rpcEx)
                {
                    Console.WriteLine($"SecretManager error: {rpcEx.Message}");
                    ViewBag.Error = "Server error retrieving manager email. Please try again later.";
                    return View();
                }

                // Determine if user is manager or regular employee
                if (email.Equals(managerEmail, StringComparison.OrdinalIgnoreCase))
                {
                    // Ensure service account has Firestore access for manager
                    userDetails = await _firebaseService.GetManagerDataAsync(userId);
                }
                else
                {
                    userDetails = await _firebaseService.GetUserDetailsAsync(userId);
                }

                // Validate user details
                if (userDetails == null || !userDetails.ContainsKey("role"))
                {
                    ViewBag.Error = "User details not found.";
                    return View();
                }

                // Extract user information
                string role = userDetails["role"]?.ToString() ?? "";
                string name = userDetails.ContainsKey("name") ? userDetails["name"]?.ToString() ?? "" : "";
                string surname = userDetails.ContainsKey("surname") ? userDetails["surname"]?.ToString() ?? "" : "";
                string fullname = $"{name} {surname}";

                // Store information in session
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserId", userId);
                HttpContext.Session.SetString("UserRole", role);
                HttpContext.Session.SetString("FullName", fullname);
                HttpContext.Session.SetString("UserName", name);
                HttpContext.Session.SetString("UserSurname", surname);

                // ✅ Create the authentication identity & sign in user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Role, role),
                    new Claim(ClaimTypes.Name, fullname)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Redirect to dashboard
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Grpc.Core.RpcException rpcEx)
            {
                // Handles Firestore or Secret Manager permission errors
                Console.WriteLine($"GCP RPC Error: {rpcEx.Status.Detail}");
                ViewBag.Error = "Server error accessing data. Please check your permissions.";
                return View();
            }
            catch (Exception ex)
            {
                // Log general errors
                Console.WriteLine($"Error verifying token or fetching role: {ex.Message}");
                ViewBag.Error = "An error occurred while logging in.";
                return View();
            }
        }


        // get endpoint for forgot password

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // post endpoint for forgot password
        
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            // validate email input
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Please enter your email address.";
                return View();
            }

            try
            {
                // send password reset email using Firebase REST API
                var client = new HttpClient();
                var apiKey = _firebaseApiKey;

                // prepare the request payload
                var requestPayload = new
                {
                    requestType = "PASSWORD_RESET",
                    email = email
                };

                // make the POST request to Firebase
                var response = await client.PostAsJsonAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={apiKey}",
                    requestPayload);

                if (!response.IsSuccessStatusCode)
                {
                    // handle error response
                    var error = await response.Content.ReadAsStringAsync();
                    ViewBag.Error = $"Failed to send reset email. Details: {error}";
                    return View();
                }

                // success message
                ViewBag.Message = "A password reset email has been sent. Please check your inbox.";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"An error occurred: {ex.Message}";
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

            var (success, message) = await _firebaseService.UpdateUserDetailsAsync(userId, data);

            if (!success)
                return StatusCode(500, new { message });

            return Ok(new { message });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var email = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { error = "User email not found. Please log in again." });
            }

            if (request == null ||
                string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new { error = "All fields are required." });
            }

            var (success, errorMessage) = await _firebaseAuthService.ChangePasswordAsync(
                email,
                request.CurrentPassword,
                request.NewPassword
            );

            if (!success)
                return BadRequest(new { error = errorMessage ?? "Failed to change password." });

            return Ok(new { message = "Password changed successfully." });
        }

        // upload profile image
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No image selected.");

            // save path inside 
            var uploadsFolder = Path.Combine(_env.WebRootPath, "profileImages");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            // return the relative path to update the image
            var relativePath = Url.Content("~/profileImages/" + fileName);
            return Content(relativePath);
        }
        
        // change password
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
                // re-authenticate user with current password using Firebase REST API
                var client = new HttpClient();
                var apiKey = _firebaseApiKey;

                // re-authentication payload
                var reauthPayload = new
                {
                    email = userEmail,
                    password = request.CurrentPassword,
                    returnSecureToken = true
                };

                // make the re-authentication request
                var reauthResponse = await client.PostAsJsonAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}",
                    reauthPayload);

                if (!reauthResponse.IsSuccessStatusCode)
                {
                    return BadRequest("Current password is incorrect.");
                }

                var reauthData = await reauthResponse.Content.ReadFromJsonAsync<FirebaseSignInResponse>();

                // update password via Firebase REST API
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
   }
}
//-------------------------------------------------------------------------------------------End Of File--------------------------------------------------------------------//