namespace StudioRentalWeb.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public int CustomerId { get; set; }
        public int StudioId { get; set; }
        public User? Customer { get; set; }
        public Studio? Studio { get; set; }
    }
}