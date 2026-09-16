using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Domain.Models;

namespace CP4.ECommerce.Domain.Interfaces;

/// <summary>Contrato do repositorio de Pedido.</summary>
public interface IPedidoRepository
{
    Task<PageResultModel<IEnumerable<PedidoEntity>>> ObterTodosAsync(
        int deslocamento = 0, int registroRetornado = 3, StatusPedido? status = null);

    Task<PedidoEntity?> ObterUmAsync(int id);

    Task<PedidoEntity?> AdicionarAsync(PedidoEntity entity);

    Task<PedidoEntity?> EditarAsync(PedidoEntity entity);

    Task<PedidoEntity?> DeletarAsync(int id);
}
