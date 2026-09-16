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

/// <summary>Testes funcionais dos endpoints de Pedido.</summary>
public class PedidoControllerTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PedidoControllerTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.PedidoUseCaseMock.Reset();
    }

    private static PedidoEntity NovoPedido(int id = 1, StatusPedido status = StatusPedido.Rascunho)
        => new()
        {
            Id = id,
            NumeroPedido = $"PED-20260916-{id:D4}",
            ClienteNome = "Otavio Santos",
            ClienteEmail = "otavio@exemplo.com",
            Status = status,
            ValorTotal = 500m,
            DataPedido = new DateTime(2026, 9, 16, 10, 0, 0),
            Itens = new List<ItemPedidoEntity>
            {
                new() { Id = 1, PedidoId = id, ProdutoId = 1, ProdutoNome = "Teclado", Quantidade = 2, PrecoUnitario = 250m }
            }
        };

    [Fact(DisplayName = "GET /api/pedido retorna 200 OK com os itens")]
    [Trait("Controller", "Pedidos")]
    public async Task Get_DeveRetornar200()
    {
        _factory.PedidoUseCaseMock
            .Setup(x => x.ObterTodosPedidosAsync(0, 3, null))
            .ReturnsAsync(new PageResultModel<IEnumerable<PedidoEntity>>
            {
                Data = new List<PedidoEntity> { NovoPedido(1), NovoPedido(2) },
                Deslocamento = 0,
                RegistroRetornado = 3,
                TotalRegistros = 15
            });

        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/pedido");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var corpo = await response.Content.ReadAsStringAsync();
        using var documento = JsonDocument.Parse(corpo);

        var data = documento.RootElement.GetProperty("data");
        Assert.Equal(15, data.GetProperty("totalRegistros").GetInt32());

        // O status e serializado como texto, nao como numero.
        var primeiro = data.GetProperty("data")[0];
        Assert.Equal("Rascunho", primeiro.GetProperty("status").GetString());
        Assert.Equal(1, primeiro.GetProperty("itens").GetArrayLength());
    }

    [Fact(DisplayName = "POST /api/pedido retorna 201 Created")]
    [Trait("Controller", "Pedidos")]
    public async Task Post_DeveRetornar201()
    {
        _factory.PedidoUseCaseMock
            .Setup(x => x.AdicionarPedidoAsync(It.IsAny<PedidoDto>()))
            .ReturnsAsync(NovoPedido(16));

        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/pedido", new
        {
            clienteNome = "Otavio Santos",
            clienteEmail = "otavio@exemplo.com",
            itens = new[] { new { produtoId = 1, quantidade = 2 } }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var corpo = await response.Content.ReadAsStringAsync();
        Assert.Contains("PED-", corpo);
        Assert.Contains("Rascunho", corpo);
    }

    [Fact(DisplayName = "POST /api/pedido com e-mail invalido retorna 400 Bad Request")]
    [Trait("Controller", "Pedidos")]
    public async Task Post_ComEmailInvalido_DeveRetornar400()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/pedido", new
        {
            clienteNome = "Otavio Santos",
            clienteEmail = "isso-nao-e-email",
            itens = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _factory.PedidoUseCaseMock.Verify(
            x => x.AdicionarPedidoAsync(It.IsAny<PedidoDto>()), Times.Never);
    }

    [Fact(DisplayName = "POST /api/pedido com estoque insuficiente retorna 400 Bad Request")]
    [Trait("Controller", "Pedidos")]
    public async Task Post_ComEstoqueInsuficiente_DeveRetornar400()
    {
        _factory.PedidoUseCaseMock
            .Setup(x => x.AdicionarPedidoAsync(It.IsAny<PedidoDto>()))
            .ThrowsAsync(new ArgumentException("Estoque insuficiente para o produto 'Teclado'."));

        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/pedido", new
        {
            clienteNome = "Otavio Santos",
            clienteEmail = "otavio@exemplo.com",
            itens = new[] { new { produtoId = 1, quantidade = 99 } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var corpo = await response.Content.ReadAsStringAsync();
        Assert.Contains("Estoque insuficiente", corpo);
    }

    [Fact(DisplayName = "PATCH /api/pedido/{id}/confirmar retorna 200 OK")]
    [Trait("Controller", "Pedidos")]
    public async Task Confirmar_DeveRetornar200()
    {
        _factory.PedidoUseCaseMock
            .Setup(x => x.ConfirmarPedidoAsync(1))
            .ReturnsAsync(NovoPedido(1, StatusPedido.Confirmado));

        using var client = _factory.CreateClient();

        var response = await client.PatchAsync("/api/pedido/1/confirmar", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var corpo = await response.Content.ReadAsStringAsync();
        Assert.Contains("Confirmado", corpo);
    }

    [Fact(DisplayName = "PATCH /api/pedido/{id}/cancelar inexistente retorna 404 Not Found")]
    [Trait("Controller", "Pedidos")]
    public async Task Cancelar_Inexistente_DeveRetornar404()
    {
        _factory.PedidoUseCaseMock
            .Setup(x => x.CancelarPedidoAsync(It.IsAny<int>()))
            .ReturnsAsync((PedidoEntity?)null);

        using var client = _factory.CreateClient();

        var response = await client.PatchAsync("/api/pedido/999/cancelar", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
