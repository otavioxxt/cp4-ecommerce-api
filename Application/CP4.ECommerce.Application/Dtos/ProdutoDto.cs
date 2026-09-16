using System.ComponentModel.DataAnnotations;

namespace CP4.ECommerce.Application.Dtos;

/// <summary>
/// Dados de entrada de Produto (POST e PUT).
/// Record: imutável e com igualdade por valor.
/// As validações ficam nos parâmetros do construtor posicional — é onde o
/// ASP.NET Core lê os metadados de validação de um record.
/// </summary>
public record ProdutoDto(
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 120 caracteres.")]
    string Nome,

    [StringLength(400)]
    string Descricao,

    [Required(ErrorMessage = "A categoria é obrigatória.")]
    [StringLength(60)]
    string Categoria,

    [Range(0.01, 999999.99, ErrorMessage = "O preço deve ser maior que zero.")]
    decimal Preco,

    [Range(0, int.MaxValue, ErrorMessage = "O estoque não pode ser negativo.")]
    int Estoque);
