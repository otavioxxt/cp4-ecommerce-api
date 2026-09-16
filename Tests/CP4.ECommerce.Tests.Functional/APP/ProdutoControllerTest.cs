using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CP4.ECommerce.Application.Dtos;
using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Domain.Models;
using CP4.ECommerce.Tests.Functional.Setup;
using Moq;
using Xunit;

namespace CP4.ECommerce.Tests.Functional.APP;

/// <summary>Testes funcionais dos endpoints de Produto.</summary>
public class ProdutoControllerTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProdutoControllerTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;

        // O xUnit cria uma instancia da classe por teste; o Reset evita que
        // um teste herde a configuracao do anterior.
        _factory.ProdutoUseCaseMock.Reset();
    }

    [Fact(DisplayName = "GET /api/produto retorna 200 OK com paginacao e HATEOAS")]
    [Trait("Controller", "Produtos")]
    public async Task Get_DeveRetornar200()
    {
        // Arrange
        _factory.ProdutoUseCaseMock
            .Setup(x => x.ObterTodosProdutosAsync(0, 3, null))
            .ReturnsAsync(new PageResultModel<IEnumerable<ProdutoEntity>>
            {
                Data = new List<ProdutoEntity>
                {
                    new() { Id = 1, Nome = "Teclado", Categoria = "Perifericos", Preco = 150m, Estoque = 30, Ativo = true },
                    new() { Id = 2, Nome = "Mouse", Categoria = "Perifericos", Preco = 90m, Estoque = 25, Ativo = true }
                },
                Deslocamento = 0,
                RegistroRetornado = 3,
                TotalRegistros = 48
            });

        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/produto");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var corpo = await response.Content.ReadAsStringAsync();
        using var documento = JsonDocument.Parse(corpo);

        var data = documento.RootElement.GetProperty("data");
        Assert.Equal(48, data.GetProperty("totalRegistros").GetInt32());
        Assert.Equal(2, data.GetProperty("data").GetArrayLength());

        var links = documento.RootElement.GetProperty("links");
        Assert.True(links.TryGetProperty("self", out _));
        Assert.True(links.TryGetProperty("getById", out _));
    }

    [Fact(DisplayName = "GET /api/produto sem registros retorna 204 No Content")]
    [Trait("Controller", "Produtos")]
    public async Task Get_SemRegistros_DeveRetornar204()
    {
        _factory.ProdutoUseCaseMock
            .Setup(x => x.ObterTodosProdutosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
            .ReturnsAsync(new PageResultModel<IEnumerable<ProdutoEntity>>
            {
                Data = new List<ProdutoEntity>(),
                Deslocamento = 0,
                RegistroRetornado = 3,
                TotalRegistros = 0
            });

        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/produto");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact(DisplayName = "GET /api/produto/{id} inexistente retorna 404 Not Found")]
    [Trait("Controller", "Produtos")]
    public async Task GetById_Inexistente_DeveRetornar404()
    {
        _factory.ProdutoUseCaseMock
            .Setup(x => x.ObterUmProdutoAsync(It.IsAny<int>()))
            .ReturnsAsync((ProdutoEntity?)null);

        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/produto/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "POST /api/produto retorna 201 Created com Location")]
    [Trait("Controller", "Produtos")]
    public async Task Post_DeveRetornar201()
    {
        _factory.ProdutoUseCaseMock
            .Setup(x => x.AdicionarProdutoAsync(It.IsAny<ProdutoDto>()))
            .ReturnsAsync(new ProdutoEntity
            {
                Id = 49,
                Nome = "Teclado Mecanico RGB",
                Categoria = "Perifericos",
                Preco = 349.90m,
                Estoque = 25,
                Ativo = true
            });

        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/produto", new
        {
            nome = "Teclado Mecanico RGB",
            descricao = "Switch blue",
            categoria = "Perifericos",
            preco = 349.90m,
            estoque = 25
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var corpo = await response.Content.ReadAsStringAsync();
        Assert.Contains("Teclado Mecanico RGB", corpo);
    }

    [Fact(DisplayName = "POST /api/produto com payload invalido retorna 400 Bad Request")]
    [Trait("Controller", "Produtos")]
    public async Task Post_ComPayloadInvalido_DeveRetornar400()
    {
        using var client = _factory.CreateClient();

        // Nome curto e preco zerado: barrados pelas DataAnnotations do DTO.
        var response = await client.PostAsJsonAsync("/api/produto", new
        {
            nome = "ab",
            descricao = "invalido",
            categoria = "Perifericos",
            preco = 0m,
            estoque = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _factory.ProdutoUseCaseMock.Verify(
            x => x.AdicionarProdutoAsync(It.IsAny<ProdutoDto>()), Times.Never);
    }

    [Fact(DisplayName = "DELETE /api/produto/{id} retorna 200 OK com o produto inativado")]
    [Trait("Controller", "Produtos")]
    public async Task Delete_DeveRetornar200()
    {
        _factory.ProdutoUseCaseMock
            .Setup(x => x.DeletarProdutoAsync(1))
            .ReturnsAsync(new ProdutoEntity { Id = 1, Nome = "Teclado", Ativo = false });

        using var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/produto/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var corpo = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"ativo\":false", corpo);
    }
}
