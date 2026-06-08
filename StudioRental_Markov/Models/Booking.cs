using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace StudioRental_Markov.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; 

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int CustomerId { get; set; }

        public int StudioId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual User? Customer { get; set; }

        [ForeignKey(nameof(StudioId))]
        public virtual Studio? Studio { get; set; }
    }
}
