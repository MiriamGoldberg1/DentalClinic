namespace DentalClinic.Core.Entities;

/// <summary>תחום התמחות של רופא. קשר רבים-לרבים מול Doctor.</summary>
public class Specialization
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
