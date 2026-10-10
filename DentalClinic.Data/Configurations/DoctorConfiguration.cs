using DentalClinic.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Data.Configurations;

/// <summary>Fluent API לרופא: Concurrency Token, קשר רבים-לרבים להתמחויות וקשר אחד-לרבים לתורים.</summary>
public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Phone).IsRequired().HasMaxLength(20);
        builder.Ignore(d => d.Age);

        builder.OwnsOne(d => d.Address, address => AddressConfiguration.Configure(address));
        builder.Navigation(d => d.Address).IsRequired();

        // Optimistic Concurrency: גרסת היומן נכנסת לתנאי ה-WHERE בכל UPDATE של הרופא
        builder.Property(d => d.Version).IsConcurrencyToken();

        builder.HasMany(d => d.Specializations)
            .WithMany(s => s.Doctors)
            .UsingEntity(join => join.ToTable("DoctorSpecializations"));

        builder.HasMany(d => d.Appointments)
            .WithOne(a => a.Doctor)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
