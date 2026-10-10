using DentalClinic.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Data.Configurations;

/// <summary>
/// Fluent API לתור. האינדקסים הייחודיים הם רשת ביטחון אחרונה בבסיס הנתונים:
/// לא ייתכנו שני תורים לאותו רופא (או לאותו מטופל) באותו תאריך ושעה.
/// </summary>
public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Notes).HasMaxLength(500);

        builder.HasIndex(a => new { a.DoctorId, a.Date, a.Time }).IsUnique();
        builder.HasIndex(a => new { a.PatientId, a.Date, a.Time }).IsUnique();
    }
}
