using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Infrastructure.Data.AppData;
using CP4.ECommerce.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CP4.ECommerce.Tests.Unit.APP;

/// <summary>
/// Testes do repositorio de Pedido com o EF Core InMemory.
/// </summary>
public class PedidoRepositoryTest
{
    private readonly ApplicationContext _applicationContext;
    private readonly PedidoRepository _pedidoRepository;

    public PedidoRepositoryTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: $"TestDatabase_Pedido_{Guid.NewGuid()}")
            .Options;

        _applicationContext = new ApplicationContext(options);

        _applicationContext.Database.EnsureDeleted();
        _applicationContext.Database.EnsureCreated();

        _pedidoRepository = new PedidoRepository(_applicationContext);
    }

    private static PedidoEntity NovoPedido(string numero, StatusPedido status, int quantidade, decimal preco)
    {
        var pedido = new PedidoEntity
        {
            NumeroPedido = numero,
            ClienteNome = "Cliente de Teste",
            ClienteEmail = "cliente@exemplo.com",
            Status = status,
            DataPedido = DateTime.Now,
            Itens = new List<ItemPedidoEntity>
            {
                new() { ProdutoId = 1, ProdutoNome = "Teclado", Quantidade = quantidade, PrecoUnitario = preco }
            }
        };

        pedido.ValorTotal = pedido.Itens.Sum(i => i.Quantidade * i.PrecoUnitario);

        return pedido;
    }

    [Fact]
    [Trait("Repository", "Pedidos")]
    public async Task Adicionar_DeveGravarOPedidoComOsItens()
    {
        // Arrange
        var pedido = NovoPedido("PED-001", StatusPedido.Rascunho, 2, 150m);

        // Act
        var resultado = await _pedidoRepository.AdicionarAsync(pedido);

        // Assert
        Assert.NotNull(resultado);
        Assert.True(resultado!.Id > 0);
        Assert.Equal(300m, resultado.ValorTotal);

        var pedidoNoDb = _applicationContext.Pedido
            .Include(p => p.Itens)
            .First(p => p.Id == resultado.Id);

        Assert.Single(pedidoNoDb.Itens);
    }

    [Fact]
    [Trait("Repository", "Pedidos")]
    public async Task ObterUm_DeveCarregarOsItensDoPedido()
    {
        // Arrange
        var pedido = NovoPedido("PED-002", StatusPedido.Rascunho, 3, 300m);

        _applicationContext.Pedido.Add(pedido);
        await _applicationContext.SaveChangesAsync();

        // Act
        var resultado = await _pedidoRepository.ObterUmAsync(pedido.Id);

        // Assert
        Assert.NotNull(resultado);
        Assert.Single(resultado!.Itens);
        Assert.Equal("Teclado", resultado.Itens.First().ProdutoNome);
        Assert.Equal(900m, resultado.Itens.First().Subtotal);
    }

    [Fact]
    [Trait("Repository", "Pedidos")]
    public async Task ObterTodos_ComFiltroDeStatus_DeveFiltrar()
    {
        // Arrange
        _applicationContext.Pedido.AddRange(
            NovoPedido("PED-010", StatusPedido.Confirmado, 1, 100m),
            NovoPedido("PED-011", StatusPedido.Confirmado, 1, 90m),
            NovoPedido("PED-012", StatusPedido.Rascunho, 1, 1200m));

        await _applicationContext.SaveChangesAsync();

        // Act
        var resultado = await _pedidoRepository.ObterTodosAsync(0, 10, StatusPedido.Confirmado);

        // Assert
        Assert.Equal(2, resultado.TotalRegistros);
        Assert.All(resultado.Data, pedido => Assert.Equal(StatusPedido.Confirmado, pedido.Status));
    }

    [Fact]
    [Trait("Repository", "Pedidos")]
    public async Task Editar_DeveAtualizarOStatus()
    {
        // Arrange
        var pedido = NovoPedido("PED-020", StatusPedido.Rascunho, 1, 100m);

        _applicationContext.Pedido.Add(pedido);
        await _applicationContext.SaveChangesAsync();

        // Act
        pedido.Status = StatusPedido.Confirmado;
        var resultado = await _pedidoRepository.EditarAsync(pedido);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(StatusPedido.Confirmado, _applicationContext.Pedido.First(p => p.Id == pedido.Id).Status);
    }
}
