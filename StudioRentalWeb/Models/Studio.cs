using System.ComponentModel.DataAnnotations;

namespace StudioRentalWeb.Models
{
    public class Studio
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название студии обязательно")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Адрес обязателен")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Цена за час обязательна")]
        [Range(0, 100000, ErrorMessage = "Цена должна быть от 0 до 100000")]
        public decimal PricePerHour { get; set; }

        [Url(ErrorMessage = "Неверный формат URL")]
        public string? ImageUrl { get; set; }

        public byte[]? ImageData { get; set; }
        public string? ImageContentType { get; set; }

        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }

        [Required]
        public int OwnerId { get; set; }

        public User? Owner { get; set; }

        public string GetImageUrl()
        {
            if (ImageData != null && ImageContentType != null)
            {
                return $"data:{ImageContentType};base64,{Convert.ToBase64String(ImageData)}";
            }
            return ImageUrl ?? "/img/studio.jpg";
        }
    }
}