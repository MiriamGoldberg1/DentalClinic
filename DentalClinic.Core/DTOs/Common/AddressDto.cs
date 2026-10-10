using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Core.DTOs.Common;

/// <summary>כתובת בבקשות ובתשובות של ה-API.</summary>
public class AddressDto
{
    [Required(ErrorMessage = "יש להזין עיר")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "שם העיר חייב להכיל 2-50 תווים")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "יש להזין רחוב")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "שם הרחוב חייב להכיל 2-100 תווים")]
    public string Street { get; set; } = string.Empty;

    [Required(ErrorMessage = "יש להזין מספר בניין")]
    [StringLength(10, ErrorMessage = "מספר הבניין ארוך מדי")]
    public string BuildingNumber { get; set; } = string.Empty;
}
