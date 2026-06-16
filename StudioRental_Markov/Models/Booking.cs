using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace StudioRental_Markov.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Дата и время начала обязательны")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Дата и время окончания обязательны")]
        public DateTime EndTime { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 1000000, ErrorMessage = "Некорректная стоимость")]
        public decimal TotalPrice { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int StudioId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual User? Customer { get; set; }

        [ForeignKey(nameof(StudioId))]
        public virtual Studio? Studio { get; set; }
    }
}