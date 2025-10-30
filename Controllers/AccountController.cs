using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    
    public class AccountController : Controller
    {

        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly FirebaseService _firebaseService;
        private readonly IWebHostEnvironment _env;

        public AccountController(FirebaseAuthService firebaseAuthService, FirebaseService firebaseService, IWebHostEnvironment env)
        {
            _firebaseAuthService = firebaseAuthService;
            _firebaseService = firebaseService; 
            _env = env;
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

                // 2️⃣ Fetch role from Firestore
                var role = await _firebaseService.GetUserRoleAsync(userId);

                if (role == null)
                {
                    ViewBag.Error = "User role not found.";
                    return View();
                }

                // 3️⃣ Store info in session
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserId", userId);
                HttpContext.Session.SetString("UserRole", role);

                // 4️⃣ Redirect based on role
                if (role == "admin")
                    return RedirectToAction("AdminDashboard", "Dashboard");
                else if (role == "manager")
                    return RedirectToAction("ManagerDashboard", "Dashboard");

                // fallback
                return View("AccessDenied");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying token or fetching role: {ex.Message}");
                ViewBag.Error = "An error occurred while logging in.";
                return View();
            }
        }


        [HttpGet]
        public IActionResult Profile()
        {
            return View();
        }

         [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not logged in or session expired." });

            var userData = await _firebaseService.GetManagerDataAsync(userId);
            return Json(userData);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] Dictionary<string, object> updatedData)
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not logged in or session expired." });

            await _firebaseService.UpdateManagerDataAsync(userId, updatedData);
            return Ok(new { message = "Profile updated successfully" });
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

            var fileName = Path.GetFileName(image.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            // Return the relative path to update the <img> src
            var relativePath = Url.Content("~/profileImages/" + fileName);
            return Content(relativePath);
        }
    }
}
