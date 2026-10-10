using DentalClinic.Core.Common;
using Xunit;

namespace DentalClinic.Tests.Core;

/// <summary>בדיקות לחישוב הגיל ולכללי שעות הפעילות.</summary>
public class AgeCalculatorTests
{
    [Theory]
    [InlineData(1990, 10, 7, 36)]  // יום ההולדת היום
    [InlineData(1990, 10, 8, 35)]  // יום ההולדת מחר
    [InlineData(1990, 1, 1, 36)]
    public void Calculate_ReturnsWholeYears(int year, int month, int day, int expected)
    {
        var age = AgeCalculator.Calculate(new DateOnly(year, month, day), new DateOnly(2026, 10, 7));

        Assert.Equal(expected, age);
    }

    [Fact]
    public void ClinicHours_HasFourteenSlotsBetween11And18()
    {
        var slots = ClinicHours.AllSlots().ToList();

        Assert.Equal(14, slots.Count);
        Assert.Equal(new TimeOnly(11, 0), slots[0]);
        Assert.Equal(new TimeOnly(17, 30), slots[^1]);
    }

    [Theory]
    [InlineData(2026, 10, 11, true)]   // ראשון
    [InlineData(2026, 10, 15, true)]   // חמישי
    [InlineData(2026, 10, 16, false)]  // שישי
    [InlineData(2026, 10, 17, false)]  // שבת
    public void IsWorkingDay_SundayToThursdayOnly(int year, int month, int day, bool expected)
    {
        Assert.Equal(expected, ClinicHours.IsWorkingDay(new DateOnly(year, month, day)));
    }
}
