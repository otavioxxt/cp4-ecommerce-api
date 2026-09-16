using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Domain.Interfaces;
using CP4.ECommerce.Domain.Models;
using CP4.ECommerce.Infrastructure.Data.AppData;
using Microsoft.EntityFrameworkCore;

namespace CP4.ECommerce.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do Repository Pattern para Pedido.
/// </summary>
public class PedidoRepository : IPedidoRepository
{
    private readonly ApplicationContext _context;

    public PedidoRepository(ApplicationContext context)
    {
        _context = context;
    }

    public async Task<PageResultModel<IEnumerable<PedidoEntity>>> ObterTodosAsync(
        int deslocamento = 0, int registroRetornado = 3, StatusPedido? status = null)
    {
        if (deslocamento < 0) deslocamento = 0;
        if (registroRetornado <= 0) registroRetornado = 3;
        if (registroRetornado > 50) registroRetornado = 50;

        var query = _context.Pedido
            .AsNoTracking()
            .Include(x => x.Itens)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var totalRegistros = await query.CountAsync();

        var result = await query
            .OrderByDescending(x => x.Id)
            .Skip(deslocamento)
            .Take(registroRetornado)
            .ToListAsync();

        return new PageResultModel<IEnumerable<PedidoEntity>>
        {
            Data = result,
            Deslocamento = deslocamento,
            RegistroRetornado = registroRetornado,
            TotalRegistros = totalRegistros
        };
    }

    public async Task<PedidoEntity?> ObterUmAsync(int id)
    {
        var result = await _context.Pedido
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.Id == id);

        return result;
    }

    public async Task<PedidoEntity?> AdicionarAsync(PedidoEntity entity)
    {
        _context.Pedido.Add(entity);
        await _context.SaveChangesAsync();

        return entity;
    }

    public async Task<PedidoEntity?> EditarAsync(PedidoEntity entity)
    {
        _context.Pedido.Update(entity);
        await _context.SaveChangesAsync();

        return entity;
    }

    public async Task<PedidoEntity?> DeletarAsync(int id)
    {
        var result = await _context.Pedido
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (result is not null)
        {
            _context.Remove(result);
            await _context.SaveChangesAsync();

            return result;
        }

        return null;
    }
}
