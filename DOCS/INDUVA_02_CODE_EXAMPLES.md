# INDUVA — 02 Code Examples (reference implementation per layer)

> These are **reference patterns**. Follow structure/naming/behavior; fix compile details as needed.
> Every other entity/service/controller must follow the same pattern as the Factory vertical slice shown here.
> AI-specific code is in file 03.

---

## 1. Domain

### 1.1 `Domain/Common/BaseEntity.cs`
```csharp
namespace Induva.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
```

### 1.2 `Domain/Constants/Roles.cs`
```csharp
namespace Induva.Domain.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string FactoryOwner = "FactoryOwner";
    public const string Supplier = "Supplier";
    public const string Investor = "Investor";
    public const string Startup = "Startup";
    public const string Buyer = "Buyer";

    public static readonly string[] All = [Admin, FactoryOwner, Supplier, Investor, Startup, Buyer];
    public static readonly string[] SelfRegistrable = [FactoryOwner, Supplier, Investor, Startup, Buyer];
}
```

### 1.3 `Domain/Enums/Sector.cs` (all enums follow this style)
```csharp
namespace Induva.Domain.Enums;

public enum Sector
{
    FoodProcessing, Textiles, Plastics, Chemicals, Metals, Ceramics,
    Wood, Construction, Packaging, Electronics, Agriculture, Other
}
```

### 1.4 `Domain/Entities/Governorate.cs`
```csharp
using Induva.Domain.Common;
namespace Induva.Domain.Entities;

public class Governorate : BaseEntity
{
    public string NameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
```

### 1.5 `Domain/Entities/Factory.cs` (+ children)
```csharp
using Induva.Domain.Common;
using Induva.Domain.Enums;
namespace Induva.Domain.Entities;

public class Factory : BaseEntity
{
    public string OwnerUserId { get; set; } = default!;      // Identity user id (no FK — keeps Domain pure)
    public string Name { get; set; } = default!;
    public Sector Sector { get; set; }
    public int GovernorateId { get; set; }
    public Governorate Governorate { get; set; } = default!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Description { get; set; }
    public int UtilizationPercent { get; set; }              // 0..100
    public bool IsPublic { get; set; } = true;

    public ICollection<Machine> Machines { get; set; } = new List<Machine>();
    public ICollection<IdleWindow> IdleWindows { get; set; } = new List<IdleWindow>();
    public ICollection<FactoryProduct> Products { get; set; } = new List<FactoryProduct>();
    public ICollection<FactoryMaterialNeed> MaterialNeeds { get; set; } = new List<FactoryMaterialNeed>();
    public FactoryCapabilityProfile? CapabilityProfile { get; set; }
}

public class Machine : BaseEntity
{
    public int FactoryId { get; set; }
    public Factory Factory { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? MachineType { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal? CapacityPerHour { get; set; }
    public string? CapacityUnit { get; set; }                // e.g. "kg", "pieces", "liters"
    public string? Description { get; set; }
}

public class IdleWindow : BaseEntity
{
    public int FactoryId { get; set; }
    public Factory Factory { get; set; } = default!;
    public int StartMonth { get; set; }                      // 1..12
    public int EndMonth { get; set; }                        // 1..12 (may wrap year-end, e.g. 11 → 2)
    public int AvailableCapacityPercent { get; set; }       // 0..100
    public string? Note { get; set; }
}

public class FactoryProduct : BaseEntity
{
    public int FactoryId { get; set; }
    public Factory Factory { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Category { get; set; }
}

public class FactoryMaterialNeed : BaseEntity
{
    public int FactoryId { get; set; }
    public Factory Factory { get; set; } = default!;
    public int RawMaterialId { get; set; }
    public RawMaterial RawMaterial { get; set; } = default!;
    public decimal? MonthlyTons { get; set; }
}

public class FactoryCapabilityProfile : BaseEntity
{
    public int FactoryId { get; set; }
    public Factory Factory { get; set; } = default!;
    public string Summary { get; set; } = default!;
    public string TagsCsv { get; set; } = "";
    public string ResultJson { get; set; } = "{}";
    public int? AiRunId { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
```

---

## 2. Application

### 2.1 `Application/Common/Result.cs`
```csharp
namespace Induva.Application.Common;

public enum ErrorKind { None, Validation, NotFound, Forbidden, RateLimited, AiUnavailable }

public class Result
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public ErrorKind Kind { get; init; }

    public static Result Ok() => new() { Succeeded = true };
    public static Result Fail(ErrorKind kind, string error) => new() { Succeeded = false, Kind = kind, Error = error };
}

public class Result<T> : Result
{
    public T? Value { get; init; }

    public static Result<T> Ok(T value) => new() { Succeeded = true, Value = value };
    public new static Result<T> Fail(ErrorKind kind, string error) => new() { Succeeded = false, Kind = kind, Error = error };
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
}
```

### 2.2 `Application/Abstractions/*`
```csharp
using System.Linq.Expressions;
using Induva.Domain.Common;
namespace Induva.Application.Abstractions;

public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default, params Expression<Func<T, object>>[] includes);
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default,
                                     params Expression<Func<T, object>>[] includes);
    Task<(IReadOnlyList<T> Items, int Total)> PagedAsync(Expression<Func<T, bool>>? filter, int page, int pageSize,
                                     CancellationToken ct = default, params Expression<Func<T, object>>[] includes);
    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}

public interface IUnitOfWork
{
    IGenericRepository<T> Repository<T>() where T : BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface ICurrentUserService
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
```
> Limitation: `Expression<Func<T,object>>` supports first-level `Include` only. For deeper graphs, add a dedicated
> method on a specific interface in Application and implement it in Infrastructure (only when really needed).

### 2.3 DTOs `Application/Dtos/FactoryDtos.cs`
```csharp
using Induva.Domain.Enums;
namespace Induva.Application.Dtos;

public sealed record FactoryFilter(int? GovernorateId = null, Sector? Sector = null, string? Q = null, int Page = 1, int PageSize = 12);

public sealed record FactoryListItemDto(int Id, string Name, Sector Sector, string GovernorateName, int UtilizationPercent, int MachineCount);

public sealed record MachineDto(int Id, string Name, string? MachineType, int Quantity, decimal? CapacityPerHour, string? CapacityUnit);

public sealed record FactoryDetailsDto(
    int Id, string Name, Sector Sector, int GovernorateId, string GovernorateName, string? Description,
    int UtilizationPercent, double Latitude, double Longitude, bool IsOwner,
    IReadOnlyList<MachineDto> Machines, IReadOnlyList<string> Products, IReadOnlyList<string> CapabilityTags);

public sealed record SaveFactoryDto(
    string Name, Sector Sector, int GovernorateId, string? Description,
    int UtilizationPercent, double? Latitude, double? Longitude, bool IsPublic);
```

### 2.4 Mapping `Application/Mappings/FactoryMappings.cs`
```csharp
using Induva.Application.Dtos;
using Induva.Domain.Entities;
namespace Induva.Application.Mappings;

public static class FactoryMappings
{
    public static FactoryListItemDto ToListItem(this Factory f) =>
        new(f.Id, f.Name, f.Sector, f.Governorate?.NameEn ?? "", f.UtilizationPercent, f.Machines?.Count ?? 0);

    public static MachineDto ToDto(this Machine m) =>
        new(m.Id, m.Name, m.MachineType, m.Quantity, m.CapacityPerHour, m.CapacityUnit);
}
```

### 2.5 Service `Application/Services/FactoryService.cs`
```csharp
using Induva.Application.Abstractions;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Application.Mappings;
using Induva.Domain.Constants;
using Induva.Domain.Entities;
namespace Induva.Application.Services;

public interface IFactoryService
{
    Task<PagedResult<FactoryListItemDto>> SearchAsync(FactoryFilter filter, CancellationToken ct);
    Task<Result<FactoryDetailsDto>> GetAsync(int id, CancellationToken ct);
    Task<Result<int>> CreateAsync(SaveFactoryDto dto, CancellationToken ct);
    Task<Result> UpdateAsync(int id, SaveFactoryDto dto, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}

public sealed class FactoryService(IUnitOfWork uow, ICurrentUserService user) : IFactoryService
{
    public async Task<PagedResult<FactoryListItemDto>> SearchAsync(FactoryFilter f, CancellationToken ct)
    {
        var q = f.Q?.Trim();
        var (items, total) = await uow.Repository<Factory>().PagedAsync(
            x => x.IsPublic
                 && (f.GovernorateId == null || x.GovernorateId == f.GovernorateId)
                 && (f.Sector == null || x.Sector == f.Sector)
                 && (string.IsNullOrEmpty(q) || x.Name.Contains(q)),
            f.Page, f.PageSize, ct, x => x.Governorate, x => x.Machines);

        return new PagedResult<FactoryListItemDto>(items.Select(x => x.ToListItem()).ToList(), total, f.Page, f.PageSize);
    }

    public async Task<Result<FactoryDetailsDto>> GetAsync(int id, CancellationToken ct)
    {
        var f = await uow.Repository<Factory>().GetByIdAsync(id, ct,
            x => x.Governorate, x => x.Machines, x => x.Products, x => x.CapabilityProfile!);
        if (f is null) return Result<FactoryDetailsDto>.Fail(ErrorKind.NotFound, "Factory not found.");
        if (!f.IsPublic && !CanManage(f)) return Result<FactoryDetailsDto>.Fail(ErrorKind.Forbidden, "This profile is private.");

        var tags = string.IsNullOrWhiteSpace(f.CapabilityProfile?.TagsCsv)
            ? Array.Empty<string>()
            : f.CapabilityProfile!.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);

        return Result<FactoryDetailsDto>.Ok(new FactoryDetailsDto(
            f.Id, f.Name, f.Sector, f.GovernorateId, f.Governorate.NameEn, f.Description, f.UtilizationPercent,
            f.Latitude, f.Longitude, CanManage(f),
            f.Machines.Select(m => m.ToDto()).ToList(), f.Products.Select(p => p.Name).ToList(), tags));
    }

    public async Task<Result<int>> CreateAsync(SaveFactoryDto dto, CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.UserId is null)
            return Result<int>.Fail(ErrorKind.Forbidden, "Please log in.");

        var gov = await uow.Repository<Governorate>().GetByIdAsync(dto.GovernorateId, ct);
        if (gov is null) return Result<int>.Fail(ErrorKind.Validation, "Unknown governorate.");

        var entity = new Factory
        {
            OwnerUserId = user.UserId,
            Name = dto.Name.Trim(),
            Sector = dto.Sector,
            GovernorateId = gov.Id,
            Description = dto.Description?.Trim(),
            UtilizationPercent = Math.Clamp(dto.UtilizationPercent, 0, 100),
            Latitude = dto.Latitude ?? gov.Latitude,        // fallback to governorate centroid
            Longitude = dto.Longitude ?? gov.Longitude,
            IsPublic = dto.IsPublic
        };
        await uow.Repository<Factory>().AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        return Result<int>.Ok(entity.Id);
    }

    public async Task<Result> UpdateAsync(int id, SaveFactoryDto dto, CancellationToken ct)
    {
        var f = await uow.Repository<Factory>().GetByIdAsync(id, ct);
        if (f is null) return Result.Fail(ErrorKind.NotFound, "Factory not found.");
        if (!CanManage(f)) return Result.Fail(ErrorKind.Forbidden, "You do not own this factory.");

        f.Name = dto.Name.Trim();
        f.Sector = dto.Sector;
        f.GovernorateId = dto.GovernorateId;
        f.Description = dto.Description?.Trim();
        f.UtilizationPercent = Math.Clamp(dto.UtilizationPercent, 0, 100);
        f.IsPublic = dto.IsPublic;
        if (dto.Latitude is not null && dto.Longitude is not null) { f.Latitude = dto.Latitude.Value; f.Longitude = dto.Longitude.Value; }

        uow.Repository<Factory>().Update(f);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var f = await uow.Repository<Factory>().GetByIdAsync(id, ct);
        if (f is null) return Result.Fail(ErrorKind.NotFound, "Factory not found.");
        if (!CanManage(f)) return Result.Fail(ErrorKind.Forbidden, "You do not own this factory.");
        uow.Repository<Factory>().Remove(f);
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private bool CanManage(Factory f) => user.IsInRole(Roles.Admin) || (user.UserId is not null && f.OwnerUserId == user.UserId);
}
```
> Machines / IdleWindows / Products / MaterialNeeds get their own small services (`MachineService`, …) following the same
> ownership pattern: load the parent factory, `CanManage`, mutate, save.

### 2.6 `Application/DependencyInjection.cs`
```csharp
using Induva.Application.AI;
using Induva.Application.AI.Features;
using Induva.Application.Services;
using Microsoft.Extensions.DependencyInjection;
namespace Induva.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection s)
    {
        s.AddScoped<IFactoryService, FactoryService>();
        s.AddScoped<ISupplierService, SupplierService>();
        s.AddScoped<IStartupService, StartupService>();
        s.AddScoped<IInvestorService, InvestorService>();
        s.AddScoped<IDemandRequestService, DemandRequestService>();
        s.AddScoped<IConnectionService, ConnectionService>();
        s.AddScoped<IMapService, MapService>();
        s.AddScoped<ILookupService, LookupService>();

        s.AddScoped<IAiJsonRunner, AiJsonRunner>();
        s.AddScoped<IFactoryAnalysisService, FactoryAnalysisService>();
        s.AddScoped<IProductSuggestionService, ProductSuggestionService>();
        s.AddScoped<IDemandForecastService, DemandForecastService>();
        s.AddScoped<IMatchingService, MatchingService>();
        s.AddScoped<IInvestmentAnalysisService, InvestmentAnalysisService>();
        return s;
    }
}
```
(`Microsoft.Extensions.DependencyInjection.Abstractions` package is needed in Application for this extension.)

### 2.7 `Application/Common/Geo.cs`
```csharp
namespace Induva.Application.Common;

public static class Geo
{
    public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        double dLat = Rad(lat2 - lat1), dLon = Rad(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * R * Math.Asin(Math.Sqrt(a));
    }
    private static double Rad(double d) => d * Math.PI / 180;

    /// 0..1 — 1 = same place, 0 = 600 km or more (roughly Alexandria → Aswan is ~900 km).
    public static double ProximityScore(double km) => Math.Max(0, 1 - km / 600.0);
}
```

---

## 3. Infrastructure

### 3.1 `Infrastructure/Identity/ApplicationUser.cs`
```csharp
using Microsoft.AspNetCore.Identity;
namespace Induva.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
}
```

### 3.2 `Infrastructure/Persistence/ApplicationDbContext.cs`
```csharp
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
        c.Properties<string>().HaveMaxLength(500);   // default; override with HasMaxLength / HasColumnType("nvarchar(max)") for JSON/text
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var e in ChangeTracker.Entries<BaseEntity>().Where(e => e.State == EntityState.Modified))
            e.Entity.UpdatedAtUtc = DateTime.UtcNow;
        return base.SaveChangesAsync(ct);
    }
}
```
> `ConfigureConventions` default of 500 chars would truncate JSON/Description columns → every JSON/long text property MUST
> be configured explicitly with `.HasColumnType("nvarchar(max)")`. (See `AiAnalysisRunConfiguration`.)

### 3.3 Entity configuration `Infrastructure/Persistence/Configurations/FactoryConfiguration.cs`
```csharp
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

        b.HasOne(x => x.Governorate).WithMany().HasForeignKey(x => x.GovernorateId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Machines).WithOne(m => m.Factory).HasForeignKey(m => m.FactoryId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.IdleWindows).WithOne(m => m.Factory).HasForeignKey(m => m.FactoryId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Products).WithOne(m => m.Factory).HasForeignKey(m => m.FactoryId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.MaterialNeeds).WithOne(m => m.Factory).HasForeignKey(m => m.FactoryId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.CapabilityProfile).WithOne(p => p.Factory)
            .HasForeignKey<FactoryCapabilityProfile>(p => p.FactoryId).OnDelete(DeleteBehavior.Cascade);
    }
}
```
(`FactoryMaterialNeed.RawMaterial` FK: `Restrict`.)

### 3.4 `Infrastructure/Persistence/Repositories/GenericRepository.cs`
```csharp
using System.Linq.Expressions;
using Induva.Application.Abstractions;
using Induva.Domain.Common;
using Microsoft.EntityFrameworkCore;
namespace Induva.Infrastructure.Persistence.Repositories;

public class GenericRepository<T>(ApplicationDbContext db) : IGenericRepository<T> where T : BaseEntity
{
    private IQueryable<T> Build(Expression<Func<T, bool>>? filter, Expression<Func<T, object>>[] includes, bool tracking)
    {
        IQueryable<T> q = db.Set<T>();
        if (!tracking) q = q.AsNoTracking();
        foreach (var inc in includes) q = q.Include(inc);
        if (includes.Length > 1) q = q.AsSplitQuery();          // avoids cartesian explosion with several collection includes
        if (filter is not null) q = q.Where(filter);
        return q;
    }

    public Task<T?> GetByIdAsync(int id, CancellationToken ct = default, params Expression<Func<T, object>>[] includes)
        => Build(null, includes, tracking: true).FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default,
        params Expression<Func<T, object>>[] includes)
        => await Build(filter, includes, tracking: false).ToListAsync(ct);

    public async Task<(IReadOnlyList<T> Items, int Total)> PagedAsync(Expression<Func<T, bool>>? filter, int page, int pageSize,
        CancellationToken ct = default, params Expression<Func<T, object>>[] includes)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var q = Build(filter, includes, tracking: false);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(e => e.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<int> CountAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default)
        => (filter is null ? db.Set<T>() : db.Set<T>().Where(filter)).CountAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default) => await db.Set<T>().AddAsync(entity, ct);
    public void Update(T entity) => db.Set<T>().Update(entity);
    public void Remove(T entity) => db.Set<T>().Remove(entity);
}
```

### 3.5 `Infrastructure/Persistence/Repositories/UnitOfWork.cs`
```csharp
using Induva.Application.Abstractions;
using Induva.Domain.Common;
namespace Induva.Infrastructure.Persistence.Repositories;

public class UnitOfWork(ApplicationDbContext db) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repos = new();

    public IGenericRepository<T> Repository<T>() where T : BaseEntity
    {
        if (!_repos.TryGetValue(typeof(T), out var repo))
            _repos[typeof(T)] = repo = new GenericRepository<T>(db);
        return (IGenericRepository<T>)repo;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
```

### 3.6 `Infrastructure/DependencyInjection.cs`
```csharp
using Induva.Application.AI;
using Induva.Application.Abstractions;
using Induva.Infrastructure.AI;
using Induva.Infrastructure.Identity;
using Induva.Infrastructure.Persistence;
using Induva.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
namespace Induva.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection s, IConfiguration cfg)
    {
        s.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(cfg.GetConnectionString("Default")));
        s.AddScoped<IUnitOfWork, UnitOfWork>();

        s.AddIdentity<ApplicationUser, IdentityRole>(o =>
        {
            o.Password.RequiredLength = 8;
            o.Password.RequireNonAlphanumeric = false;
            o.User.RequireUniqueEmail = true;
        }).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

        s.Configure<AiOptions>(cfg.GetSection("Ai"));
        s.PostConfigure<AiOptions>(o => o.ApiKey ??= cfg["SBG_API_KEY"]);   // env var fallback
        s.AddSingleton<AiConcurrencyGate>();                                 // limits parallel calls on the shared student key

        if (string.Equals(cfg["Ai:Mode"], "Demo", StringComparison.OrdinalIgnoreCase))
        {
            s.AddScoped<IAiClient, DemoAiClient>();
        }
        else
        {
            s.AddHttpClient<IAiClient, ItiAiClient>((sp, c) =>
            {
                var o = sp.GetRequiredService<IOptions<AiOptions>>().Value;
                if (string.IsNullOrWhiteSpace(o.ApiKey))
                    throw new InvalidOperationException("AI API key missing. Set user-secret Ai:ApiKey or env var SBG_API_KEY (or use Ai:Mode=Demo).");
                c.BaseAddress = new Uri(o.BaseUrl);                 // MUST end with '/'
                c.Timeout = Timeout.InfiniteTimeSpan;               // resilience handler owns timeouts (HttpClient default is 100s!)
                c.DefaultRequestHeaders.Authorization = new("Bearer", o.ApiKey);
            })
            .AddStandardResilienceHandler(r =>
            {
                r.AttemptTimeout.Timeout = TimeSpan.FromSeconds(120);
                r.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(300);
                r.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(240);   // must be >= 2 x AttemptTimeout
                r.Retry.MaxRetryAttempts = 1;                                     // LLM calls are slow; retry once only
            });
        }
        return s;
    }
}
```

### 3.7 Seeder `Infrastructure/Seeding/DbInitializer.cs`
```csharp
using Induva.Domain.Constants;
using Induva.Domain.Entities;
using Induva.Infrastructure.Identity;
using Induva.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Induva.Infrastructure.Seeding;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        await db.Database.MigrateAsync();

        // 1) roles
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var r in Roles.All)
            if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole(r));

        // 2) admin (password only from user-secrets: Seed:AdminPassword)
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = cfg["Seed:AdminEmail"]; var pwd = cfg["Seed:AdminPassword"];
        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(pwd) && await users.FindByEmailAsync(email) is null)
        {
            var admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = "Admin" };
            if ((await users.CreateAsync(admin, pwd)).Succeeded) await users.AddToRoleAsync(admin, Roles.Admin);
        }

        // 3) lookups
        if (!await db.Governorates.AnyAsync())
        {
            db.Governorates.AddRange(
                G("Cairo","القاهرة",30.0444,31.2357), G("Giza","الجيزة",30.0131,31.2089), G("Alexandria","الإسكندرية",31.2001,29.9187),
                G("Qalyubia","القليوبية",30.4660,31.1850), G("Port Said","بورسعيد",31.2653,32.3019), G("Suez","السويس",29.9668,32.5498),
                G("Ismailia","الإسماعيلية",30.5965,32.2715), G("Damietta","دمياط",31.4165,31.8133), G("Dakahlia","الدقهلية",31.0409,31.3785),
                G("Sharqia","الشرقية",30.5877,31.5020), G("Gharbia","الغربية",30.7865,31.0004), G("Kafr El Sheikh","كفر الشيخ",31.1107,30.9388),
                G("Monufia","المنوفية",30.5972,30.9876), G("Beheira","البحيرة",31.0341,30.4682), G("Fayoum","الفيوم",29.3084,30.8428),
                G("Beni Suef","بني سويف",29.0661,31.0994), G("Minya","المنيا",28.1099,30.7503), G("Assiut","أسيوط",27.1809,31.1837),
                G("Sohag","سوهاج",26.5591,31.6948), G("Qena","قنا",26.1551,32.7160), G("Luxor","الأقصر",25.6872,32.6396),
                G("Aswan","أسوان",24.0889,32.8998), G("Red Sea","البحر الأحمر",27.2579,33.8116), G("New Valley","الوادي الجديد",25.4390,30.5586),
                G("Matrouh","مطروح",31.3543,27.2373), G("North Sinai","شمال سيناء",31.1321,33.7984), G("South Sinai","جنوب سيناء",28.2410,33.6220));
            await db.SaveChangesAsync();
        }

        if (!await db.RawMaterials.AnyAsync())
        {
            db.RawMaterials.AddRange(
                M("Limestone","حجر جيري","Mineral"), M("Gypsum","جبس","Mineral"), M("Silica sand","رمل سيليكا","Mineral"),
                M("Clay","طفلة","Mineral"), M("Kaolin","كاولين","Mineral"), M("Feldspar","فلسبار","Mineral"),
                M("Marble","رخام","Mineral"), M("Granite","جرانيت","Mineral"), M("Basalt","بازلت","Mineral"),
                M("Phosphate","فوسفات","Mineral"), M("Iron ore","خام الحديد","Mineral"), M("Salt","ملح","Mineral"),
                M("Cotton","قطن","Agricultural"), M("Sugarcane","قصب السكر","Agricultural"), M("Sugar beet","بنجر السكر","Agricultural"),
                M("Tomatoes","طماطم","Agricultural"), M("Citrus","موالح","Agricultural"), M("Dates","بلح","Agricultural"),
                M("Olives","زيتون","Agricultural"), M("Wheat","قمح","Agricultural"), M("Flax","كتان","Agricultural"),
                M("Polymers (resin)","راتنج بوليمر","Chemical"), M("Recycled plastic","بلاستيك مُعاد تدويره","Chemical"));
            await db.SaveChangesAsync();
        }

        // 4) demo data (factories, suppliers, demand, startups, investors) → see file 04, milestone M7
    }

    private static Governorate G(string en, string ar, double lat, double lng) => new() { NameEn = en, NameAr = ar, Latitude = lat, Longitude = lng };
    private static RawMaterial M(string en, string ar, string cat) => new() { NameEn = en, NameAr = ar, Category = cat };
}
```

---

## 4. Web

### 4.1 `Web/Services/CurrentUserService.cs`
```csharp
using System.Security.Claims;
using Induva.Application.Abstractions;
namespace Induva.Web.Services;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
```

### 4.2 `Web/Extensions/ResultExtensions.cs`
```csharp
using Induva.Application.Common;
using Microsoft.AspNetCore.Mvc;
namespace Induva.Web.Extensions;

public static class ResultExtensions
{
    public static int ToStatusCode(this ErrorKind k) => k switch
    {
        ErrorKind.Validation => 400,
        ErrorKind.NotFound => 404,
        ErrorKind.Forbidden => 403,
        ErrorKind.RateLimited => 429,
        ErrorKind.AiUnavailable => 503,
        _ => 500
    };

    /// For AJAX endpoints: JSON { error } with the right status code.
    public static IActionResult ToJsonError(this Result r) =>
        new ObjectResult(new { error = r.Error }) { StatusCode = r.Kind.ToStatusCode() };
}
```

### 4.3 `Web/Program.cs`
```csharp
using Induva.Application;
using Induva.Application.Abstractions;
using Induva.Infrastructure;
using Induva.Infrastructure.Seeding;
using Induva.Web.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

await DbInitializer.SeedAsync(app.Services);
app.Run();
```

### 4.4 ViewModel `Web/ViewModels/FactoryFormViewModel.cs`
```csharp
using System.ComponentModel.DataAnnotations;
using Induva.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace Induva.Web.ViewModels;

public class FactoryFormViewModel
{
    public int? Id { get; set; }

    [Required, StringLength(200)] public string Name { get; set; } = "";
    [Required] public Sector Sector { get; set; }
    [Required, Display(Name = "Governorate")] public int GovernorateId { get; set; }
    [StringLength(4000)] public string? Description { get; set; }
    [Range(0, 100), Display(Name = "Current utilization %")] public int UtilizationPercent { get; set; } = 70;
    [Range(-90, 90)] public double? Latitude { get; set; }
    [Range(-180, 180)] public double? Longitude { get; set; }
    public bool IsPublic { get; set; } = true;

    public IEnumerable<SelectListItem> Governorates { get; set; } = [];   // filled by controller, never posted
}
```

### 4.5 Controller `Web/Controllers/FactoriesController.cs`
```csharp
using Induva.Application.Dtos;
using Induva.Application.Services;
using Induva.Domain.Constants;
using Induva.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace Induva.Web.Controllers;

public class FactoriesController(IFactoryService factories, ILookupService lookups) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Index([FromQuery] FactoryFilter filter, CancellationToken ct)
    {
        ViewBag.Governorates = await GovernorateSelectAsync(ct);
        return View(await factories.SearchAsync(filter, ct));
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var r = await factories.GetAsync(id, ct);
        if (!r.Succeeded) return r.Kind == Induva.Application.Common.ErrorKind.NotFound ? NotFound() : Forbid();
        return View(r.Value);
    }

    [Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
    public async Task<IActionResult> Create(CancellationToken ct)
        => View("Form", new FactoryFormViewModel { Governorates = await GovernorateSelectAsync(ct) });

    [HttpPost, Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
    public async Task<IActionResult> Create(FactoryFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) { vm.Governorates = await GovernorateSelectAsync(ct); return View("Form", vm); }

        var r = await factories.CreateAsync(
            new SaveFactoryDto(vm.Name, vm.Sector, vm.GovernorateId, vm.Description, vm.UtilizationPercent, vm.Latitude, vm.Longitude, vm.IsPublic), ct);

        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error!);
            vm.Governorates = await GovernorateSelectAsync(ct);
            return View("Form", vm);
        }
        TempData["Success"] = "Factory created. Add machines, then run the AI analysis.";
        return RedirectToAction(nameof(Details), new { id = r.Value });
    }

    private async Task<IEnumerable<SelectListItem>> GovernorateSelectAsync(CancellationToken ct)
        => (await lookups.GetGovernoratesAsync(ct)).Select(g => new SelectListItem(g.NameEn, g.Id.ToString()));
}
```
`Edit` / `Delete` follow the same shape (`POST` + service call + redirect). All other CRUD controllers (Suppliers, Startups,
Investors, DemandRequests) repeat this pattern — **do not invent new patterns per module**.

### 4.6 AI controller (thin) `Web/Controllers/FactoryAnalysisController.cs`
```csharp
using Induva.Application.AI.Features;
using Induva.Domain.Constants;
using Induva.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Induva.Web.Controllers;

[Authorize(Roles = $"{Roles.FactoryOwner},{Roles.Admin}")]
public class FactoryAnalysisController(IFactoryAnalysisService service) : Controller
{
    [HttpPost("factories/{factoryId:int}/analysis")]
    public async Task<IActionResult> Run(int factoryId, CancellationToken ct)
    {
        var r = await service.AnalyzeAsync(factoryId, ct);
        return r.Succeeded ? PartialView("_FactoryAnalysisCard", r.Value) : r.ToJsonError();
    }
}
```

### 4.7 View snippet `Views/Factories/Details.cshtml` (AI card area)
```cshtml
@model Induva.Application.Dtos.FactoryDetailsDto

<h1>@Model.Name</h1>
<p class="text-muted">@Model.GovernorateName · @Model.Sector · Utilization @Model.UtilizationPercent%</p>

@if (Model.IsOwner)
{
    <div class="card my-3">
      <div class="card-body">
        <h5 class="card-title">AI factory analysis</h5>
        <p class="small text-muted">AI-generated estimate based on the data you entered. Review before relying on it.</p>
        <button class="btn btn-primary"
                data-ai-url="@Url.Action("Run", "FactoryAnalysis", new { factoryId = Model.Id })"
                data-ai-target="#analysis-result">Analyze my factory</button>
        <div id="analysis-result" class="mt-3"></div>
      </div>
    </div>
}
```

### 4.8 Layout additions (`_Layout.cshtml`) + `wwwroot/js/ai-actions.js`
```cshtml
@inject Microsoft.AspNetCore.Antiforgery.IAntiforgery Antiforgery
<meta name="csrf" content="@Antiforgery.GetAndStoreTokens(Context).RequestToken" />
...
<script src="~/js/ai-actions.js" asp-append-version="true"></script>
```
```javascript
// Generic handler for every AI button: <button data-ai-url="..." data-ai-target="#selector">
document.addEventListener('click', async (e) => {
  const btn = e.target.closest('[data-ai-url]');
  if (!btn || btn.disabled) return;
  const target = document.querySelector(btn.dataset.aiTarget);
  const token = document.querySelector('meta[name="csrf"]').content;

  btn.disabled = true;
  target.innerHTML = '<div class="d-flex align-items-center gap-2"><div class="spinner-border spinner-border-sm"></div>' +
                     '<span>Analyzing… this can take up to a minute.</span></div>';
  try {
    const res = await fetch(btn.dataset.aiUrl, {
      method: 'POST',
      headers: { 'RequestVerificationToken': token },
      body: btn.dataset.aiBody ? new URLSearchParams(JSON.parse(btn.dataset.aiBody)) : null
    });
    if (res.ok) {
      target.innerHTML = await res.text();           // server-rendered partial (Razor-encoded)
    } else {
      const err = await res.json().catch(() => ({}));
      const box = document.createElement('div');
      box.className = 'alert alert-warning';
      box.textContent = err.error || 'Something went wrong. Please try again.';   // textContent: never innerHTML for errors
      target.replaceChildren(box);
    }
  } catch {
    const box = document.createElement('div');
    box.className = 'alert alert-warning';
    box.textContent = 'Network error. Please try again.';
    target.replaceChildren(box);
  } finally {
    btn.disabled = false;
  }
});
```

### 4.9 Map: `MapController` + `wwwroot/js/map.js`
```csharp
public class MapController(IMapService map) : Controller
{
    public IActionResult Index() => View();

    [HttpGet("api/map")]
    public async Task<IActionResult> Data([FromQuery] string[]? layers, [FromQuery] int? governorateId, CancellationToken ct)
        => Json(await map.GetMarkersAsync(layers ?? ["factories", "suppliers", "resources", "demand"], governorateId, ct));
}
// MapMarkerDto(string Type, int Id, string Title, string Subtitle, double Lat, double Lng, string Url)
```
```javascript
const map = L.map('map').setView([27.0, 30.8], 6);   // Egypt
L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', { attribution: '© OpenStreetMap contributors', maxZoom: 18 }).addTo(map);

const layers = { factories: L.layerGroup(), suppliers: L.layerGroup(), resources: L.layerGroup(), demand: L.layerGroup() };
Object.values(layers).forEach(l => l.addTo(map));
L.control.layers(null, {
  'Factories': layers.factories, 'Suppliers': layers.suppliers, 'Raw materials': layers.resources, 'Open demand': layers.demand
}).addTo(map);

fetch('/api/map').then(r => r.json()).then(markers => {
  markers.forEach(m => {
    const el = L.marker([m.lat, m.lng]);
    const div = document.createElement('div');
    const b = document.createElement('strong'); b.textContent = m.title;
    const s = document.createElement('div'); s.textContent = m.subtitle;
    const a = document.createElement('a'); a.href = m.url; a.textContent = 'Open';
    div.append(b, s, a);
    el.bindPopup(div);                                  // DOM nodes, not HTML strings → no XSS
    (layers[m.type] ?? layers.factories).addLayer(el);
  });
});
```
Resource markers = governorate centroid + list of that governorate's `GovernorateResource` rows in the subtitle.

---

## 5. Tests (`tests/Induva.Tests`)

### 5.1 Ownership test (pattern for every service)
```csharp
public class FactoryServiceTests
{
    private static (FactoryService svc, ApplicationDbContext db) Create(string? userId, bool admin = false)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(userId); user.IsAuthenticated.Returns(userId != null); user.IsInRole(Roles.Admin).Returns(admin);
        return (new FactoryService(new UnitOfWork(db), user), db);
    }

    [Fact]
    public async Task Update_by_non_owner_is_forbidden()
    {
        var (svc, db) = Create("user-B");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        db.Factories.Add(new Factory { Id = 5, OwnerUserId = "user-A", Name = "F", GovernorateId = 1 });
        await db.SaveChangesAsync();

        var r = await svc.UpdateAsync(5, new SaveFactoryDto("X", Sector.Plastics, 1, null, 50, null, null, true), default);

        Assert.False(r.Succeeded);
        Assert.Equal(ErrorKind.Forbidden, r.Kind);
    }
}
```
(Tests for `JsonExtractor`, `FinancialCalculator`, `RuleBasedScorer`: see file 03 §9.)
