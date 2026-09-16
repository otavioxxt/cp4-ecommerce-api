namespace CP4.ECommerce.Domain.Models;

/// <summary>
/// Retorno paginado. Usa generics para assumir o tipo do objeto que for passado.
/// </summary>
public class PageResultModel<T>
{
    public required T Data { get; set; }

    public int Deslocamento { get; set; }

    public int RegistroRetornado { get; set; }

    public int TotalRegistros { get; set; }
}
