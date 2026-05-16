using Microsoft.AspNetCore.Mvc;
using GrievanceRedressal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace GrievanceRedressal.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly GrievanceRepository _repo;

        public UserController(GrievanceRepository repo)
        {
            _repo = repo;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var grievances = await _repo.GetUserGrievancesAsync(userId);
                ViewBag.Grievances = grievances.OrderByDescending(g => g.CreatedAt).Take(5);
                
                // Dynamic Counts for Dashboard
                ViewBag.TotalCount = grievances.Count();
                ViewBag.OpenCount = grievances.Count(g => g.StatusId == 1);
                ViewBag.InProgressCount = grievances.Count(g => g.StatusId == 2);
                ViewBag.ResolvedCount = grievances.Count(g => g.StatusId == 3);
            }
            return View();
        }

        public IActionResult RaiseGrievance()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitGrievance(string complaintSubject, string detailedDescription, IFormFile? attachment, int categoryId = 1, int priorityRange = 2, int departmentId = 1)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                string? attachmentPath = null;
                if (attachment != null && attachment.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(attachment.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await attachment.CopyToAsync(fileStream);
                    }
                    attachmentPath = "/uploads/" + uniqueFileName;
                }

                await _repo.CreateGrievanceAsync(userId, complaintSubject, detailedDescription, categoryId, priorityRange, departmentId, attachmentPath);
            }
            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> MyGrievances(int? statusId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var grievances = await _repo.GetUserGrievancesAsync(userId);
                if (statusId.HasValue)
                {
                    grievances = grievances.Where(g => g.StatusId == statusId.Value);
                    ViewBag.CurrentStatusName = statusId == 1 ? "Open" : statusId == 2 ? "In Progress" : "Resolved";
                }
                return View(grievances);
            }
            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> GrievanceDetails(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var grievances = await _repo.GetUserGrievancesAsync(userId);
                var ticket = grievances.FirstOrDefault(g => g.GrievanceId == id);
                if (ticket != null)
                {
                    return View(ticket);
                }
            }
            return RedirectToAction("MyGrievances");
        }

        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var grievances = await _repo.GetUserGrievancesAsync(userId);
                ViewBag.Grievances = grievances;
                
                // Fetch user info (using email as we don't have GetUserById yet, or I'll use the email from claims)
                var email = User.FindFirstValue(ClaimTypes.Email);
                if (!string.IsNullOrEmpty(email))
                {
                    var user = await _repo.GetUserByEmailAsync(email);
                    return View(user);
                }
            }
            return View();
        }

        public async Task<IActionResult> Settings()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrEmpty(email))
            {
                var user = await _repo.GetUserByEmailAsync(email);
                return View(user);
            }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSettings(string name, string? currentPassword, string? newPassword)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (int.TryParse(userIdStr, out int userId))
            {
                if (!string.IsNullOrEmpty(name))
                {
                    await _repo.UpdateUserNameAsync(userId, name);
                    
                    // Re-issue cookie with updated name
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Name, name),
                        new Claim(ClaimTypes.Email, email ?? ""),
                        new Claim(ClaimTypes.Role, role ?? "User")
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                    
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                }

                if (!string.IsNullOrEmpty(newPassword) && !string.IsNullOrEmpty(currentPassword))
                {
                    // Update password directly
                    await _repo.UpdatePasswordAsync(userId, newPassword); 
                    TempData["SuccessMessage"] = "Password and Profile updated successfully!";
                }
            }
            return RedirectToAction("Settings");
        }
    }
}
