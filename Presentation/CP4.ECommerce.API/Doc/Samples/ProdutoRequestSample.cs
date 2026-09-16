using CP4.ECommerce.Application.Dtos;
using Swashbuckle.AspNetCore.Filters;

namespace CP4.ECommerce.API.Doc.Samples;

/// <summary>Exemplo de payload de entrada de produto exibido no Swagger.</summary>
public class ProdutoRequestSample : IExamplesProvider<ProdutoDto>
{
    public ProdutoDto GetExamples()
    {
        return new ProdutoDto(
            Nome: "Teclado Mecanico RGB",
            Descricao: "Switch blue, layout ABNT2, iluminacao RGB",
            Categoria: "Perifericos",
            Preco: 349.90m,
            Estoque: 25);
    }
}
