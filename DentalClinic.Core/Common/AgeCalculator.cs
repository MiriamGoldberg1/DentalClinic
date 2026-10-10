namespace DentalClinic.Core.Common;

public static class AgeCalculator
{
    /// <summary>מחשב גיל בשנים שלמות לפי תאריך לידה ותאריך נתון (כולל טיפול ביום הולדת שעוד לא הגיע).</summary>
    public static int Calculate(DateOnly dateOfBirth, DateOnly today)
    {
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age))
        {
            age--;
        }

        return age < 0 ? 0 : age;
    }
}
