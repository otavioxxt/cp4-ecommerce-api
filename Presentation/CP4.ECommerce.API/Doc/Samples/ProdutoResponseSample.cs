using CP4.ECommerce.Domain.Entities;
using Swashbuckle.AspNetCore.Filters;

namespace CP4.ECommerce.API.Doc.Samples;

/// <summary>Exemplo de resposta com um produto.</summary>
public class ProdutoResponseSample : IExamplesProvider<ProdutoEntity>
{
    public ProdutoEntity GetExamples()
    {
        return new ProdutoEntity
        {
            Id = 1,
            Nome = "Teclado Mecanico RGB",
            Descricao = "Switch blue, layout ABNT2, iluminacao RGB",
            Categoria = "Perifericos",
            Preco = 349.90m,
            Estoque = 25,
            Ativo = true,
            DataCadastro = new DateTime(2026, 9, 16, 10, 30, 0)
        };
    }
}
