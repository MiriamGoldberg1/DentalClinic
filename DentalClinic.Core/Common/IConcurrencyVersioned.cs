namespace DentalClinic.Core.Common;

/// <summary>
/// ישות שמוגנת ב-Optimistic Concurrency: השדה Version מתחלף בכל עדכון,
/// ושמירה שמבוססת על גרסה ישנה נדחית.
/// </summary>
public interface IConcurrencyVersioned
{
    Guid Version { get; set; }
}
