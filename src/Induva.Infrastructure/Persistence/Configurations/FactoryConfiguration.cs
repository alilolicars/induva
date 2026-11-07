using Induva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Induva.Infrastructure.Persistence.Configurations;

public class FactoryConfiguration : IEntityTypeConfiguration<Factory>
{
    public void Configure(EntityTypeBuilder<Factory> b)
    {
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.OwnerUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.Description).HasColumnType("nvarchar(max)");
        b.HasIndex(x => x.OwnerUserId);
        b.HasIndex(x => new { x.GovernorateId, x.Sector });

        b.HasOne(x => x.Governorate)
            .WithMany()
            .HasForeignKey(x => x.GovernorateId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Machines)
            .WithOne(m => m.Factory)
            .HasForeignKey(m => m.FactoryId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.IdleWindows)
            .WithOne(w => w.Factory)
            .HasForeignKey(w => w.FactoryId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Products)
            .WithOne(p => p.Factory)
            .HasForeignKey(p => p.FactoryId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.MaterialNeeds)
            .WithOne(n => n.Factory)
            .HasForeignKey(n => n.FactoryId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.CapabilityProfile)
            .WithOne(p => p.Factory)
            .HasForeignKey<FactoryCapabilityProfile>(p => p.FactoryId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.ProductSuggestions)
            .WithOne(s => s.Factory)
            .HasForeignKey(s => s.FactoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MachineConfiguration : IEntityTypeConfiguration<Machine>
{
    public void Configure(EntityTypeBuilder<Machine> b)
    {
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.MachineType).HasMaxLength(100);
        b.Property(x => x.CapacityUnit).HasMaxLength(50);
        b.Property(x => x.Description).HasColumnType("nvarchar(max)");
    }
}

public class IdleWindowConfiguration : IEntityTypeConfiguration<IdleWindow>
{
    public void Configure(EntityTypeBuilder<IdleWindow> b)
    {
        b.Property(x => x.Note).HasMaxLength(500);
    }
}

public class FactoryProductConfiguration : IEntityTypeConfiguration<FactoryProduct>
{
    public void Configure(EntityTypeBuilder<FactoryProduct> b)
    {
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Category).HasMaxLength(100);
    }
}

public class FactoryMaterialNeedConfiguration : IEntityTypeConfiguration<FactoryMaterialNeed>
{
    public void Configure(EntityTypeBuilder<FactoryMaterialNeed> b)
    {
        b.HasOne(x => x.RawMaterial)
            .WithMany()
            .HasForeignKey(x => x.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FactoryCapabilityProfileConfiguration : IEntityTypeConfiguration<FactoryCapabilityProfile>
{
    public void Configure(EntityTypeBuilder<FactoryCapabilityProfile> b)
    {
        b.Property(x => x.Summary).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.TagsCsv).HasMaxLength(1000);
        b.Property(x => x.ResultJson).HasColumnType("nvarchar(max)").IsRequired();
    }
}

