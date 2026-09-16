using System.ComponentModel.DataAnnotations;

namespace CP4.ECommerce.Application.Dtos;

/// <summary>
/// Dados de entrada de Pedido (POST).
/// </summary>
public record PedidoDto(
    [Required(ErrorMessage = "O nome do cliente é obrigatório.")]
    [StringLength(150, MinimumLength = 3)]
    string ClienteNome,

    [Required(ErrorMessage = "O e-mail do cliente é obrigatório.")]
    [EmailAddress(ErrorMessage = "O e-mail informado é inválido.")]
    [StringLength(180)]
    string ClienteEmail,

    List<ItemPedidoDto> Itens);

/// <summary>
/// Item informado na criação do pedido ou na inclusão de um novo item.
/// </summary>
public record ItemPedidoDto(
    [Range(1, int.MaxValue, ErrorMessage = "O ProdutoId é obrigatório.")]
    int ProdutoId,

    [Range(1, 1000, ErrorMessage = "A quantidade deve estar entre 1 e 1000.")]
    int Quantidade);
