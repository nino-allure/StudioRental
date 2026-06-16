using System.ComponentModel.DataAnnotations;

namespace StudioRentalWeb.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Дата и время начала обязательны")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Дата и время окончания обязательны")]
        public DateTime EndTime { get; set; }

        [Range(0, 1000000, ErrorMessage = "Некорректная стоимость")]
        public decimal TotalPrice { get; set; }

        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int StudioId { get; set; }

        public User? Customer { get; set; }
        public Studio? Studio { get; set; }
    }
}