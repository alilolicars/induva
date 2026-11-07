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

public class InvestorServiceTests
{
    private static (InvestorService svc, ApplicationDbContext db, ICurrentUserService user) CreateContext(string? userId, bool isAdmin = false)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(userId);
        user.IsAuthenticated.Returns(!string.IsNullOrEmpty(userId));
        user.IsInRole(Roles.Admin).Returns(isAdmin);

        return (new InvestorService(new UnitOfWork(db), user), db, user);
    }

    [Fact]
    public async Task Save_creates_or_updates_profile_for_current_user()
    {
        var (svc, db, _) = CreateContext("investor-1");

        // First save creates profile
        var rCreate = await svc.SaveAsync(new SaveInvestorProfileDto(
            "Nile Ventures",
            [Sector.Plastics, Sector.Chemicals],
            500000,
            5000000,
            [StartupStage.Prototype, StartupStage.Growth],
            "Focused on circular industrial economy"));

        Assert.True(rCreate.Succeeded);
        var profile = await db.InvestorProfiles.FindAsync(rCreate.Value);
        Assert.NotNull(profile);
        Assert.Equal("investor-1", profile.OwnerUserId);
        Assert.Equal("Nile Ventures", profile.DisplayName);

        // Second save updates the existing profile
        var rUpdate = await svc.SaveAsync(new SaveInvestorProfileDto(
            "Nile Ventures Industrial",
            [Sector.Plastics],
            1000000,
            10000000,
            [StartupStage.Growth],
            "Updated mandate"));

        Assert.True(rUpdate.Succeeded);
        Assert.Equal(rCreate.Value, rUpdate.Value);

        var updated = await db.InvestorProfiles.FindAsync(rCreate.Value);
        Assert.Equal("Nile Ventures Industrial", updated!.DisplayName);
        Assert.Equal(10000000, updated.MaxTicketEgp);
    }

    [Fact]
    public async Task Delete_by_non_owner_is_forbidden()
    {
        var (svc, db, _) = CreateContext("intruder");
        db.InvestorProfiles.Add(new InvestorProfile
        {
            Id = 8,
            OwnerUserId = "real-investor",
            DisplayName = "Delta Angel Network"
        });
        await db.SaveChangesAsync();

        var r = await svc.DeleteAsync(8);
        Assert.False(r.Succeeded);
        Assert.Equal(ErrorKind.Forbidden, r.Kind);
    }
}
