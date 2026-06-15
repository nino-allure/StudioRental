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
        public byte[]? ImageData { get; set; } 
        public string? ImageContentType { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public int OwnerId { get; set; }
        public User? Owner { get; set; }
        public string GetImageUrl()
        {
            if (ImageData != null && ImageContentType != null)
            {
                return $"data:{ImageContentType};base64,{Convert.ToBase64String(ImageData)}";
            }
            return ImageUrl ?? "/img/gear.jpg";
        }
    }
}