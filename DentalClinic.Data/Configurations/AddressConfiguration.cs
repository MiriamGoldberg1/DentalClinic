using DentalClinic.Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.Data.Configurations;

/// <summary>הגדרת שדות הכתובת (Owned Type) - משותפת למטופל ולרופא.</summary>
internal static class AddressConfiguration
{
    public static void Configure<TOwner>(OwnedNavigationBuilder<TOwner, Address> address)
        where TOwner : class
    {
        address.Property(a => a.City).IsRequired().HasMaxLength(50);
        address.Property(a => a.Street).IsRequired().HasMaxLength(100);
        address.Property(a => a.BuildingNumber).IsRequired().HasMaxLength(10);
    }
}
