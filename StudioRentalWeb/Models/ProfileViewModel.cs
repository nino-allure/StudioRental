using System.ComponentModel.DataAnnotations;

namespace StudioRentalWeb.Models
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "ФИО обязательно")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Неверный формат телефона")]
        public string? Phone { get; set; }
    }
}