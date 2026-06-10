using System.ComponentModel.DataAnnotations;

namespace StudioRentalWeb.Models
{
    public class StudioViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название студии обязательно")]
        [Display(Name = "Название")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Описание")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Адрес обязателен")]
        [Display(Name = "Адрес")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Цена за час обязательна")]
        [Range(0, 100000, ErrorMessage = "Цена должна быть от 0 до 100000")]
        [Display(Name = "Цена за час")]
        public decimal PricePerHour { get; set; }

        [Display(Name = "URL изображения")]
        public string? ImageUrl { get; set; }
    }
}