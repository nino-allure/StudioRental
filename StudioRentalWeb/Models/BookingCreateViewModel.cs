using System.ComponentModel.DataAnnotations;

namespace StudioRentalWeb.Models
{
    public class BookingCreateViewModel
    {
        [Required]
        public int StudioId { get; set; }

        [Required(ErrorMessage = "Дата и время начала обязательны")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Дата и время окончания обязательны")]
        [GreaterThan("StartTime", ErrorMessage = "Время окончания должно быть позже времени начала")]
        public DateTime EndTime { get; set; }
    }

    public class GreaterThanAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public GreaterThanAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime currentValue)
            {
                var property = validationContext.ObjectType.GetProperty(_comparisonProperty);
                if (property != null)
                {
                    var comparisonValue = (DateTime)property.GetValue(validationContext.ObjectInstance);
                    if (currentValue <= comparisonValue)
                    {
                        return new ValidationResult(ErrorMessage);
                    }
                }
            }
            return ValidationResult.Success;
        }
    }
}