using Induva.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Induva.Infrastructure.Persistence.Configurations;

public class AiAnalysisRunConfiguration : IEntityTypeConfiguration<AiAnalysisRun>
{
    public void Configure(EntityTypeBuilder<AiAnalysisRun> b)
    {
        b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        b.Property(x => x.PromptVersion).HasMaxLength(100).IsRequired();
        b.Property(x => x.Model).HasMaxLength(100).IsRequired();
        b.Property(x => x.InputHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.RawResponse).HasColumnType("nvarchar(max)");
        b.Property(x => x.ParsedJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.ErrorMessage).HasColumnType("nvarchar(max)");

        b.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        b.HasIndex(x => x.InputHash);
    }
}

public class ProductSuggestionConfiguration : IEntityTypeConfiguration<ProductSuggestion>
{
    public void Configure(EntityTypeBuilder<ProductSuggestion> b)
    {
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Category).HasMaxLength(100);
        b.Property(x => x.Rationale).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.RequiredMachinesCsv).HasMaxLength(1000);
        b.Property(x => x.RequiredMaterialsCsv).HasMaxLength(1000);
        b.Property(x => x.SeasonalityNote).HasMaxLength(500);
        b.Property(x => x.RisksJson).HasColumnType("nvarchar(max)").IsRequired();

        b.HasOne(x => x.Factory)
            .WithMany(f => f.ProductSuggestions)
            .HasForeignKey(x => x.FactoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DemandForecastConfiguration : IEntityTypeConfiguration<DemandForecast>
{
    public void Configure(EntityTypeBuilder<DemandForecast> b)
    {
        b.Property(x => x.RequestedByUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.ProductName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Category).HasMaxLength(100);
        b.Property(x => x.Trend).HasMaxLength(50).IsRequired();
        b.Property(x => x.ResultJson).HasColumnType("nvarchar(max)").IsRequired();

        b.HasOne(x => x.Governorate)
            .WithMany()
            .HasForeignKey(x => x.GovernorateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MatchResultConfiguration : IEntityTypeConfiguration<MatchResult>
{
    public void Configure(EntityTypeBuilder<MatchResult> b)
    {
        b.Property(x => x.RequestedByUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.ReasonsJson).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.NextStep).HasMaxLength(500);

        b.HasIndex(x => new { x.SourceType, x.SourceId, x.TargetType, x.TargetId });
    }
}

public class InvestmentAnalysisConfiguration : IEntityTypeConfiguration<InvestmentAnalysis>
{
    public void Configure(EntityTypeBuilder<InvestmentAnalysis> b)
    {
        b.Property(x => x.RequestedByUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.Title).HasMaxLength(250).IsRequired();
        b.Property(x => x.AssumptionsJson).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.MetricsJson).HasColumnType("nvarchar(max)").IsRequired();

        b.HasOne(x => x.Governorate)
            .WithMany()
            .HasForeignKey(x => x.GovernorateId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Factory)
            .WithMany()
            .HasForeignKey(x => x.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Startup)
            .WithMany()
            .HasForeignKey(x => x.StartupId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ProductSuggestion)
            .WithMany()
            .HasForeignKey(x => x.ProductSuggestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

