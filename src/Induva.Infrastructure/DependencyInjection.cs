using Induva.Application.Abstractions;
using Induva.Application.AI;
using Induva.Infrastructure.AI;
using Induva.Infrastructure.Email;
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
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<ApplicationDbContext>(o =>
            o.UseSqlServer(cfg.GetConnectionString("Default")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddIdentity<ApplicationUser, IdentityRole>(o =>
        {
            o.Password.RequiredLength = 8;
            o.Password.RequireNonAlphanumeric = false;
            o.User.RequireUniqueEmail = true;
            o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            o.Lockout.MaxFailedAccessAttempts = 5;
            o.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // SMTP Email Service
        services.Configure<SmtpOptions>(cfg.GetSection("Smtp"));
        services.AddTransient<IEmailService, SmtpEmailService>();

        // AI configuration and gate
        services.Configure<AiOptions>(cfg.GetSection("Ai"));
        services.PostConfigure<AiOptions>(o => o.ApiKey ??= cfg["SBG_API_KEY"]);
        services.AddSingleton<AiConcurrencyGate>();

        if (string.Equals(cfg["Ai:Mode"], "Demo", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IAiClient, DemoAiClient>();
        }
        else
        {
            services.AddHttpClient<IAiClient, ItiAiClient>((sp, c) =>
            {
                var o = sp.GetRequiredService<IOptions<AiOptions>>().Value;
                if (string.IsNullOrWhiteSpace(o.ApiKey))
                {
                    throw new InvalidOperationException("AI API key missing. Set user-secret Ai:ApiKey or env var SBG_API_KEY (or use Ai:Mode=Demo).");
                }

                c.BaseAddress = new Uri(o.BaseUrl);
                c.Timeout = Timeout.InfiniteTimeSpan; // Resilience handler owns timeouts
                c.DefaultRequestHeaders.Authorization = new("Bearer", o.ApiKey);
            })
            .AddStandardResilienceHandler(r =>
            {
                r.AttemptTimeout.Timeout = TimeSpan.FromSeconds(120);
                r.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(300);
                r.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(240); // must be >= 2 x AttemptTimeout
                r.Retry.MaxRetryAttempts = 1; // LLM calls are slow; retry once only
            });
        }

        return services;
    }
}
