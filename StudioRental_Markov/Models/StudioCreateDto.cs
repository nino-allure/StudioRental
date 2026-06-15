using System.ComponentModel.DataAnnotations;

namespace StudioRental_Markov.Models
{
    public class StudioCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required]
        public string Address { get; set; } = string.Empty;
        [Required]
        public decimal PricePerHour { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? Image { get; set; }
    }
}
