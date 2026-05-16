using Microsoft.AspNetCore.Mvc;
using GrievanceRedressal.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace GrievanceRedressal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly GrievanceRepository _repo;

        public AdminController(GrievanceRepository repo)
        {
            _repo = repo;
        }

        public async Task<IActionResult> Index()
        {
            var counts = await _repo.GetDashboardCountsAsync();
            var grievances = await _repo.GetAllGrievancesAsync();

            // Category Counts (Mapping: 1=Tech, 2=Hostel, 3=Academic, 4=Other)
            ViewBag.TechCount = grievances.Count(g => g.CategoryId == 1);
            ViewBag.HostelCount = grievances.Count(g => g.CategoryId == 2);
            ViewBag.AcademicCount = grievances.Count(g => g.CategoryId == 3);
            ViewBag.OtherCount = grievances.Count(g => g.CategoryId == 4);

            // Status Counts (Mapping: 1=Open, 2=In Progress, 3=Resolved)
            // Note: counts also has some, but let's be consistent with status chart labels
            ViewBag.OpenCount = grievances.Count(g => g.StatusId == 1);
            ViewBag.InProgressCount = grievances.Count(g => g.StatusId == 2);
            ViewBag.ResolvedCount = grievances.Count(g => g.StatusId == 3);

            // Added: Trend Predictions 
            ViewBag.Trends = await _repo.GetTrendPredictionsAsync();
            
            // Added: Recent Grievances for Dashboard Table
            ViewBag.RecentGrievances = grievances.OrderByDescending(g => g.CreatedAt).Take(6);

            return View(counts);
        }

        public async Task<IActionResult> ManageGrievances(int? statusId)
        {
            var grievances = await _repo.GetAllGrievancesAsync();
            
            // To make the static front-end counters dynamic as well
            ViewBag.NewCount = grievances.Count(g => g.StatusId == 1);
            ViewBag.InProgressCount = grievances.Count(g => g.StatusId == 2);
            ViewBag.ResolvedCount = grievances.Count(g => g.StatusId == 3);

            if (statusId.HasValue)
            {
                grievances = grievances.Where(g => g.StatusId == statusId.Value);
                ViewBag.CurrentFilter = statusId.Value;
            }
            
            return View(grievances);
        }


        public async Task<IActionResult> GenerateDetailedReport()
        {
            var grievances = await _repo.GetAllGrievancesAsync();
            
            // Grouping by Month for the report
            var monthlyReport = grievances
                .GroupBy(g => new { g.CreatedAt.Year, g.CreatedAt.Month })
                .Select(g => new {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", System.Globalization.CultureInfo.CurrentCulture),
                    Total = g.Count(),
                    Resolved = g.Count(x => x.StatusId == 3),
                    InProgress = g.Count(x => x.StatusId == 2),
                    Open = g.Count(x => x.StatusId == 1)
                })
                .OrderByDescending(x => x.Month)
                .ToList();

            ViewBag.MonthlyData = monthlyReport;
            return View(grievances); // Pass all grievances for detail view
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int grievanceId, int newStatus, string actionRemark = "Updated by Admin")
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int adminId))
            {
                // Status mapping: 1=Open, 2=In Progress, 3=Resolved
                await _repo.UpdateGrievanceStatusAsync(grievanceId, newStatus, actionRemark, adminId);
            }
            return RedirectToAction("ManageGrievances");
        }

        public async Task<IActionResult> ChatbotLogs()
        {
            var logs = await _repo.GetAllChatbotLogsAsync();
            return View(logs);
        }

    }
}