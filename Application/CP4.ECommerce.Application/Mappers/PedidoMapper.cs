using CP4.ECommerce.Application.Dtos;
using CP4.ECommerce.Domain.Entities;

namespace CP4.ECommerce.Application.Mappers;

/// <summary>Mapeamento DTO -> Entidade do Pedido e dos seus itens.</summary>
public static class PedidoMapper
{
    public static PedidoEntity ToPedidoEntity(this PedidoDto obj)
    {
        return new PedidoEntity
        {
            NumeroPedido = GerarNumeroPedido(),
            ClienteNome = obj.ClienteNome.Trim(),
            ClienteEmail = obj.ClienteEmail.Trim().ToLower(),
            Status = StatusPedido.Rascunho,
            ValorTotal = 0m,
            DataPedido = DateTime.Now,
            Itens = new List<ItemPedidoEntity>()
        };
    }

    public static ItemPedidoEntity ToItemPedidoEntity(this ItemPedidoDto obj, ProdutoEntity produto)
    {
        return new ItemPedidoEntity
        {
            ProdutoId = produto.Id,
            ProdutoNome = produto.Nome,
            Quantidade = obj.Quantidade,
            PrecoUnitario = produto.Preco
        };
    }

    private static string GerarNumeroPedido()
        => $"PED-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
}
