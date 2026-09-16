using CP4.ECommerce.Application.Interfaces;
using CP4.ECommerce.Application.UseCases;
using CP4.ECommerce.Domain.Interfaces;
using CP4.ECommerce.Infrastructure.Data.AppData;
using CP4.ECommerce.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CP4.ECommerce.Infrastructure.Ioc;

/// <summary>
/// Injecao de dependencia: contexto, repositorios e casos de uso.
/// </summary>
public static class Bootstrap
{
    public static IServiceCollection ConfigureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Oracle")
                               ?? throw new InvalidOperationException(
                                   "A connection string 'Oracle' nao foi configurada no appsettings.json.");

        services.AddDbContext<ApplicationContext>(options => options.UseOracle(connectionString));

        services.AddTransient<IProdutoRepository, ProdutoRepository>();
        services.AddTransient<IPedidoRepository, PedidoRepository>();

        services.AddTransient<IProdutoUseCase, ProdutoUseCase>();
        services.AddTransient<IPedidoUseCase, PedidoUseCase>();

        return services;
    }
}
