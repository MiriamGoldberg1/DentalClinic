namespace DentalClinic.Core.Common;

/// <summary>
/// כללי שעות הפעילות של המרפאה: ימים א'-ה', בין 11:00 ל-18:00, תור של 30 דקות.
/// </summary>
public static class ClinicHours
{
    public const int AppointmentLengthMinutes = 30;

    public static readonly TimeOnly Opening = new(11, 0);
    public static readonly TimeOnly Closing = new(18, 0);

    public static readonly IReadOnlyList<DayOfWeek> WorkingDays = new[]
                                  //DayOfWeek->enum של c# מחלקה קימת בשפה
    {
        DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday
    };

    /// <summary>האם התאריך נופל ביום עבודה (ראשון עד חמישי).</summary>
    public static bool IsWorkingDay(DateOnly date) => WorkingDays.Contains(date.DayOfWeek);

    /// <summary>האם שעת ההתחלה תקינה: בתוך שעות הפעילות, מסתיימת עד 18:00 ומיושרת לחלון של 30 דקות.</summary>
    public static bool IsValidStartTime(TimeOnly time)
    {
        if (time < Opening || time >= Closing || time.Second != 0 || time.Millisecond != 0)
        {
            return false;
        }

        var minutesFromOpening = (int)(time - Opening).TotalMinutes;
        return minutesFromOpening % AppointmentLengthMinutes == 0
               && time.AddMinutes(AppointmentLengthMinutes) <= Closing;
    }

    /// <summary>מחזיר את כל שעות ההתחלה האפשריות ביום עבודה (11:00, 11:30 ... 17:30).</summary>
    public static IEnumerable<TimeOnly> AllSlots()
    {
        for (var time = Opening; time.AddMinutes(AppointmentLengthMinutes) <= Closing; time = time.AddMinutes(AppointmentLengthMinutes))
        {
            yield return time;
        }
    }

    /// <summary>מחזיר את יום העבודה הקרוב ביותר שאחרי התאריך הנתון.</summary>
    public static DateOnly NextWorkingDay(DateOnly after)
    {
        var date = after.AddDays(1);
        while (!IsWorkingDay(date))
        {
            date = date.AddDays(1);
        }

        return date;
    }
}
