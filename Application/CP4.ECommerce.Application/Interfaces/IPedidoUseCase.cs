using CP4.ECommerce.Application.Dtos;
using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Domain.Models;

namespace CP4.ECommerce.Application.Interfaces;

/// <summary>Casos de uso de Pedido.</summary>
public interface IPedidoUseCase
{
    Task<PageResultModel<IEnumerable<PedidoEntity>>> ObterTodosPedidosAsync(
        int deslocamento = 0, int registroRetornado = 3, string? status = null);

    Task<PedidoEntity?> ObterUmPedidoAsync(int id);

    Task<PedidoEntity?> AdicionarPedidoAsync(PedidoDto dto);

    Task<PedidoEntity?> AdicionarItemAsync(int pedidoId, ItemPedidoDto dto);

    Task<PedidoEntity?> ConfirmarPedidoAsync(int pedidoId);

    Task<PedidoEntity?> CancelarPedidoAsync(int pedidoId);
}
