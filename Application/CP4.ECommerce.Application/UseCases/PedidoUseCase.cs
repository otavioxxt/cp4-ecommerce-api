using CP4.ECommerce.Application.Dtos;
using CP4.ECommerce.Application.Interfaces;
using CP4.ECommerce.Application.Mappers;
using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Domain.Interfaces;
using CP4.ECommerce.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CP4.ECommerce.Application.UseCases;

/// <summary>
/// Casos de uso de Pedido. Usa os dois repositorios porque incluir um item
/// baixa o estoque do produto e cancelar o pedido devolve esse estoque.
/// </summary>
public class PedidoUseCase : IPedidoUseCase
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly ILogger<PedidoUseCase> _logger;

    public PedidoUseCase(
        IPedidoRepository pedidoRepository,
        IProdutoRepository produtoRepository,
        ILogger<PedidoUseCase> logger)
    {
        _pedidoRepository = pedidoRepository;
        _produtoRepository = produtoRepository;
        _logger = logger;
    }

    public async Task<PageResultModel<IEnumerable<PedidoEntity>>> ObterTodosPedidosAsync(
        int deslocamento = 0, int registroRetornado = 3, string? status = null)
    {
        try
        {
            _logger.LogInformation(
                "Obtendo pedidos do repositório. Deslocamento {Deslocamento}, registros {RegistroRetornado}, status {Status}",
                deslocamento, registroRetornado, status);

            return await _pedidoRepository.ObterTodosAsync(deslocamento, registroRetornado, ConverterStatus(status));
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao obter a lista de pedidos");
            throw;
        }
    }

    public async Task<PedidoEntity?> ObterUmPedidoAsync(int id)
    {
        _logger.LogInformation("Obtendo o pedido {PedidoId} do repositório", id);

        var pedido = await _pedidoRepository.ObterUmAsync(id);

        if (pedido is null)
            _logger.LogWarning("Pedido {PedidoId} não encontrado", id);

        return pedido;
    }

    public async Task<PedidoEntity?> AdicionarPedidoAsync(PedidoDto dto)
    {
        if (dto is null)
            throw new ArgumentException("Os dados do pedido são obrigatórios.");

        if (string.IsNullOrWhiteSpace(dto.ClienteNome))
            throw new ArgumentException("O nome do cliente é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.ClienteEmail) || !dto.ClienteEmail.Contains('@'))
            throw new ArgumentException("O e-mail do cliente é inválido.");

        var pedido = dto.ToPedidoEntity();

        foreach (var item in dto.Itens ?? new List<ItemPedidoDto>())
        {
            var produto = await _produtoRepository.ObterUmAsync(item.ProdutoId)
                          ?? throw new ArgumentException($"O produto {item.ProdutoId} não foi encontrado.");

            BaixarEstoque(produto, item.Quantidade);

            pedido.Itens.Add(item.ToItemPedidoEntity(produto));

            await _produtoRepository.EditarAsync(produto.Id, produto);
        }

        pedido.ValorTotal = pedido.Itens.Sum(i => i.Quantidade * i.PrecoUnitario);

        var criado = await _pedidoRepository.AdicionarAsync(pedido);

        _logger.LogInformation(
            "Pedido {PedidoId} ({NumeroPedido}) criado com valor total {ValorTotal}",
            criado?.Id, criado?.NumeroPedido, criado?.ValorTotal);

        return criado;
    }

    public async Task<PedidoEntity?> AdicionarItemAsync(int pedidoId, ItemPedidoDto dto)
    {
        var pedido = await _pedidoRepository.ObterUmAsync(pedidoId);

        if (pedido is null)
        {
            _logger.LogWarning("Pedido {PedidoId} não encontrado para inclusão de item", pedidoId);
            return null;
        }

        if (pedido.Status != StatusPedido.Rascunho)
            throw new ArgumentException($"O pedido não pode ser alterado no status {pedido.Status}.");

        var produto = await _produtoRepository.ObterUmAsync(dto.ProdutoId)
                      ?? throw new ArgumentException($"O produto {dto.ProdutoId} não foi encontrado.");

        BaixarEstoque(produto, dto.Quantidade);

        var existente = pedido.Itens.FirstOrDefault(i => i.ProdutoId == produto.Id);

        if (existente is not null)
            existente.Quantidade += dto.Quantidade;
        else
            pedido.Itens.Add(dto.ToItemPedidoEntity(produto));

        pedido.ValorTotal = pedido.Itens.Sum(i => i.Quantidade * i.PrecoUnitario);

        await _produtoRepository.EditarAsync(produto.Id, produto);

        _logger.LogInformation(
            "Item adicionado ao pedido {PedidoId}. Produto {ProdutoId}, quantidade {Quantidade}",
            pedidoId, dto.ProdutoId, dto.Quantidade);

        return await _pedidoRepository.EditarAsync(pedido);
    }

    public async Task<PedidoEntity?> ConfirmarPedidoAsync(int pedidoId)
    {
        var pedido = await _pedidoRepository.ObterUmAsync(pedidoId);

        if (pedido is null)
        {
            _logger.LogWarning("Pedido {PedidoId} não encontrado para confirmação", pedidoId);
            return null;
        }

        if (pedido.Status != StatusPedido.Rascunho)
            throw new ArgumentException($"Somente pedidos em rascunho podem ser confirmados. Status atual: {pedido.Status}.");

        if (pedido.Itens.Count == 0)
            throw new ArgumentException("Não é possível confirmar um pedido sem itens.");

        pedido.Status = StatusPedido.Confirmado;

        _logger.LogInformation("Pedido {PedidoId} confirmado com valor total {ValorTotal}", pedidoId, pedido.ValorTotal);

        return await _pedidoRepository.EditarAsync(pedido);
    }

    public async Task<PedidoEntity?> CancelarPedidoAsync(int pedidoId)
    {
        var pedido = await _pedidoRepository.ObterUmAsync(pedidoId);

        if (pedido is null)
        {
            _logger.LogWarning("Pedido {PedidoId} não encontrado para cancelamento", pedidoId);
            return null;
        }

        if (pedido.Status == StatusPedido.Entregue)
            throw new ArgumentException("Um pedido já entregue não pode ser cancelado.");

        if (pedido.Status == StatusPedido.Cancelado)
            throw new ArgumentException("Este pedido já está cancelado.");

        pedido.Status = StatusPedido.Cancelado;

        // Devolve ao estoque as unidades que haviam sido reservadas.
        foreach (var item in pedido.Itens)
        {
            var produto = await _produtoRepository.ObterUmAsync(item.ProdutoId);

            if (produto is null)
                continue;

            produto.Estoque += item.Quantidade;
            await _produtoRepository.EditarAsync(produto.Id, produto);
        }

        _logger.LogWarning("Pedido {PedidoId} cancelado e estoque reposto", pedidoId);

        return await _pedidoRepository.EditarAsync(pedido);
    }

    /// <summary>Regra de estoque: nao vende mais do que existe nem produto inativo.</summary>
    private static void BaixarEstoque(ProdutoEntity produto, int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("A quantidade do item deve ser maior que zero.");

        if (!produto.Ativo)
            throw new ArgumentException($"O produto '{produto.Nome}' está inativo e não pode ser vendido.");

        if (quantidade > produto.Estoque)
            throw new ArgumentException(
                $"Estoque insuficiente para o produto '{produto.Nome}'. Disponível: {produto.Estoque}, solicitado: {quantidade}.");

        produto.Estoque -= quantidade;
    }

    private static StatusPedido? ConverterStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        if (!Enum.TryParse<StatusPedido>(status, ignoreCase: true, out var convertido))
            throw new ArgumentException(
                $"Status '{status}' inválido. Valores aceitos: {string.Join(", ", Enum.GetNames<StatusPedido>())}.");

        return convertido;
    }
}
