using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CP4.ECommerce.Domain.Entities;

/// <summary>Produto do catalogo.</summary>
[Table("tb_produto")]
[Index(nameof(Nome), IsUnique = true, Name = "IDX_produto_nome")]                 // Indice unico
[Index(nameof(Categoria), Name = "IDX_produto_categoria")]                        // Indice simples
[Index(nameof(Categoria), nameof(Ativo), Name = "IDX_produto_categoria_ativo")]   // Indice composto
public class ProdutoEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(400)]
    public string Descricao { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    public string Categoria { get; set; } = string.Empty;

    [Precision(18, 2)]
    public decimal Preco { get; set; }

    public int Estoque { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime DataCadastro { get; set; } = DateTime.Now;
}
