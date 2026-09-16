using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Domain.Models;
using CP4.ECommerce.Tests.Functional.Setup;
using Moq;
using Xunit;

namespace CP4.ECommerce.Tests.Functional.APP;

/// <summary>
/// Testes da infraestrutura da API: compressao de dados, health check e
/// documentacao Swagger.
/// </summary>
public class InfraestruturaTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public InfraestruturaTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ProdutoUseCaseMock.Reset();

        _factory.ProdutoUseCaseMock
            .Setup(x => x.ObterTodosProdutosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
            .ReturnsAsync(new PageResultModel<IEnumerable<ProdutoEntity>>
            {
                Data = Enumerable.Range(1, 50).Select(i => new ProdutoEntity
                {
                    Id = i,
                    Nome = $"Produto numero {i}",
                    Descricao = "Descricao longa o suficiente para a compressao fazer diferenca no payload.",
                    Categoria = "Perifericos",
                    Preco = 100m + i,
                    Estoque = i,
                    Ativo = true
                }).ToList(),
                Deslocamento = 0,
                RegistroRetornado = 50,
                TotalRegistros = 50
            });
    }

    [Fact(DisplayName = "Compressao: Accept-Encoding br devolve Content-Encoding br")]
    [Trait("Infra", "Compressao")]
    public async Task Compressao_ComBrotli_DeveComprimirAResposta()
    {
        using var client = _factory.CreateDefaultClient();

        var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/produto?registroRetornado=50");
        requisicao.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));

        var response = await client.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("br", response.Content.Headers.ContentEncoding);
    }

    [Fact(DisplayName = "Compressao: Accept-Encoding gzip devolve Content-Encoding gzip")]
    [Trait("Infra", "Compressao")]
    public async Task Compressao_ComGzip_DeveComprimirAResposta()
    {
        using var client = _factory.CreateDefaultClient();

        var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/produto?registroRetornado=50");
        requisicao.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        var response = await client.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("gzip", response.Content.Headers.ContentEncoding);
    }

    [Fact(DisplayName = "GET /api/health/live retorna 200 OK e status Healthy")]
    [Trait("Infra", "HealthCheck")]
    public async Task HealthLive_DeveRetornar200()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var corpo = await response.Content.ReadAsStringAsync();
        using var documento = JsonDocument.Parse(corpo);

        Assert.Equal("Healthy", documento.RootElement.GetProperty("status").GetString());

        var checks = documento.RootElement.GetProperty("checks").EnumerateArray().ToList();
        Assert.Single(checks);
        Assert.Equal("self", checks[0].GetProperty("name").GetString());
    }

    [Fact(DisplayName = "Swagger expoe os endpoints e as annotations")]
    [Trait("Infra", "Swagger")]
    public async Task Swagger_DeveExporADocumentacao()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var corpo = await response.Content.ReadAsStringAsync();
        using var documento = JsonDocument.Parse(corpo);

        var paths = documento.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/produto", out _));
        Assert.True(paths.TryGetProperty("/api/pedido", out _));
        Assert.True(paths.TryGetProperty("/api/health/live", out _));

        // Conteudo vindo das Swagger Annotations
        Assert.Contains("Listar produtos paginados", corpo);
        Assert.Contains("Cadastrar produto", corpo);
    }
}
