using DentalClinic.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Data.Configurations;

/// <summary>Fluent API למשתמש: אימייל ייחודי וקישור אופציונלי למטופל.</summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(200);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(300);

        builder.HasOne(u => u.Patient)
            .WithMany()
            .HasForeignKey(u => u.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
