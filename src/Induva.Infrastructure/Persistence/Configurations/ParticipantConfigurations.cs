using Induva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Induva.Infrastructure.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        b.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        b.Property(x => x.OwnerUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.Description).HasColumnType("nvarchar(max)");
        b.HasIndex(x => x.OwnerUserId);

        b.HasOne(x => x.Governorate)
            .WithMany()
            .HasForeignKey(x => x.GovernorateId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Materials)
            .WithOne(m => m.Supplier)
            .HasForeignKey(m => m.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SupplierMaterialConfiguration : IEntityTypeConfiguration<SupplierMaterial>
{
    public void Configure(EntityTypeBuilder<SupplierMaterial> b)
    {
        b.Property(x => x.PriceNote).HasMaxLength(250);

        b.HasOne(x => x.RawMaterial)
            .WithMany()
            .HasForeignKey(x => x.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StartupConfiguration : IEntityTypeConfiguration<Startup>
{
    public void Configure(EntityTypeBuilder<Startup> b)
    {
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.OwnerUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.Description).HasColumnType("nvarchar(max)");
        b.Property(x => x.ProductIdea).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.RequiredTagsCsv).HasMaxLength(1000);
        b.HasIndex(x => x.OwnerUserId);

        b.HasOne(x => x.Governorate)
            .WithMany()
            .HasForeignKey(x => x.GovernorateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class InvestorProfileConfiguration : IEntityTypeConfiguration<InvestorProfile>
{
    public void Configure(EntityTypeBuilder<InvestorProfile> b)
    {
        b.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        b.Property(x => x.OwnerUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.SectorsCsv).HasMaxLength(1000);
        b.Property(x => x.PreferredStagesCsv).HasMaxLength(500);
        b.Property(x => x.Bio).HasColumnType("nvarchar(max)");
        b.HasIndex(x => x.OwnerUserId).IsUnique();
    }
}

public class DemandRequestConfiguration : IEntityTypeConfiguration<DemandRequest>
{
    public void Configure(EntityTypeBuilder<DemandRequest> b)
    {
        b.Property(x => x.Title).HasMaxLength(250).IsRequired();
        b.Property(x => x.ProductName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Unit).HasMaxLength(50).IsRequired();
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        b.Property(x => x.RequiredTagsCsv).HasMaxLength(1000);
        b.Property(x => x.Description).HasColumnType("nvarchar(max)");
        b.Property(x => x.RequesterUserId).HasMaxLength(450).IsRequired();
        b.HasIndex(x => x.RequesterUserId);
        b.HasIndex(x => new { x.GovernorateId, x.Sector });

        b.HasOne(x => x.Governorate)
            .WithMany()
            .HasForeignKey(x => x.GovernorateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ConnectionRequestConfiguration : IEntityTypeConfiguration<ConnectionRequest>
{
    public void Configure(EntityTypeBuilder<ConnectionRequest> b)
    {
        b.Property(x => x.FromUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.ToUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.Message).HasColumnType("nvarchar(max)").IsRequired();
        b.HasIndex(x => x.FromUserId);
        b.HasIndex(x => x.ToUserId);
        b.HasIndex(x => new { x.FromUserId, x.ToUserId, x.RelatedType, x.RelatedId });
    }
}

