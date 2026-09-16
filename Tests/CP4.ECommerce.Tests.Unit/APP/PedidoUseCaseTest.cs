using CP4.ECommerce.Application.Dtos;
using CP4.ECommerce.Application.UseCases;
using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CP4.ECommerce.Tests.Unit.APP;

/// <summary>
/// Testes do PedidoUseCase com os dois repositorios mockados. Cobrem a baixa
/// de estoque, o preco gravado na venda, as transicoes de status e a
/// reposicao de estoque no cancelamento.
/// </summary>
public class PedidoUseCaseTest
{
    private readonly Mock<IPedidoRepository> _pedidoRepository;
    private readonly Mock<IProdutoRepository> _produtoRepository;
    private readonly PedidoUseCase _pedidoUseCase;

    public PedidoUseCaseTest()
    {
        _pedidoRepository = new Mock<IPedidoRepository>();
        _produtoRepository = new Mock<IProdutoRepository>();

        _pedidoUseCase = new PedidoUseCase(
            _pedidoRepository.Object,
            _produtoRepository.Object,
            NullLogger<PedidoUseCase>.Instance);

        // Os repositorios devolvem a propria entidade recebida.
        _pedidoRepository
            .Setup(obj => obj.AdicionarAsync(It.IsAny<PedidoEntity>()))
            .Returns((PedidoEntity e) => Task.FromResult<PedidoEntity?>(e));

        _pedidoRepository
            .Setup(obj => obj.EditarAsync(It.IsAny<PedidoEntity>()))
            .Returns((PedidoEntity e) => Task.FromResult<PedidoEntity?>(e));

        _produtoRepository
            .Setup(obj => obj.EditarAsync(It.IsAny<int>(), It.IsAny<ProdutoEntity>()))
            .Returns((int _, ProdutoEntity e) => Task.FromResult<ProdutoEntity?>(e));
    }

    private static ProdutoEntity NovoProduto(int id, decimal preco = 100m, int estoque = 50, bool ativo = true)
        => new()
        {
            Id = id,
            Nome = $"Produto {id}",
            Categoria = "Perifericos",
            Preco = preco,
            Estoque = estoque,
            Ativo = ativo
        };

    private void ConfigurarProduto(ProdutoEntity produto)
        => _produtoRepository
            .Setup(obj => obj.ObterUmAsync(produto.Id))
            .Returns(Task.FromResult<ProdutoEntity?>(produto));

    private void ConfigurarPedido(PedidoEntity pedido)
        => _pedidoRepository
            .Setup(obj => obj.ObterUmAsync(pedido.Id))
            .Returns(Task.FromResult<PedidoEntity?>(pedido));

    [Fact]
    [Trait("UseCase", "Pedidos")]
    public async Task AdicionarPedido_DeveCalcularOTotalEBaixarOEstoque()
    {
        // Arrange
        var produto = NovoProduto(1, preco: 250m, estoque: 10);
        ConfigurarProduto(produto);

        var dto = new PedidoDto("Otavio Santos", "otavio@exemplo.com",
            new List<ItemPedidoDto> { new(1, 2) });

        // Act
        var resultado = await _pedidoUseCase.AdicionarPedidoAsync(dto);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(StatusPedido.Rascunho, resultado!.Status);
        Assert.Equal(500m, resultado.ValorTotal);
        Assert.Single(resultado.Itens);
        Assert.Equal(8, produto.Estoque);
        Assert.StartsWith("PED-", resultado.NumeroPedido);
    }

    [Fact]
    [Trait("UseCase", "Pedidos")]
    public async Task AdicionarPedido_DeveGravarOPrecoDoMomentoDaVenda()
    {
        // Arrange
        var produto = NovoProduto(1, preco: 100m, estoque: 10);
        ConfigurarProduto(produto);

        var dto = new PedidoDto("Cliente", "cliente@exemplo.com",
            new List<ItemPedidoDto> { new(1, 2) });

        // Act
        var resultado = await _pedidoUseCase.AdicionarPedidoAsync(dto);

        // O catalogo reajusta o preco depois da venda.
        produto.Preco = 999m;

        // Assert
        Assert.Equal(100m, resultado!.Itens.First().PrecoUnitario);
        Assert.Equal(200m, resultado.ValorTotal);
    }

    [Fact]
    [Trait("UseCase", "Pedidos")]
    public async Task AdicionarPedido_ComEstoqueInsuficiente_DeveLancarExcecao()
    {
        // Arrange
        ConfigurarProduto(NovoProduto(1, estoque: 2));

        var dto = new PedidoDto("Cliente", "cliente@exemplo.com",
            new List<ItemPedidoDto> { new(1, 5) });

        // Act + Assert
        var excecao = await Assert.ThrowsAsync<ArgumentException>(
            () => _pedidoUseCase.AdicionarPedidoAsync(dto));

        Assert.Contains("Estoque insuficiente", excecao.Message);
        _pedidoRepository.Verify(obj => obj.AdicionarAsync(It.IsAny<PedidoEntity>()), Times.Never);
    }

    [Fact]
    [Trait("UseCase", "Pedidos")]
    public async Task AdicionarItem_ComProdutoRepetido_DeveSomarAQuantidade()
    {
        // Arrange
        var produto = NovoProduto(1, preco: 80m, estoque: 20);
        ConfigurarProduto(produto);

        var pedido = new PedidoEntity
        {
            Id = 10,
            Status = StatusPedido.Rascunho,
            Itens = new List<ItemPedidoEntity>
            {
                new() { ProdutoId = 1, ProdutoNome = "Produto 1", Quantidade = 2, PrecoUnitario = 80m }
            }
        };
        ConfigurarPedido(pedido);

        // Act
        var resultado = await _pedidoUseCase.AdicionarItemAsync(10, new ItemPedidoDto(1, 3));

        // Assert
        Assert.NotNull(resultado);
        Assert.Single(resultado!.Itens);
        Assert.Equal(5, resultado.Itens.First().Quantidade);
        Assert.Equal(400m, resultado.ValorTotal);
        Assert.Equal(17, produto.Estoque);
    }

    [Fact]
    [Trait("UseCase", "Pedidos")]
    public async Task ConfirmarPedido_SemItens_DeveLancarExcecao()
    {
        var pedido = new PedidoEntity { Id = 4, Status = StatusPedido.Rascunho };
        ConfigurarPedido(pedido);

        var excecao = await Assert.ThrowsAsync<ArgumentException>(
            () => _pedidoUseCase.ConfirmarPedidoAsync(4));

        Assert.Contains("sem itens", excecao.Message);
        Assert.Equal(StatusPedido.Rascunho, pedido.Status);
    }

    [Fact]
    [Trait("UseCase", "Pedidos")]
    public async Task CancelarPedido_DeveReporOEstoque()
    {
        // Arrange
        var produto = NovoProduto(1, preco: 100m, estoque: 6);
        ConfigurarProduto(produto);

        var pedido = new PedidoEntity
        {
            Id = 6,
            Status = StatusPedido.Confirmado,
            ValorTotal = 400m,
            Itens = new List<ItemPedidoEntity>
            {
                new() { ProdutoId = 1, ProdutoNome = "Produto 1", Quantidade = 4, PrecoUnitario = 100m }
            }
        };
        ConfigurarPedido(pedido);

        // Act
        var resultado = await _pedidoUseCase.CancelarPedidoAsync(6);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(StatusPedido.Cancelado, resultado!.Status);
        Assert.Equal(10, produto.Estoque);
    }

    [Fact]
    [Trait("UseCase", "Pedidos")]
    public async Task ObterTodosPedidos_ComStatusInvalido_DeveLancarExcecao()
    {
        var excecao = await Assert.ThrowsAsync<ArgumentException>(
            () => _pedidoUseCase.ObterTodosPedidosAsync(0, 3, "NaoExiste"));

        Assert.Contains("Rascunho", excecao.Message);
    }
}
