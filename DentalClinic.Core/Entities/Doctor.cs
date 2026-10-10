using DentalClinic.Core.Common;

namespace DentalClinic.Core.Entities;

/// <summary>
/// רופא במרפאה. יומן הרופא הוא המשאב המוגבל: Version מתחלף בכל קביעת תור,
/// כך ששתי קביעות מקבילות ביומן של אותו רופא לא יכולות להצליח שתיהן על סמך אותו מצב.
/// </summary>
public class Doctor : Person, IConcurrencyVersioned
{
    public DateOnly EmploymentStartDate { get; set; }

    // Concurrency Token - גרסת יומן הרופא
    //זה בשביל הבעיה של תחרות על משאב משותף
    //בכל שינוי של תור כלשהוא הגרסה של יהומן כולו של הרופא משתנה
    public Guid Version { get; set; } = Guid.NewGuid();

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    //רשימת התורים שנקבעו לרופא הזה 
    public ICollection<Specialization> Specializations { get; set; } = new List<Specialization>();
}
