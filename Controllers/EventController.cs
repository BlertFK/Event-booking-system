using EventBookingSystem.Data;
using EventBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventBookingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EventController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<EventController> _logger;
        private const int PageSize = 10;

        public EventController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            ILogger<EventController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: Event
        public async Task<IActionResult> Index(string searchString, string sortOrder, int page = 1)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["CurrentSort"] = sortOrder;

            var events = from e in _context.Events
                        select e;

            // Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                events = events.Where(e => e.Name.Contains(searchString)
                    || e.Location.Contains(searchString)
                    || e.Description.Contains(searchString));
            }

            // Sorting
            switch (sortOrder)
            {
                case "name_desc":
                    events = events.OrderByDescending(e => e.Name);
                    break;
                case "Date":
                    events = events.OrderBy(e => e.EventDate);
                    break;
                case "date_desc":
                    events = events.OrderByDescending(e => e.EventDate);
                    break;
                default:
                    events = events.OrderBy(e => e.Name);
                    break;
            }

            // Pagination
            var totalCount = await events.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            var pagedEvents = await events
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = PageSize;

            // Statistics
            ViewBag.TotalEvents = await _context.Events.CountAsync();
            ViewBag.UpcomingEvents = await _context.Events
                .Where(e => e.EventDate >= DateTime.Today && e.IsActive)
                .CountAsync();
            ViewBag.TotalBookings = 0; // Will be updated when booking system is implemented

            return View(pagedEvents);
        }

        // GET: Event/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // GET: Event/Create
        public IActionResult Create()
        {
            return View(new EventViewModel());
        }

        // POST: Event/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventViewModel model)
        {
            if (ModelState.IsValid)
            {
                var @event = new Event
                {
                    Name = model.Name,
                    Description = model.Description,
                    EventDate = model.EventDate,
                    EventTime = model.EventTime,
                    Location = model.Location,
                    Capacity = model.Capacity,
                    Price = model.Price,
                    IsActive = model.IsActive,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name
                };

                // Handle image upload
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    @event.ImagePath = await SaveImageAsync(model.ImageFile);
                }

                _context.Add(@event);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Event {EventId} created by {User}", @event.Id, User.Identity?.Name);
                TempData["SuccessMessage"] = "Event created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Event/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }

            var model = new EventViewModel
            {
                Id = @event.Id,
                Name = @event.Name,
                Description = @event.Description,
                EventDate = @event.EventDate,
                EventTime = @event.EventTime,
                Location = @event.Location,
                Capacity = @event.Capacity,
                Price = @event.Price,
                IsActive = @event.IsActive,
                CurrentImagePath = @event.ImagePath
            };

            return View(model);
        }

        // POST: Event/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var @event = await _context.Events.FindAsync(id);
                    if (@event == null)
                    {
                        return NotFound();
                    }

                    @event.Name = model.Name;
                    @event.Description = model.Description;
                    @event.EventDate = model.EventDate;
                    @event.EventTime = model.EventTime;
                    @event.Location = model.Location;
                    @event.Capacity = model.Capacity;
                    @event.Price = model.Price;
                    @event.IsActive = model.IsActive;
                    @event.UpdatedDate = DateTime.UtcNow;

                    // Handle image upload
                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(@event.ImagePath))
                        {
                            DeleteImage(@event.ImagePath);
                        }
                        @event.ImagePath = await SaveImageAsync(model.ImageFile);
                    }

                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Event {EventId} updated by {User}", @event.Id, User.Identity?.Name);
                    TempData["SuccessMessage"] = "Event updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            model.CurrentImagePath = await _context.Events
                .Where(e => e.Id == id)
                .Select(e => e.ImagePath)
                .FirstOrDefaultAsync();

            return View(model);
        }

        // GET: Event/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // POST: Event/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                // Delete associated image if exists
                if (!string.IsNullOrEmpty(@event.ImagePath))
                {
                    DeleteImage(@event.ImagePath);
                }

                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Event {EventId} deleted by {User}", id, User.Identity?.Name);
                TempData["SuccessMessage"] = "Event deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new InvalidOperationException("Invalid file type. Only image files are allowed.");
            }

            // Validate file size (max 5MB)
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException("File size exceeds 5MB limit.");
            }

            // Create uploads directory if it doesn't exist
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "events");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            // Return relative path
            return $"/uploads/events/{uniqueFileName}";
        }

        private void DeleteImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return;

            try
            {
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete image: {ImagePath}", imagePath);
            }
        }
    }
}

