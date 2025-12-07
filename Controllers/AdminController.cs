using EventBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventBookingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Data.ApplicationDbContext _context;

        public AdminController(
            ILogger<AdminController> logger,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            Data.ApplicationDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel();

            model.TotalUsers = _userManager.Users.Count();

            var adminRole = await _roleManager.FindByNameAsync("Admin");
            if (adminRole != null)
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                model.TotalAdmins = adminUsers.Count;
            }

            var userRole = await _roleManager.FindByNameAsync("User");
            if (userRole != null)
            {
                var regularUsers = await _userManager.GetUsersInRoleAsync("User");
                model.TotalRegularUsers = regularUsers.Count;
            }

            model.TotalEvents = await _context.Events.CountAsync();

            model.TotalBookings = await _context.Bookings.CountAsync();

            model.UpcomingEvents = await _context.Events
                .Where(e => e.Date > DateTime.Now)
                .OrderBy(e => e.Date)
                .ToListAsync();

            return View(model);
        }

        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Roles = string.Join(", ", roles),
                    EmailConfirmed = user.EmailConfirmed
                });
            }

            ViewBag.Users = userList;
            ViewBag.TotalUsers = users.Count;
            ViewBag.AdminCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count;
            ViewBag.RegularUserCount = (await _userManager.GetUsersInRoleAsync("User")).Count;

            return View();
        }

        public IActionResult ManageEvents()
        {
            return View();
        }
    }
}
