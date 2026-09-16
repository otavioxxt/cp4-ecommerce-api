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

[Route("api/produto")]
[ApiController]
public class ProdutoController : ControllerBase
{
    private readonly IProdutoUseCase _produtoUseCase;
    private readonly ILogger<ProdutoController> _logger;

    public ProdutoController(IProdutoUseCase produtoUseCase, ILogger<ProdutoController> logger)
    {
        _produtoUseCase = produtoUseCase;
        _logger = logger;
    }

    /// <summary>Listar produtos paginados</summary>
    [HttpGet]
    [EnableRateLimiting("rateLimitePolicy")]
    [SwaggerOperation(
        Summary = "Listar produtos paginados",
        Description = """
        Retorna a lista de produtos usando **paginacao Offset-Based**.

        ### Parametros
        * **deslocamento:** quantos registros pular (`Skip`). Padrao `0`.
        * **registroRetornado:** quantos registros trazer (`Take`). Padrao `3`, maximo `50`.
        * **categoria:** filtro opcional, atendido pelo indice `IDX_produto_categoria`.

        ### Retorno
        * **200 (OK):** pagina de produtos, total de registros e links HATEOAS.
        * **204 (No Content):** nenhum registro para os filtros informados.
        * **429 (Too Many Requests):** limite de requisicoes da janela excedido.
        """
    )]
    [SwaggerResponse(statusCode: 200, description: "Lista retornada com sucesso", type: typeof(PageResultModel<IEnumerable<ProdutoEntity>>))]
    [SwaggerResponse(statusCode: 204, description: "Nao possui dados para os filtros informados")]
    [SwaggerResponse(statusCode: 429, description: "Limite de requisicoes excedido")]
    [SwaggerResponseExample(statusCode: 200, typeof(ProdutoResponseListSample))]
    public async Task<IActionResult> Get(int deslocamento = 0, int registroRetornado = 3, string? categoria = null)
    {
        var result = await _produtoUseCase.ObterTodosProdutosAsync(deslocamento, registroRetornado, categoria);

        if (!result.Data.Any())
            return NoContent();

        var id = result.Data.FirstOrDefault()?.Id ?? 0;

        var hateoas = new
        {
            data = result,
            links = new
            {
                self = Url.Action(nameof(Get), "Produto", null),
                getById = Url.Action(nameof(GetById), "Produto", new { id }),
                post = Url.Action(nameof(Post), "Produto", null),
                put = Url.Action(nameof(Put), "Produto", new { id }),
                delete = Url.Action(nameof(Delete), "Produto", new { id })
            }
        };

        return Ok(hateoas);
    }

    /// <summary>Obter produto por ID</summary>
    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "Obter produto por ID",
        Description = "Retorna o produto do ID informado, com os links HATEOAS das operacoes relacionadas."
    )]
    [SwaggerResponse(statusCode: 200, description: "Produto encontrado", type: typeof(ProdutoEntity))]
    [SwaggerResponse(statusCode: 404, description: "Produto nao encontrado")]
    [SwaggerResponseExample(statusCode: 200, typeof(ProdutoResponseSample))]
    public async Task<IActionResult> GetById(
        [FromRoute, SwaggerParameter("Identificador unico do produto")] int id)
    {
        var result = await _produtoUseCase.ObterUmProdutoAsync(id);

        if (result is null)
            return NotFound();

        var hateoas = new
        {
            data = result,
            links = new
            {
                self = Url.Action(nameof(GetById), "Produto", new { id }),
                get = Url.Action(nameof(Get), "Produto", null),
                put = Url.Action(nameof(Put), "Produto", new { id }),
                delete = Url.Action(nameof(Delete), "Produto", new { id })
            }
        };

        return Ok(hateoas);
    }

    /// <summary>Cadastrar produto</summary>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Cadastrar produto",
        Description = """
        Cadastra um novo produto no catalogo.

        ### Regras de negocio
        * **Nome:** obrigatorio, minimo de 3 caracteres e unico (indice `IDX_produto_nome`).
        * **Preco:** deve ser maior que zero.
        * **Estoque:** nao pode ser negativo.
        """
    )]
    [SwaggerRequestExample(typeof(ProdutoDto), typeof(ProdutoRequestSample))]
    [SwaggerResponse(statusCode: 201, description: "Produto cadastrado com sucesso", type: typeof(ProdutoEntity))]
    [SwaggerResponse(statusCode: 400, description: "Dados invalidos ou nome ja cadastrado")]
    [SwaggerResponseExample(statusCode: 201, typeof(ProdutoResponseSample))]
    public async Task<IActionResult> Post(ProdutoDto entity)
    {
        try
        {
            var result = await _produtoUseCase.AdicionarProdutoAsync(entity);

            return CreatedAtAction(nameof(GetById), new { id = result?.Id }, result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Falha ao cadastrar produto: {Mensagem}", ex.Message);
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>Editar produto</summary>
    [HttpPut("{id}")]
    [SwaggerOperation(
        Summary = "Editar produto",
        Description = """
        Atualiza os dados de um produto existente.

        ### Observacao
        O nome continua precisando ser unico; a validacao ignora o proprio registro.
        """
    )]
    [SwaggerRequestExample(typeof(ProdutoDto), typeof(ProdutoRequestSample))]
    [SwaggerResponse(statusCode: 200, description: "Produto alterado com sucesso", type: typeof(ProdutoEntity))]
    [SwaggerResponse(statusCode: 400, description: "Dados invalidos ou nome duplicado")]
    [SwaggerResponse(statusCode: 404, description: "Produto nao encontrado")]
    [SwaggerResponseExample(statusCode: 200, typeof(ProdutoResponseSample))]
    public async Task<IActionResult> Put(
        [FromRoute, SwaggerParameter("Identificador unico do produto")] int id,
        ProdutoDto entity)
    {
        try
        {
            var result = await _produtoUseCase.EditarProdutoAsync(id, entity);

            if (result is null)
                return NotFound();

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>Inativar produto</summary>
    [HttpDelete("{id}")]
    [SwaggerOperation(
        Summary = "Inativar produto",
        Description = "Exclusao logica: o produto deixa de ser vendavel, mas continua na base para preservar o historico dos pedidos."
    )]
    [SwaggerResponse(statusCode: 200, description: "Produto inativado com sucesso", type: typeof(ProdutoEntity))]
    [SwaggerResponse(statusCode: 404, description: "Produto nao encontrado")]
    public async Task<IActionResult> Delete(
        [FromRoute, SwaggerParameter("Identificador unico do produto")] int id)
    {
        var result = await _produtoUseCase.DeletarProdutoAsync(id);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
}
