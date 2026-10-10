using DentalClinic.Core.Common;
using DentalClinic.Core.Enums;

namespace DentalClinic.Core.Entities;

/// <summary>בסיס משותף למטופל ולרופא: פרטים אישיים, כתובת וגיל מחושב.</summary>
public abstract class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Address Address { get; set; } = new();
    public DateOnly DateOfBirth { get; set; }
    public Gender Gender { get; set; }

    // גיל מחושב לפי תאריך הלידה והתאריך הנוכחי - אינו נשמר בבסיס הנתונים
    public int Age => AgeCalculator.Calculate(DateOfBirth, DateOnly.FromDateTime(DateTime.UtcNow));
}
