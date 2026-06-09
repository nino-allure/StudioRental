namespace StudioRentalWeb.Models
{
    public class Studio
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Address { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public int OwnerId { get; set; }
        public User? Owner { get; set; }
    }
}