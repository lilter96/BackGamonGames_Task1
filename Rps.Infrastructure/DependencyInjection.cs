using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rps.Application.Interfaces;
using Rps.Infrastructure.Persistence;
using Rps.Infrastructure.Services;

namespace Rps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<RpsDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("PostgresConnection"));
        });

        services.AddScoped<IGameService, GameService>();
        services.AddScoped<ITransactionService, TransactionService>();

        return services;
    }
}
