using Induva.Domain.Constants;
using Induva.Domain.Entities;
using Induva.Infrastructure.Identity;
using Induva.Infrastructure.Persistence;
using Induva.Infrastructure.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Induva.Tests;

public class DbInitializerTests
{
    private static IServiceProvider BuildServiceProvider(string dbName)
    {
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(o =>
            o.UseInMemoryDatabase(dbName));

        services.AddIdentity<ApplicationUser, IdentityRole>(o =>
        {
            o.Password.RequiredLength = 8;
            o.Password.RequireNonAlphanumeric = false;
            o.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:AdminEmail"] = "admin@induva.local",
                ["Seed:AdminPassword"] = "Admin123!"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Seeder_seeds_lookups_roles_and_admin_and_is_idempotent()
    {
        var sp = BuildServiceProvider(Guid.NewGuid().ToString());

        // First run
        await DbInitializer.SeedAsync(sp);

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Check roles
            foreach (var r in Roles.All)
            {
                Assert.True(await roleManager.RoleExistsAsync(r), $"Role {r} should exist");
            }

            // Check admin
            var admin = await userManager.FindByEmailAsync("admin@induva.local");
            Assert.NotNull(admin);
            Assert.True(await userManager.IsInRoleAsync(admin, Roles.Admin));

            // Check lookups
            Assert.Equal(27, await db.Governorates.CountAsync());
            Assert.Equal(23, await db.RawMaterials.CountAsync());
        }

        // Second run: must be completely idempotent and not throw or duplicate rows
        await DbInitializer.SeedAsync(sp);

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(27, await db.Governorates.CountAsync());
            Assert.Equal(23, await db.RawMaterials.CountAsync());
        }
    }
}

