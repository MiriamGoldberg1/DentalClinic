using DentalClinic.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Data.Configurations;

/// <summary>Fluent API למטופל: אורכי שדות, כתובת כ-Owned Type וקשר אחד-לרבים לתורים.</summary>
public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Phone).IsRequired().HasMaxLength(20);
        builder.Ignore(p => p.Age);

        builder.OwnsOne(p => p.Address, address => AddressConfiguration.Configure(address));
        builder.Navigation(p => p.Address).IsRequired();

        builder.HasMany(p => p.Appointments)
            .WithOne(a => a.Patient)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
