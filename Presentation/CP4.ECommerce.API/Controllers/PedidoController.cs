using CP4.ECommerce.API.Doc.Samples;
using CP4.ECommerce.Application.Dtos;
using CP4.ECommerce.Application.Interfaces;
using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace CP4.ECommerce.API.Controllers;

[Route("api/pedido")]
[ApiController]
public class PedidoController : ControllerBase
{
    private readonly IPedidoUseCase _pedidoUseCase;
    private readonly ILogger<PedidoController> _logger;

    public PedidoController(IPedidoUseCase pedidoUseCase, ILogger<PedidoController> logger)
    {
        _pedidoUseCase = pedidoUseCase;
        _logger = logger;
    }

    /// <summary>Listar pedidos paginados</summary>
    [HttpGet]
    [EnableRateLimiting("rateLimitePolicy")]
    [SwaggerOperation(
        Summary = "Listar pedidos paginados",
        Description = """
        Retorna os pedidos com **paginacao Offset-Based** e os itens de cada pedido.

        ### Parametros
        * **deslocamento / registroRetornado:** controlam a pagina retornada.
        * **status:** filtro opcional - `Rascunho`, `Confirmado`, `Enviado`, `Entregue` ou `Cancelado`.
          Atendido pelo indice composto `IDX_pedido_status_data`.
        """
    )]
    [SwaggerResponse(statusCode: 200, description: "Lista retornada com sucesso", type: typeof(PageResultModel<IEnumerable<PedidoEntity>>))]
    [SwaggerResponse(statusCode: 204, description: "Nao possui pedidos para os filtros informados")]
    [SwaggerResponse(statusCode: 400, description: "Status informado e invalido")]
    public async Task<IActionResult> Get(int deslocamento = 0, int registroRetornado = 3, string? status = null)
    {
        try
        {
            var result = await _pedidoUseCase.ObterTodosPedidosAsync(deslocamento, registroRetornado, status);

            if (!result.Data.Any())
                return NoContent();

            var id = result.Data.FirstOrDefault()?.Id ?? 0;

            var hateoas = new
            {
                data = result,
                links = new
                {
                    self = Url.Action(nameof(Get), "Pedido", null),
                    getById = Url.Action(nameof(GetById), "Pedido", new { id }),
                    post = Url.Action(nameof(Post), "Pedido", null),
                    confirmar = Url.Action(nameof(Confirmar), "Pedido", new { id }),
                    cancelar = Url.Action(nameof(Cancelar), "Pedido", new { id })
                }
            };

            return Ok(hateoas);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>Obter pedido por ID</summary>
    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "Obter pedido por ID",
        Description = "Retorna o pedido completo, com todos os itens e o valor total calculado."
    )]
    [SwaggerResponse(statusCode: 200, description: "Pedido encontrado", type: typeof(PedidoEntity))]
    [SwaggerResponse(statusCode: 404, description: "Pedido nao encontrado")]
    [SwaggerResponseExample(statusCode: 200, typeof(PedidoResponseSample))]
    public async Task<IActionResult> GetById(
        [FromRoute, SwaggerParameter("Identificador unico do pedido")] int id)
    {
        var result = await _pedidoUseCase.ObterUmPedidoAsync(id);

        if (result is null)
            return NotFound();

        var hateoas = new
        {
            data = result,
            links = new
            {
                self = Url.Action(nameof(GetById), "Pedido", new { id }),
                get = Url.Action(nameof(Get), "Pedido", null),
                adicionarItem = Url.Action(nameof(AdicionarItem), "Pedido", new { id }),
                confirmar = Url.Action(nameof(Confirmar), "Pedido", new { id }),
                cancelar = Url.Action(nameof(Cancelar), "Pedido", new { id })
            }
        };

        return Ok(hateoas);
    }

    /// <summary>Abrir pedido</summary>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Abrir pedido",
        Description = """
        Cria um pedido no status **Rascunho**. Os itens sao opcionais na abertura.

        ### Regras de negocio
        * Cada item **baixa o estoque** do produto correspondente.
        * O **preco unitario e gravado** no momento da venda.
        * Estoque insuficiente ou produto inativo resultam em `400 Bad Request`.
        * O **valor total** e calculado pelo UseCase, nunca enviado pelo cliente.
        """
    )]
    [SwaggerRequestExample(typeof(PedidoDto), typeof(PedidoRequestSample))]
    [SwaggerResponse(statusCode: 201, description: "Pedido criado com sucesso", type: typeof(PedidoEntity))]
    [SwaggerResponse(statusCode: 400, description: "Payload invalido, produto inexistente ou estoque insuficiente")]
    [SwaggerResponseExample(statusCode: 201, typeof(PedidoResponseSample))]
    public async Task<IActionResult> Post(PedidoDto entity)
    {
        try
        {
            var result = await _pedidoUseCase.AdicionarPedidoAsync(entity);

            return CreatedAtAction(nameof(GetById), new { id = result?.Id }, result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha ao abrir pedido: {Mensagem}", ex.Message);
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>Adicionar item ao pedido</summary>
    [HttpPost("{id}/item")]
    [SwaggerOperation(
        Summary = "Adicionar item ao pedido",
        Description = """
        Inclui um item no pedido, baixa o estoque do produto e recalcula o valor total.
        Se o produto ja estiver no pedido, apenas soma a quantidade.

        Somente pedidos no status **Rascunho** aceitam alteracao de itens.
        """
    )]
    [SwaggerResponse(statusCode: 200, description: "Item adicionado com sucesso", type: typeof(PedidoEntity))]
    [SwaggerResponse(statusCode: 400, description: "Estoque insuficiente ou pedido nao editavel")]
    [SwaggerResponse(statusCode: 404, description: "Pedido nao encontrado")]
    public async Task<IActionResult> AdicionarItem(
        [FromRoute, SwaggerParameter("Identificador unico do pedido")] int id,
        ItemPedidoDto entity)
    {
        try
        {
            var result = await _pedidoUseCase.AdicionarItemAsync(id, entity);

            if (result is null)
                return NotFound();

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>Confirmar pedido</summary>
    [HttpPatch("{id}/confirmar")]
    [SwaggerOperation(
        Summary = "Confirmar pedido",
        Description = """
        Muda o status do pedido de **Rascunho** para **Confirmado**.

        ### Validacoes
        1. O pedido precisa existir.
        2. O status atual precisa ser `Rascunho`.
        3. O pedido precisa ter ao menos um item.
        """
    )]
    [SwaggerResponse(statusCode: 200, description: "Pedido confirmado", type: typeof(PedidoEntity))]
    [SwaggerResponse(statusCode: 400, description: "Transicao invalida ou pedido sem itens")]
    [SwaggerResponse(statusCode: 404, description: "Pedido nao encontrado")]
    public async Task<IActionResult> Confirmar(
        [FromRoute, SwaggerParameter("Identificador unico do pedido")] int id)
    {
        try
        {
            var result = await _pedidoUseCase.ConfirmarPedidoAsync(id);

            if (result is null)
                return NotFound();

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>Cancelar pedido</summary>
    [HttpPatch("{id}/cancelar")]
    [SwaggerOperation(
        Summary = "Cancelar pedido",
        Description = "Cancela o pedido e **devolve ao estoque** as unidades reservadas. Um pedido ja Entregue nao pode ser cancelado."
    )]
    [SwaggerResponse(statusCode: 200, description: "Pedido cancelado e estoque reposto", type: typeof(PedidoEntity))]
    [SwaggerResponse(statusCode: 400, description: "Pedido ja entregue ou ja cancelado")]
    [SwaggerResponse(statusCode: 404, description: "Pedido nao encontrado")]
    public async Task<IActionResult> Cancelar(
        [FromRoute, SwaggerParameter("Identificador unico do pedido")] int id)
    {
        try
        {
            var result = await _pedidoUseCase.CancelarPedidoAsync(id);

            if (result is null)
                return NotFound();

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }
}
