using System.ComponentModel.DataAnnotations;

namespace StudioRental_Markov.Models
{
    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "ФИО обязательно")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Неверный формат телефона")]
        [MaxLength(20)]
        public string? Phone { get; set; }
    }
}