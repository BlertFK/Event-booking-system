using System.Collections.Generic;

namespace EventBookingSystem.Models
{
    public class DashboardVM
    {
        public int TotalEvents { get; set; }
        public int TotalBookings { get; set; }
        public List<Event> UpcomingEvents { get; set; }
    }
}
