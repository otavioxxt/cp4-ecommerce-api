using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CP4.ECommerce.Domain.Entities;

/// <summary>Item de um pedido.</summary>
[Table("tb_item_pedido")]
[Index(nameof(PedidoId), Name = "IDX_item_pedido")]                                       // Indice simples
[Index(nameof(PedidoId), nameof(ProdutoId), IsUnique = true, Name = "IDX_item_produto")]  // Indice composto
public class ItemPedidoEntity
{
    [Key]
    public int Id { get; set; }

    public int PedidoId { get; set; }

    public int ProdutoId { get; set; }

    [Required]
    [MaxLength(120)]
    public string ProdutoNome { get; set; } = string.Empty;

    public int Quantidade { get; set; }

    // O preco e gravado no momento da venda, para que reajustes no
    // catalogo nao alterem o historico do pedido.
    [Precision(18, 2)]
    public decimal PrecoUnitario { get; set; }

    [NotMapped]
    public decimal Subtotal => Quantidade * PrecoUnitario;
}
