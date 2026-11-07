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

public class StartupServiceTests
{
    private static (StartupService svc, ApplicationDbContext db, ICurrentUserService user) CreateContext(string? userId, bool isAdmin = false)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(userId);
        user.IsAuthenticated.Returns(!string.IsNullOrEmpty(userId));
        user.IsInRole(Roles.Admin).Returns(isAdmin);

        return (new StartupService(new UnitOfWork(db), user), db, user);
    }

    [Fact]
    public async Task Create_startup_sets_current_user_as_owner()
    {
        var (svc, db, _) = CreateContext("founder-1");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Alexandria", NameAr = "الإسكندرية" });
        await db.SaveChangesAsync();

        var r = await svc.CreateAsync(new SaveStartupDto(
            "EcoPack Tech",
            Sector.Packaging,
            StartupStage.Prototype,
            "Biodegradable cassava packaging",
            "Cassava starch thermoforming process",
            350000,
            ["thermoforming", "extrusion"],
            1));

        Assert.True(r.Succeeded);
        var created = await db.Startups.FindAsync(r.Value);
        Assert.NotNull(created);
        Assert.Equal("founder-1", created.OwnerUserId);
        Assert.Equal("EcoPack Tech", created.Name);
        Assert.Contains("thermoforming", created.RequiredTagsCsv);
    }

    [Fact]
    public async Task Non_owner_cannot_modify_or_delete_startup()
    {
        var (svc, db, _) = CreateContext("outsider");
        db.Startups.Add(new Startup
        {
            Id = 5,
            Name = "Original Startup",
            OwnerUserId = "actual-founder",
            ProductIdea = "Solar Stirling engine",
            Sector = Sector.Electronics,
            Stage = StartupStage.EarlyRevenue
        });
        await db.SaveChangesAsync();

        var rUpdate = await svc.UpdateAsync(5, new SaveStartupDto(
            "Defaced Startup", Sector.Electronics, StartupStage.EarlyRevenue, null, "Hacked", 0, [], null));
        Assert.False(rUpdate.Succeeded);
        Assert.Equal(ErrorKind.Forbidden, rUpdate.Kind);

        var rDelete = await svc.DeleteAsync(5);
        Assert.False(rDelete.Succeeded);
        Assert.Equal(ErrorKind.Forbidden, rDelete.Kind);
    }
}
