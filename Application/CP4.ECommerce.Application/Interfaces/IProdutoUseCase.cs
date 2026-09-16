using CP4.ECommerce.Application.Dtos;
using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Domain.Models;

namespace CP4.ECommerce.Application.Interfaces;

/// <summary>Casos de uso de Produto.</summary>
public interface IProdutoUseCase
{
    Task<PageResultModel<IEnumerable<ProdutoEntity>>> ObterTodosProdutosAsync(
        int deslocamento = 0, int registroRetornado = 3, string? categoria = null);

    Task<ProdutoEntity?> ObterUmProdutoAsync(int id);

    Task<ProdutoEntity?> AdicionarProdutoAsync(ProdutoDto dto);

    Task<ProdutoEntity?> EditarProdutoAsync(int id, ProdutoDto dto);

    Task<ProdutoEntity?> DeletarProdutoAsync(int id);
}
