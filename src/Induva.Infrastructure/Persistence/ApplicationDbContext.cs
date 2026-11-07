using Induva.Domain.Common;
using Induva.Domain.Entities;
using Induva.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Induva.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Governorate> Governorates => Set<Governorate>();
    public DbSet<RawMaterial> RawMaterials => Set<RawMaterial>();
    public DbSet<GovernorateResource> GovernorateResources => Set<GovernorateResource>();
    public DbSet<Factory> Factories => Set<Factory>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<IdleWindow> IdleWindows => Set<IdleWindow>();
    public DbSet<FactoryProduct> FactoryProducts => Set<FactoryProduct>();
    public DbSet<FactoryMaterialNeed> FactoryMaterialNeeds => Set<FactoryMaterialNeed>();
    public DbSet<FactoryCapabilityProfile> FactoryCapabilityProfiles => Set<FactoryCapabilityProfile>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierMaterial> SupplierMaterials => Set<SupplierMaterial>();
    public DbSet<Startup> Startups => Set<Startup>();
    public DbSet<InvestorProfile> InvestorProfiles => Set<InvestorProfile>();
    public DbSet<DemandRequest> DemandRequests => Set<DemandRequest>();
    public DbSet<ConnectionRequest> ConnectionRequests => Set<ConnectionRequest>();
    public DbSet<AiAnalysisRun> AiAnalysisRuns => Set<AiAnalysisRun>();
    public DbSet<ProductSuggestion> ProductSuggestions => Set<ProductSuggestion>();
    public DbSet<DemandForecast> DemandForecasts => Set<DemandForecast>();
    public DbSet<MatchResult> MatchResults => Set<MatchResult>();
    public DbSet<InvestmentAnalysis> InvestmentAnalyses => Set<InvestmentAnalysis>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        c.Properties<decimal>().HavePrecision(18, 2);
        c.Properties<string>().HaveMaxLength(500); // default; override with HasColumnType("nvarchar(max)") for JSON/long text
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var e in ChangeTracker.Entries<BaseEntity>().Where(e => e.State == EntityState.Modified))
            e.Entity.UpdatedAtUtc = DateTime.UtcNow;

        return base.SaveChangesAsync(ct);
    }
}

