using System.ComponentModel.DataAnnotations;

namespace StudioRental_Markov.Models
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "ФИО обязательно")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Неверный формат телефона")]
        [MaxLength(20)]
        public string? Phone { get; set; }
    }
}