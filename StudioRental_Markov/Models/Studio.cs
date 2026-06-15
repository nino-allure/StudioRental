using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // Хранение бинарных данных изображения
        [MaxLength(5 * 1024 * 1024)] // 5MB максимум
        public byte[]? ImageData { get; set; }

        [MaxLength(50)]
        public string? ImageContentType { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int OwnerId { get; set; }

        [ForeignKey(nameof(OwnerId))]
        public virtual User? Owner { get; set; }

        // Вспомогательный метод для получения base64 изображения
        [NotMapped]
        public string? ImageBase64 => ImageData != null
            ? $"data:{ImageContentType};base64,{Convert.ToBase64String(ImageData)}"
            : ImageUrl;
    }
}