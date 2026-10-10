using DentalClinic.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Data.Configurations;

/// <summary>Fluent API לתחומי התמחות, כולל Seed Data שנכנס דרך migration.</summary>
public class SpecializationConfiguration : IEntityTypeConfiguration<Specialization>
{
    public void Configure(EntityTypeBuilder<Specialization> builder)
    {
        builder.ToTable("Specializations");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(s => s.Name).IsUnique();

        builder.HasData(
            new Specialization { Id = 1, Name = "רפואת שיניים כללית" },
            new Specialization { Id = 2, Name = "יישור שיניים" },
            new Specialization { Id = 3, Name = "טיפולי שורש" },
            new Specialization { Id = 4, Name = "רפואת שיניים לילדים" },
            new Specialization { Id = 5, Name = "כירורגיית פה ולסת" });
    }
}
