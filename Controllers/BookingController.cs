using System.Linq;
using System.Threading.Tasks;
using EventBookingSystem.Data;
using EventBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventBookingSystem.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public BookingController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Booking/Create/5  (5 = eventId)
        public async Task<IActionResult> Create(int id)
        {
            var ev = await _context.Events
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null) return NotFound();

            // compute available seats
            var bookedSeats = await _context.Bookings
                                .Where(b => b.EventId == id)
                                .SumAsync(b => (int?)b.SeatCount) ?? 0;

            int capacity = ev.GetType().GetProperty("Capacity") != null
                ? (int)ev.GetType().GetProperty("Capacity").GetValue(ev, null)
                : 0;

            var available = capacity - bookedSeats;

            ViewBag.Event = ev;
            ViewBag.AvailableSeats = available;

            return View(new Booking { EventId = id, SeatCount = 1 });
        }

        // POST: /Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking model)
        {
            if (!ModelState.IsValid) return View(model);

            var ev = await _context.Events
                        .FirstOrDefaultAsync(e => e.Id == model.EventId);

            if (ev == null) return NotFound();

            // compute capacity (must adapt if your Event uses a different property name)
            int capacity = ev.GetType().GetProperty("Capacity") != null
                ? (int)ev.GetType().GetProperty("Capacity").GetValue(ev, null)
                : 0;

            // atomic-ish check: use a transaction to avoid races
            using (var tx = await _context.Database.BeginTransactionAsync())
            {
                // recompute booked seats inside transaction
                var bookedSeats = await _context.Bookings
                                    .Where(b => b.EventId == model.EventId)
                                    .SumAsync(b => (int?)b.SeatCount) ?? 0;

                var available = capacity - bookedSeats;

                if (model.SeatCount <= 0)
                {
                    ModelState.AddModelError("", "Seat count must be at least 1.");
                    await tx.RollbackAsync();
                    ViewBag.Event = ev;
                    ViewBag.AvailableSeats = available;
                    return View(model);
                }

                if (model.SeatCount > available)
                {
                    ModelState.AddModelError("", $"Not enough seats available. Only {available} left.");
                    await tx.RollbackAsync();
                    ViewBag.Event = ev;
                    ViewBag.AvailableSeats = available;
                    return View(model);
                }

                // safe to create booking
                var user = await _userManager.GetUserAsync(User);
                model.UserId = user.Id;
                model.BookingDate = System.DateTime.UtcNow;
                model.Status = "Confirmed";

                _context.Bookings.Add(model);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }

            return RedirectToAction(nameof(MyBookings));
        }

        // GET: /Booking/MyBookings
        public async Task<IActionResult> MyBookings()
        {
            var user = await _userManager.GetUserAsync(User);
            var bookings = await _context.Bookings
                                .Include(b => b.Event)
                                .Where(b => b.UserId == user.Id)
                                .OrderByDescending(b => b.BookingDate)
                                .ToListAsync();

            return View(bookings);
        }

        // GET: /Booking/AdminBookings
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminBookings()
        {
            var all = await _context.Events
                        .Include(e => e) // ensure events loaded
                        .ToListAsync();

            // produce a view model: events + bookings per event
            var eventsWithBookings = await _context.Events
                                        .Include(e => e) // placeholder to ensure EF tracking
                                        .ToListAsync();

            var bookings = await _context.Bookings
                                .Include(b => b.Event)
                                .Include(b => b.User)
                                .OrderByDescending(b => b.BookingDate)
                                .ToListAsync();

            ViewBag.Bookings = bookings;
            ViewBag.Events = eventsWithBookings;

            return View();
        }

        // Optional: Cancel booking (only owner or admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            if (booking.UserId != user.Id && !isAdmin)
                return Forbid();

            booking.Status = "Cancelled";
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();

            if (isAdmin) return RedirectToAction(nameof(AdminBookings));
            return RedirectToAction(nameof(MyBookings));
        }
    }
}
