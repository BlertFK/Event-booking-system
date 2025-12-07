using System.ComponentModel.DataAnnotations;

namespace EventBookingSystem.Models
{
    public class EventViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Event name is required")]
        [StringLength(200, ErrorMessage = "Event name cannot exceed 200 characters")]
        [Display(Name = "Event Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Event Date")]
        public DateTime EventDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Event time is required")]
        [DataType(DataType.Time)]
        [Display(Name = "Event Time")]
        public TimeSpan EventTime { get; set; } = TimeSpan.FromHours(18);

        [Required(ErrorMessage = "Location is required")]
        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1")]
        [Display(Name = "Capacity")]
        public int Capacity { get; set; } = 100;

        [Range(0, double.MaxValue, ErrorMessage = "Price must be 0 or greater")]
        [Display(Name = "Price")]
        public decimal Price { get; set; } = 0;

        [Display(Name = "Event Image")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Current Image")]
        public string? CurrentImagePath { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
    }
}

