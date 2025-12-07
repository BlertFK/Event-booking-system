using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventBookingSystem.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        public string UserId { get; set; }        // FK to AspNetUsers (Identity)

        [Required]
        public int EventId { get; set; }          // FK to Event

        [Required]
        [Range(1, 1000, ErrorMessage = "Seat count must be at least 1.")]
        public int SeatCount { get; set; } = 1;

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        // optional booking status (Confirmed, Cancelled, Pending)
        [MaxLength(20)]
        public string Status { get; set; } = "Confirmed";

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual Microsoft.AspNetCore.Identity.IdentityUser User { get; set; }

        [ForeignKey("EventId")]
        public virtual Event Event { get; set; }
    }
}
