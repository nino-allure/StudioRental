using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioRental_Markov.Models
{
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string LogLevel { get; set; } = string.Empty; 

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Message { get; set; }

        [MaxLength(2000)]
        public string? Details { get; set; }

        public int? UserId { get; set; }

        [MaxLength(100)]
        public string? UserEmail { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(200)]
        public string? RequestPath { get; set; }

        [MaxLength(20)]
        public string? RequestMethod { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }
    }
}