using System.Text.Json.Serialization;

namespace CP4.ECommerce.Domain.Entities;

/// <summary>
/// Situacao do pedido. O JsonStringEnumConverter faz o status aparecer
/// como texto ("Confirmado") no JSON, em vez do numero.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusPedido
{
    Rascunho = 0,
    Confirmado = 1,
    Enviado = 2,
    Entregue = 3,
    Cancelado = 4
}
