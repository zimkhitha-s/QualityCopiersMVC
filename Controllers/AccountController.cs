using INSY7315_ElevateDigitalStudios_POE.Services;
using Microsoft.AspNetCore.Mvc;

namespace INSY7315_ElevateDigitalStudios_POE.Controllers
{
    public class AccountController : Controller
    {

        private readonly FirebaseAuthService _firebaseAuthService;

        public AccountController(FirebaseAuthService firebaseAuthService)
        {
            _firebaseAuthService = firebaseAuthService;
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
                // Successful login
                // You can store idToken or email in session
                HttpContext.Session.SetString("UserEmail", email);

                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Error = "Invalid login attempt.";
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }
    }
}
