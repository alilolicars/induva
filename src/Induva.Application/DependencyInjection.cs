using Induva.Application.AI;
using Induva.Application.AI.Features;
using Induva.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Induva.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IFactoryService, FactoryService>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IStartupService, StartupService>();
        services.AddScoped<IInvestorService, InvestorService>();
        services.AddScoped<IDemandRequestService, DemandRequestService>();
        services.AddScoped<IConnectionService, ConnectionService>();
        services.AddScoped<IMapService, MapService>();
        services.AddScoped<IGovernorateResourceService, GovernorateResourceService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();

        services.AddScoped<IAiJsonRunner, AiJsonRunner>();
        services.AddScoped<IFactoryAnalysisService, FactoryAnalysisService>();
        services.AddScoped<IProductSuggestionService, ProductSuggestionService>();
        services.AddScoped<IDemandForecastService, DemandForecastService>();
        services.AddScoped<IMatchingService, MatchingService>();
        services.AddScoped<IInvestmentService, InvestmentService>();

        return services;
    }
}
