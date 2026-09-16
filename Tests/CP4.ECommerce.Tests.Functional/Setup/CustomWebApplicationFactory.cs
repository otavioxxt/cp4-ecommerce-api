using CP4.ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace CP4.ECommerce.Tests.Functional.Setup;

/// <summary>
/// Sobe a API em memoria com o WebApplicationFactory e troca os UseCases
/// por Mocks, para os testes percorrerem todo o ciclo da requisicao sem
/// depender do banco Oracle.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IProdutoUseCase> ProdutoUseCaseMock { get; } = new();

    public Mock<IPedidoUseCase> PedidoUseCaseMock { get; } = new();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Configuracao usada pelos testes.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Oracle", "User Id=teste;Password=teste;Data Source=localhost:1521/XE;");
        Environment.SetEnvironmentVariable("Database__InicializarNaSubida", "false");
        Environment.SetEnvironmentVariable("ApplicationInsights__Habilitado", "false");

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Cria o Mock do IProdutoUseCase
            services.RemoveAll(typeof(IProdutoUseCase));
            services.AddSingleton(ProdutoUseCaseMock.Object);

            // Cria o Mock do IPedidoUseCase
            services.RemoveAll(typeof(IPedidoUseCase));
            services.AddSingleton(PedidoUseCaseMock.Object);
        });
    }
}
