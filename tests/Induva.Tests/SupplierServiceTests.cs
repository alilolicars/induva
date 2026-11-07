using Induva.Application.Abstractions;
using Induva.Application.Common;
using Induva.Application.Dtos;
using Induva.Application.Services;
using Induva.Domain.Constants;
using Induva.Domain.Entities;
using Induva.Infrastructure.Persistence;
using Induva.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Induva.Tests;

public class SupplierServiceTests
{
    private static (SupplierService svc, ApplicationDbContext db, ICurrentUserService user) CreateContext(string? userId, bool isAdmin = false)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var user = Substitute.For<ICurrentUserService>();
        user.UserId.Returns(userId);
        user.IsAuthenticated.Returns(!string.IsNullOrEmpty(userId));
        user.IsInRole(Roles.Admin).Returns(isAdmin);

        return (new SupplierService(new UnitOfWork(db), user), db, user);
    }

    [Fact]
    public async Task Create_supplier_succeeds_for_authenticated_user()
    {
        var (svc, db, _) = CreateContext("sup-1");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Suez", NameAr = "السويس" });
        await db.SaveChangesAsync();

        var r = await svc.CreateAsync(new SaveSupplierDto("Sinai Minerals", 1, "High purity raw gypsum", 29.9, 32.5, true));

        Assert.True(r.Succeeded);
        var created = await db.Suppliers.FindAsync(r.Value);
        Assert.NotNull(created);
        Assert.Equal("Sinai Minerals", created.CompanyName);
        Assert.Equal("sup-1", created.OwnerUserId);
    }

    [Fact]
    public async Task Update_by_non_owner_is_forbidden()
    {
        var (svc, db, _) = CreateContext("hacker");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        db.Suppliers.Add(new Supplier { Id = 10, CompanyName = "Real Supplier", OwnerUserId = "real-owner", GovernorateId = 1 });
        await db.SaveChangesAsync();

        var r = await svc.UpdateAsync(10, new SaveSupplierDto("Hijacked", 1, null, null, null, true));

        Assert.False(r.Succeeded);
        Assert.Equal(ErrorKind.Forbidden, r.Kind);
    }

    [Fact]
    public async Task Add_and_remove_material_respects_ownership()
    {
        var (svc, db, _) = CreateContext("intruder");
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        db.RawMaterials.Add(new RawMaterial { Id = 5, NameEn = "Silica Sand", NameAr = "رمل السيليكا", Category = "Minerals" });
        db.Suppliers.Add(new Supplier { Id = 20, CompanyName = "Glass Feedstock Co", OwnerUserId = "owner-20", GovernorateId = 1 });
        await db.SaveChangesAsync();

        var rAdd = await svc.AddMaterialAsync(20, new SaveSupplierMaterialDto(5, 50, "Spot price"));
        Assert.False(rAdd.Succeeded);
        Assert.Equal(ErrorKind.Forbidden, rAdd.Kind);
    }

    [Fact]
    public async Task Delete_by_admin_succeeds()
    {
        var (svc, db, _) = CreateContext("admin-user", isAdmin: true);
        db.Governorates.Add(new Governorate { Id = 1, NameEn = "Cairo", NameAr = "القاهرة" });
        db.Suppliers.Add(new Supplier { Id = 30, CompanyName = "Old Supplier", OwnerUserId = "user-1", GovernorateId = 1 });
        await db.SaveChangesAsync();

        var r = await svc.DeleteAsync(30);

        Assert.True(r.Succeeded);
        Assert.Null(await db.Suppliers.FindAsync(30));
    }
}

