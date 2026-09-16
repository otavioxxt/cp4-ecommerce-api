using CP4.ECommerce.Application.Dtos;
using CP4.ECommerce.Application.Mappers;
using CP4.ECommerce.Domain.Entities;
using Xunit;

namespace CP4.ECommerce.Tests.Unit.APP;

/// <summary>
/// Testes dos Mappers (Extension Methods), garantindo que a conversao
/// DTO -> Entidade continue consistente.
/// </summary>
public class MapperTest
{
    [Fact]
    [Trait("Mapper", "Produtos")]
    public void ToProdutoEntity_DeveConverterTodosOsCampos()
    {
        // Arrange
        var dto = new ProdutoDto("Monitor 27 QHD", "Painel IPS 165Hz", "Monitores", 1899.90m, 12);

        // Act
        var entity = dto.ToProdutoEntity();

        // Assert
        Assert.IsType<ProdutoEntity>(entity);
        Assert.Equal("Monitor 27 QHD", entity.Nome);
        Assert.Equal("Monitores", entity.Categoria);
        Assert.Equal(1899.90m, entity.Preco);
        Assert.Equal(12, entity.Estoque);
        Assert.True(entity.Ativo);
    }

    [Fact]
    [Trait("Mapper", "Produtos")]
    public void ToProdutoEntity_DeveRemoverEspacosEmBranco()
    {
        var dto = new ProdutoDto("   Teclado   ", "  desc  ", "  Perifericos  ", 10m, 1);

        var entity = dto.ToProdutoEntity();

        Assert.Equal("Teclado", entity.Nome);
        Assert.Equal("Perifericos", entity.Categoria);
    }

    [Fact]
    [Trait("Mapper", "Pedidos")]
    public void ToPedidoEntity_DeveGerarNumeroENormalizarOEmail()
    {
        // Arrange
        var dto = new PedidoDto("Otavio Santos", "Otavio@Exemplo.COM", new List<ItemPedidoDto>());

        // Act
        var entity = dto.ToPedidoEntity();

        // Assert
        Assert.StartsWith("PED-", entity.NumeroPedido);
        Assert.Equal("otavio@exemplo.com", entity.ClienteEmail);
        Assert.Equal(StatusPedido.Rascunho, entity.Status);
        Assert.Empty(entity.Itens);
    }

    [Fact]
    [Trait("Mapper", "Pedidos")]
    public void ToItemPedidoEntity_DeveCopiarNomeEPrecoDoProduto()
    {
        // Arrange
        var produto = new ProdutoEntity { Id = 3, Nome = "Headset 7.1", Preco = 359.90m, Estoque = 8 };

        // Act
        var item = new ItemPedidoDto(3, 2).ToItemPedidoEntity(produto);

        // Assert
        Assert.Equal(3, item.ProdutoId);
        Assert.Equal("Headset 7.1", item.ProdutoNome);
        Assert.Equal(2, item.Quantidade);
        Assert.Equal(359.90m, item.PrecoUnitario);
        Assert.Equal(719.80m, item.Subtotal);
    }
}
