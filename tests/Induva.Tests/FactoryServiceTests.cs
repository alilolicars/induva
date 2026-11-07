using Induva.Application.Abstractions;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Application.Services;
using Induva.Domain.Constants;
using Induva.Domain.Entities;
using Induva.Domain.Enums;
using Induva.Infrastructure.Persistence;
using Induva.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Induva.Tests;

public class FactoryServiceTests
{
    private static (FactoryService svc, ApplicationDbContext db, ICurrentUserService user) CreateContext(string? userId, bool isAdmin = false)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(userId);
        user.IsAuthenticated.Returns(!string.IsNullOrEmpty(userId));
        user.IsInRole(Roles.Admin).Returns(isAdmin);

        return (new FactoryService(new UnitOfWork(db), user), db, user);
    }

    [Fact]
    public async Task Create_factory_fails_when_unauthenticated()
    {
        var (svc, db, _) = CreateContext(null);

        var r = await svc.CreateAsync(new SaveFactoryDto("F1", Sector.Plastics, 1, null, 50, null, null, true));

        Assert.False(r.Succeeded);
        Assert.Equal(ErrorKind.Forbidden, r.Kind);
    }

    [Fact]
    public async Task Create_factory_succeeds_when_authenticated_and_sets_owner()
    {
        var (svc, db, _) = CreateContext("user-1");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة", Latitude = 30.04, Longitude = 31.23 });
        await db.SaveChangesAsync();

        var r = await svc.CreateAsync(new SaveFactoryDto("Delta Packaging", Sector.Packaging, 1, "Testing", 80, null, null, true));

        Assert.True(r.Succeeded);
        Assert.True(r.Value > 0);

        var created = await db.Factories.FindAsync(r.Value);
        Assert.NotNull(created);
        Assert.Equal("Delta Packaging", created.Name);
        Assert.Equal("user-1", created.OwnerUserId);
        Assert.Equal(80, created.UtilizationPercent);
    }

    [Fact]
    public async Task Update_by_non_owner_is_forbidden()
    {
        var (svc, db, _) = CreateContext("user-intruder");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        db.Factories.Add(new Factory { Id = 10, OwnerUserId = "user-legit", Name = "Real Factory", GovernorateId = 1 });
        await db.SaveChangesAsync();

        var r = await svc.UpdateAsync(10, new SaveFactoryDto("Hacked Name", Sector.Textiles, 1, null, 30, null, null, true));

        Assert.False(r.Succeeded);
        Assert.Equal(ErrorKind.Forbidden, r.Kind);

        var factory = await db.Factories.FindAsync(10);
        Assert.Equal("Real Factory", factory!.Name);
    }

    [Fact]
    public async Task Update_by_owner_succeeds()
    {
        var (svc, db, _) = CreateContext("user-legit");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        db.Factories.Add(new Factory { Id = 10, OwnerUserId = "user-legit", Name = "Original Name", GovernorateId = 1, UtilizationPercent = 50 });
        await db.SaveChangesAsync();

        var r = await svc.UpdateAsync(10, new SaveFactoryDto("Updated Name", Sector.Plastics, 1, "New desc", 90, 30.1, 31.2, true));

        Assert.True(r.Succeeded);
        var updated = await db.Factories.FindAsync(10);
        Assert.Equal("Updated Name", updated!.Name);
        Assert.Equal(90, updated.UtilizationPercent);
    }

    [Fact]
    public async Task Delete_by_non_owner_is_forbidden()
    {
        var (svc, db, _) = CreateContext("user-intruder");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        db.Factories.Add(new Factory { Id = 15, OwnerUserId = "user-owner", Name = "Do Not Delete", GovernorateId = 1 });
        await db.SaveChangesAsync();

        var r = await svc.DeleteAsync(15);

        Assert.False(r.Succeeded);
        Assert.Equal(ErrorKind.Forbidden, r.Kind);
        Assert.NotNull(await db.Factories.FindAsync(15));
    }

    [Fact]
    public async Task Delete_by_admin_succeeds()
    {
        var (svc, db, _) = CreateContext("admin-user", isAdmin: true);
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        db.Factories.Add(new Factory { Id = 20, OwnerUserId = "some-user", Name = "Admin Delete Test", GovernorateId = 1 });
        await db.SaveChangesAsync();

        var r = await svc.DeleteAsync(20);

        Assert.True(r.Succeeded);
        Assert.Null(await db.Factories.FindAsync(20));
    }

    [Fact]
    public async Task Child_entities_respect_ownership()
    {
        var (svc, db, _) = CreateContext("user-hacker");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        db.Factories.Add(new Factory { Id = 30, OwnerUserId = "legit-owner", Name = "Owned Factory", GovernorateId = 1 });
        await db.SaveChangesAsync();

        // Adding machine by non-owner must be forbidden
        var rMachine = await svc.AddMachineAsync(30, new SaveMachineDto("Unauthorized Machine", null, 1, null, null, null));
        Assert.False(rMachine.Succeeded);
        Assert.Equal(ErrorKind.Forbidden, rMachine.Kind);

        // Adding idle window by non-owner must be forbidden
        var rIdle = await svc.AddIdleWindowAsync(30, new SaveIdleWindowDto(1, 3, 50, "Fake"));
        Assert.False(rIdle.Succeeded);
        Assert.Equal(ErrorKind.Forbidden, rIdle.Kind);

        // Adding product by non-owner must be forbidden
        var rProd = await svc.AddProductAsync(30, new SaveFactoryProductDto("Fake Prod", null));
        Assert.False(rProd.Succeeded);
        Assert.Equal(ErrorKind.Forbidden, rProd.Kind);
    }

    [Fact]
    public async Task Search_filters_by_sector_governorate_and_keyword()
    {
        var (svc, db, _) = CreateContext("viewer");
        db.Governorates.AddRange(
            new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" },
            new Governorate { Id = 2, NameEn = "Giza", NameAr = "الجيزة" });

        db.Factories.AddRange(
            new Factory { Id = 1, OwnerUserId = "owner-1", Name = "Alpha Plastic", Sector = Sector.Plastics, GovernorateId = 1, IsPublic = true },
            new Factory { Id = 2, OwnerUserId = "owner-1", Name = "Beta Food", Sector = Sector.FoodProcessing, GovernorateId = 1, IsPublic = true },
            new Factory { Id = 3, OwnerUserId = "owner-2", Name = "Gamma Plastic Giza", Sector = Sector.Plastics, GovernorateId = 2, IsPublic = true },
            new Factory { Id = 4, OwnerUserId = "owner-3", Name = "Private Plastic", Sector = Sector.Plastics, GovernorateId = 1, IsPublic = false });

        await db.SaveChangesAsync();

        // 1. Filter by sector = Plastics (excludes Beta Food, and excludes private)
        var resPlastics = await svc.SearchAsync(new FactoryFilter(Sector: Sector.Plastics));
        Assert.Equal(2, resPlastics.Total);
        Assert.DoesNotContain(resPlastics.Items, f => f.Name == "Private Plastic");
        Assert.DoesNotContain(resPlastics.Items, f => f.Name == "Beta Food");

        // 2. Filter by GovernorateId = 2
        var resGiza = await svc.SearchAsync(new FactoryFilter(GovernorateId: 2));
        Assert.Single(resGiza.Items);
        Assert.Equal("Gamma Plastic Giza", resGiza.Items[0].Name);

        // 3. Search query
        var resAlpha = await svc.SearchAsync(new FactoryFilter(Q: "Alpha"));
        Assert.Single(resAlpha.Items);
        Assert.Equal("Alpha Plastic", resAlpha.Items[0].Name);
    }
}

