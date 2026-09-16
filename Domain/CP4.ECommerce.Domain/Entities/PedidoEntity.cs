using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CP4.ECommerce.Domain.Entities;

/// <summary>Pedido do cliente, com os seus itens.</summary>
[Table("tb_pedido")]
[Index(nameof(NumeroPedido), IsUnique = true, Name = "IDX_pedido_numero")]    // Indice unico
[Index(nameof(ClienteEmail), Name = "IDX_pedido_email")]                      // Indice simples
[Index(nameof(Status), nameof(DataPedido), Name = "IDX_pedido_status_data")]  // Indice composto
public class PedidoEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string NumeroPedido { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string ClienteNome { get; set; } = string.Empty;

    [Required]
    [MaxLength(180)]
    public string ClienteEmail { get; set; } = string.Empty;

    public StatusPedido Status { get; set; } = StatusPedido.Rascunho;

    [Precision(18, 2)]
    public decimal ValorTotal { get; set; }

    public DateTime DataPedido { get; set; } = DateTime.Now;

    public ICollection<ItemPedidoEntity> Itens { get; set; } = new List<ItemPedidoEntity>();
}
