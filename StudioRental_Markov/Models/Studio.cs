using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace StudioRental_Markov.Models
{
    public class Studio
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(300)]
        public string Address { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal PricePerHour { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Внешний ключ
        public int OwnerId { get; set; }

        // Навигационное свойство
        [ForeignKey(nameof(OwnerId))]
        public virtual User? Owner { get; set; }
    }
}
