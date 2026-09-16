using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Domain.Interfaces;
using CP4.ECommerce.Domain.Models;
using CP4.ECommerce.Infrastructure.Data.AppData;
using Microsoft.EntityFrameworkCore;

namespace CP4.ECommerce.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do Repository Pattern para Produto.
/// Toda a lógica de acesso a dados fica concentrada aqui.
/// </summary>
public class ProdutoRepository : IProdutoRepository
{
    private readonly ApplicationContext _context;

    public ProdutoRepository(ApplicationContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Paginação Offset-Based: Skip (deslocamento) + Take (registros retornados),
    /// executada no banco e não em memória.
    /// </summary>
    public async Task<PageResultModel<IEnumerable<ProdutoEntity>>> ObterTodosAsync(
        int deslocamento = 0, int registroRetornado = 3, string? categoria = null)
    {
        if (deslocamento < 0) deslocamento = 0;
        if (registroRetornado <= 0) registroRetornado = 3;
        if (registroRetornado > 50) registroRetornado = 50;

        var query = _context.Produto.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(categoria))
            query = query.Where(x => x.Categoria == categoria);

        var totalRegistros = await query.CountAsync();

        var result = await query
            .OrderBy(x => x.Id)
            .Skip(deslocamento)
            .Take(registroRetornado)
            .ToListAsync();

        return new PageResultModel<IEnumerable<ProdutoEntity>>
        {
            Data = result,
            Deslocamento = deslocamento,
            RegistroRetornado = registroRetornado,
            TotalRegistros = totalRegistros
        };
    }

    public async Task<ProdutoEntity?> ObterUmAsync(int id)
    {
        var result = await _context.Produto.FindAsync(id);
        return result;
    }

    public async Task<ProdutoEntity?> AdicionarAsync(ProdutoEntity entity)
    {
        _context.Produto.Add(entity);
        await _context.SaveChangesAsync();

        return entity;
    }

    public async Task<ProdutoEntity?> EditarAsync(int id, ProdutoEntity entity)
    {
        var result = await _context.Produto.FindAsync(id);

        if (result is not null)
        {
            result.Nome = entity.Nome;
            result.Descricao = entity.Descricao;
            result.Categoria = entity.Categoria;
            result.Preco = entity.Preco;
            result.Estoque = entity.Estoque;
            result.Ativo = entity.Ativo;

            _context.Update(result);
            await _context.SaveChangesAsync();

            return result;
        }

        return null;
    }

    public async Task<ProdutoEntity?> DeletarAsync(int id)
    {
        var result = await _context.Produto.FindAsync(id);

        if (result is not null)
        {
            // Exclusão lógica: preserva o histórico dos pedidos já realizados.
            result.Ativo = false;

            _context.Update(result);
            await _context.SaveChangesAsync();

            return result;
        }

        return null;
    }

    public async Task<bool> ExisteNomeAsync(string nome, int? ignorarId = null)
    {
        var termo = (nome ?? string.Empty).Trim().ToLower();

        // CountAsync em vez de AnyAsync: o AnyAsync gera "THEN True ELSE False",
        // e o Oracle nao tem literais booleanos (ORA-00904).
        var total = await _context.Produto
            .AsNoTracking()
            .CountAsync(x => x.Nome.ToLower() == termo && (ignorarId == null || x.Id != ignorarId));

        return total > 0;
    }
}
