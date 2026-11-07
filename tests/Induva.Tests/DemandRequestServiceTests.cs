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

public class DemandRequestServiceTests
{
    private static (DemandRequestService svc, ApplicationDbContext db, ICurrentUserService user) CreateContext(string? userId, bool isAdmin = false)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(userId);
        user.IsAuthenticated.Returns(!string.IsNullOrEmpty(userId));
        user.IsInRole(Roles.Admin).Returns(isAdmin);

        return (new DemandRequestService(new UnitOfWork(db), user), db, user);
    }

    [Fact]
    public async Task Create_sets_requester_user_id()
    {
        var (svc, db, _) = CreateContext("buyer-1");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        await db.SaveChangesAsync();

        var r = await svc.CreateAsync(new SaveDemandRequestDto(
            "Procurement of 10,000 pallets",
            "Wooden Heavy Pallets",
            Sector.Packaging,
            ["cnc-machining"],
            10000,
            "pallets",
            350,
            DateTime.UtcNow.AddMonths(2),
            1,
            false,
            "Open",
            "Must adhere to ISPM 15 heat treatment"));

        Assert.True(r.Succeeded);
        var created = await db.DemandRequests.FindAsync(r.Value);
        Assert.NotNull(created);
        Assert.Equal("buyer-1", created.RequesterUserId);
        Assert.Equal("Open", created.Status);
    }

    [Fact]
    public async Task Close_and_delete_enforce_ownership()
    {
        var (svc, db, _) = CreateContext("outsider");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        db.DemandRequests.Add(new DemandRequest
        {
            Id = 12,
            RequesterUserId = "real-buyer",
            Title = "Real RFQ",
            ProductName = "Steel Pipes",
            Sector = Sector.Metals,
            Quantity = 100,
            Unit = "tons",
            GovernorateId = 1,
            Status = "Open"
        });
        await db.SaveChangesAsync();

        var rClose = await svc.CloseAsync(12);
        Assert.False(rClose.Succeeded);
        Assert.Equal(ErrorKind.Forbidden, rClose.Kind);

        var rDelete = await svc.DeleteAsync(12);
        Assert.False(rDelete.Succeeded);
        Assert.Equal(ErrorKind.Forbidden, rDelete.Kind);
    }
}
