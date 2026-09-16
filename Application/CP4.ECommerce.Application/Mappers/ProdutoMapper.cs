using CP4.ECommerce.Application.Dtos;
using CP4.ECommerce.Domain.Entities;

namespace CP4.ECommerce.Application.Mappers;

/// <summary>
/// Mapeamento DTO -> Entidade usando Extension Methods, que estendem o
/// comportamento do DTO sem modificar a classe.
/// </summary>
public static class ProdutoMapper
{
    public static ProdutoEntity ToProdutoEntity(this ProdutoDto obj)
    {
        return new ProdutoEntity
        {
            Nome = obj.Nome.Trim(),
            Descricao = (obj.Descricao ?? string.Empty).Trim(),
            Categoria = obj.Categoria.Trim(),
            Preco = obj.Preco,
            Estoque = obj.Estoque,
            Ativo = true,
            DataCadastro = DateTime.Now
        };
    }
}
