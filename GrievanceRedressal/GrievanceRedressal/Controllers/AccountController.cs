using Microsoft.AspNetCore.Mvc;
using GrievanceRedressal.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Cryptography;
using System.Text;

namespace GrievanceRedressal.Controllers
{
    public class AccountController : Controller
    {
        private readonly GrievanceRepository _repo;

        public AccountController(GrievanceRepository repo)
        {
            _repo = repo;
        }

        // -------------------------------------------------
        // Login
        // -------------------------------------------------
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
            var user = await _repo.LoginAsync(email, hash);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim(ClaimTypes.Role, user.Role ?? "User")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                              new ClaimsPrincipal(identity));

                return user.Role == "Admin"
                    ? RedirectToAction("Index", "Admin")
                    : RedirectToAction("Dashboard", "User");
            }

            ViewBag.Error = "Invalid credentials";
            return View();
        }

        // -------------------------------------------------
        // Register
        // -------------------------------------------------
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string name, string email, string password, string role = "User")
        {
            var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
            try
            {
                await _repo.RegisterAsync(name, email, hash, role, null);
                return RedirectToAction("Login");
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                ViewBag.RegisterError = "Email / Username already exists. Please use another one or login.";
                return View("Login");
            }
            catch (Exception ex)
            {
                ViewBag.RegisterError = "Something went wrong. Technical details: " + ex.Message;
                return View("Login");
            }
        }

        // -------------------------------------------------
        // Logout
        // -------------------------------------------------
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // -------------------------------------------------
        // Forgot Password – two‑step flow
        // -------------------------------------------------
        public IActionResult ForgotPassword()
        {
            ViewBag.IsReset = false;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email, string password = null, string confirmPassword = null)
        {
            // Clean inputs
            var cleanEmail = email?.Trim();

            // STEP 1 – only e‑mail submitted
            if (string.IsNullOrWhiteSpace(password))
            {
                var user = await _repo.GetUserByEmailAsync(cleanEmail);
                if (user == null)
                {
                    ViewBag.Message = $"Email not found: '{cleanEmail}'. Please try another.";
                    ViewBag.IsReset = false;
                    return View();
                }

                // Email exists – show step 2
                ViewBag.IsReset = true;
                ViewBag.Email = cleanEmail;
                ViewBag.Message = "User found! Enter your new password.";
                return View();
            }

            // STEP 2 – resetting password
            if (password != confirmPassword)
            {
                ViewBag.Message = "Passwords do not match. Try again.";
                ViewBag.IsReset = true;
                ViewBag.Email = cleanEmail;
                return View();
            }

            // Final lookup & update
            var existingUser = await _repo.GetUserByEmailAsync(cleanEmail);
            if (existingUser != null)
            {
                var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
                await _repo.UpdatePasswordAsync(existingUser.UserId, hash);
                ViewBag.Message = "Password reset successfully! Go log in.";
                ViewBag.IsReset = false;
                return View();
            }
            else
            {
                ViewBag.Message = $"Could not verify email '{cleanEmail}' anymore. Try starting over.";
                ViewBag.IsReset = false;
                return View();
            }
        }
    }
}
