using Induva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Induva.Infrastructure.Persistence.Configurations;

public class GovernorateConfiguration : IEntityTypeConfiguration<Governorate>
{
    public void Configure(EntityTypeBuilder<Governorate> b)
    {
        b.Property(x => x.NameEn).HasMaxLength(100).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.NameEn).IsUnique();
    }
}

public class RawMaterialConfiguration : IEntityTypeConfiguration<RawMaterial>
{
    public void Configure(EntityTypeBuilder<RawMaterial> b)
    {
        b.Property(x => x.NameEn).HasMaxLength(100).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(100).IsRequired();
        b.Property(x => x.Category).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.NameEn).IsUnique();
    }
}

public class GovernorateResourceConfiguration : IEntityTypeConfiguration<GovernorateResource>
{
    public void Configure(EntityTypeBuilder<GovernorateResource> b)
    {
        b.Property(x => x.Notes).HasColumnType("nvarchar(max)");

        b.HasOne(x => x.Governorate)
            .WithMany()
            .HasForeignKey(x => x.GovernorateId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.RawMaterial)
            .WithMany()
            .HasForeignKey(x => x.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.GovernorateId, x.RawMaterialId });
    }
}

