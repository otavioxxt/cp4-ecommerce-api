using CP4.ECommerce.Domain.Entities;
using Swashbuckle.AspNetCore.Filters;

namespace CP4.ECommerce.API.Doc.Samples;

/// <summary>Exemplo de resposta com um pedido completo.</summary>
public class PedidoResponseSample : IExamplesProvider<PedidoEntity>
{
    public PedidoEntity GetExamples()
    {
        return new PedidoEntity
        {
            Id = 16,
            NumeroPedido = "PED-20260916104500-A1B2C3",
            ClienteNome = "Otavio Santos",
            ClienteEmail = "otavio@exemplo.com",
            Status = StatusPedido.Rascunho,
            ValorTotal = 2599.70m,
            DataPedido = new DateTime(2026, 9, 16, 10, 45, 0),
            Itens = new List<ItemPedidoEntity>
            {
                new()
                {
                    Id = 40,
                    PedidoId = 16,
                    ProdutoId = 1,
                    ProdutoNome = "Teclado Mecanico RGB",
                    Quantidade = 2,
                    PrecoUnitario = 349.90m
                },
                new()
                {
                    Id = 41,
                    PedidoId = 16,
                    ProdutoId = 10,
                    ProdutoNome = "Monitor 27 QHD",
                    Quantidade = 1,
                    PrecoUnitario = 1899.90m
                }
            }
        };
    }
}
