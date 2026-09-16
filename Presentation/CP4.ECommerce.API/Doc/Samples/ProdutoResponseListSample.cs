using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Domain.Models;
using Swashbuckle.AspNetCore.Filters;

namespace CP4.ECommerce.API.Doc.Samples;

/// <summary>Exemplo de resposta paginada da listagem de produtos.</summary>
public class ProdutoResponseListSample : IExamplesProvider<PageResultModel<IEnumerable<ProdutoEntity>>>
{
    public PageResultModel<IEnumerable<ProdutoEntity>> GetExamples()
    {
        var produtos = new List<ProdutoEntity>
        {
            new()
            {
                Id = 9,
                Nome = "Monitor 24 144Hz",
                Descricao = "Monitor 24 144Hz - linha monitores, garantia de 12 meses.",
                Categoria = "Monitores",
                Preco = 1299.90m,
                Estoque = 34,
                Ativo = true,
                DataCadastro = new DateTime(2026, 9, 16, 10, 30, 0)
            },
            new()
            {
                Id = 10,
                Nome = "Monitor 27 QHD",
                Descricao = "Monitor 27 QHD - linha monitores, garantia de 12 meses.",
                Categoria = "Monitores",
                Preco = 1899.90m,
                Estoque = 12,
                Ativo = true,
                DataCadastro = new DateTime(2026, 9, 16, 10, 30, 0)
            },
            new()
            {
                Id = 11,
                Nome = "Monitor Ultrawide 34",
                Descricao = "Monitor Ultrawide 34 - linha monitores, garantia de 12 meses.",
                Categoria = "Monitores",
                Preco = 3499.00m,
                Estoque = 5,
                Ativo = true,
                DataCadastro = new DateTime(2026, 9, 16, 10, 30, 0)
            }
        };

        return new PageResultModel<IEnumerable<ProdutoEntity>>
        {
            Data = produtos,
            Deslocamento = 0,
            RegistroRetornado = 3,
            TotalRegistros = 48
        };
    }
}
