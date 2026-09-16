using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Domain.Models;

namespace CP4.ECommerce.Domain.Interfaces;

/// <summary>Contrato do repositorio de Produto.</summary>
public interface IProdutoRepository
{
    Task<PageResultModel<IEnumerable<ProdutoEntity>>> ObterTodosAsync(
        int deslocamento = 0, int registroRetornado = 3, string? categoria = null);

    Task<ProdutoEntity?> ObterUmAsync(int id);

    Task<ProdutoEntity?> AdicionarAsync(ProdutoEntity entity);

    Task<ProdutoEntity?> EditarAsync(int id, ProdutoEntity entity);

    Task<ProdutoEntity?> DeletarAsync(int id);

    Task<bool> ExisteNomeAsync(string nome, int? ignorarId = null);
}
