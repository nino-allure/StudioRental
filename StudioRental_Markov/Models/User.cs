using System.ComponentModel.DataAnnotations;

namespace StudioRental_Markov.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string Role { get; set; } = "User";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
