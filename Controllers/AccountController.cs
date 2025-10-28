using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class AccountController : Controller
    {

        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly FirebaseService _firebaseService;

        public AccountController(FirebaseAuthService firebaseAuthService, FirebaseService firebaseService)
        {
            _firebaseAuthService = firebaseAuthService;
            _firebaseService = firebaseService; 
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
            

            if (!string.IsNullOrEmpty(idToken))
            {
                // Decode Firebase ID token to extract the UID
                var firebaseAuth = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance;
                var decodedToken = await firebaseAuth.VerifyIdTokenAsync(idToken);
                var userId = decodedToken.Uid; // 🔹 Firebase User UID

                // Store useful info in session
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserId", userId); // 🔹 Store UID here

                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Error = "Invalid login attempt.";
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        [HttpGet("get")]
        public async Task<IActionResult> GetProfile()
        {
            // Get the current user's UID from session
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not logged in or session expired." });
            }

            var userData = await _firebaseService.GetManagerDataAsync(userId);
            return Json(userData);
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] Dictionary<string, object> updatedData)
        {
            // Get the current user's UID from session
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not logged in or session expired." });
            }

            await _firebaseService.UpdateManagerDataAsync(userId, updatedData);
            return Ok(new { message = "Profile updated successfully" });
        }
    }
}
