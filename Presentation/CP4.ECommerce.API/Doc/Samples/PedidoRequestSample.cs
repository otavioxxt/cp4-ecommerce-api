using CP4.ECommerce.Application.Dtos;
using Swashbuckle.AspNetCore.Filters;

namespace CP4.ECommerce.API.Doc.Samples;

/// <summary>Exemplo de payload de abertura de pedido.</summary>
public class PedidoRequestSample : IExamplesProvider<PedidoDto>
{
    public PedidoDto GetExamples()
    {
        return new PedidoDto(
            ClienteNome: "Otavio Santos",
            ClienteEmail: "otavio@exemplo.com",
            Itens: new List<ItemPedidoDto>
            {
                new(ProdutoId: 1, Quantidade: 2),
                new(ProdutoId: 10, Quantidade: 1)
            });
    }
}
