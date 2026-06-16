using System.ComponentModel.DataAnnotations;

namespace StudioRental_Markov.Models
{
    public class StudioUpdateDto
    {
        [Required(ErrorMessage = "Название студии обязательно")]
        [MaxLength(200, ErrorMessage = "Название не должно превышать 200 символов")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Адрес обязателен")]
        [MaxLength(300, ErrorMessage = "Адрес не должен превышать 300 символов")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Цена за час обязательна")]
        [Range(0, 100000, ErrorMessage = "Цена должна быть от 0 до 100000")]
        public decimal PricePerHour { get; set; }

        [Url(ErrorMessage = "Неверный формат URL изображения")]
        public string? ImageUrl { get; set; }

        public IFormFile? Image { get; set; }

        public bool? RemoveImage { get; set; }
    }
}