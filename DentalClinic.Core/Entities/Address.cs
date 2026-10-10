namespace DentalClinic.Core.Entities;

/// <summary>כתובת: עיר, רחוב ומספר בניין. נשמרת כחלק מטבלת הבעלים (Owned Type).</summary>
public class Address
{
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string BuildingNumber { get; set; } = string.Empty;
}
