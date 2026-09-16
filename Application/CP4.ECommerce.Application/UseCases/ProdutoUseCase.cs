using CP4.ECommerce.Application.Dtos;
using CP4.ECommerce.Application.Interfaces;
using CP4.ECommerce.Application.Mappers;
using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Domain.Interfaces;
using CP4.ECommerce.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CP4.ECommerce.Application.UseCases;

/// <summary>
/// Casos de uso de Produto: valida as regras, converte o DTO pelo Mapper
/// e delega a persistencia ao repositorio.
/// </summary>
public class ProdutoUseCase : IProdutoUseCase
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly ILogger<ProdutoUseCase> _logger;

    public ProdutoUseCase(IProdutoRepository produtoRepository, ILogger<ProdutoUseCase> logger)
    {
        _produtoRepository = produtoRepository;
        _logger = logger;
    }

    public async Task<PageResultModel<IEnumerable<ProdutoEntity>>> ObterTodosProdutosAsync(
        int deslocamento = 0, int registroRetornado = 3, string? categoria = null)
    {
        try
        {
            _logger.LogInformation(
                "Obtendo produtos do repositório. Deslocamento {Deslocamento}, registros {RegistroRetornado}, categoria {Categoria}",
                deslocamento, registroRetornado, categoria);

            return await _produtoRepository.ObterTodosAsync(deslocamento, registroRetornado, categoria);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao obter a lista de produtos");
            throw;
        }
    }

    public async Task<ProdutoEntity?> ObterUmProdutoAsync(int id)
    {
        try
        {
            _logger.LogInformation("Obtendo o produto {ProdutoId} do repositório", id);

            var produto = await _produtoRepository.ObterUmAsync(id);

            if (produto is null)
                _logger.LogWarning("Produto {ProdutoId} não encontrado", id);

            return produto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao obter o produto {ProdutoId}", id);
            throw;
        }
    }

    public async Task<ProdutoEntity?> AdicionarProdutoAsync(ProdutoDto dto)
    {
        ValidarProduto(dto);

        if (await _produtoRepository.ExisteNomeAsync(dto.Nome))
            throw new ArgumentException($"Já existe um produto cadastrado com o nome '{dto.Nome}'.");

        var entity = dto.ToProdutoEntity();
        var produto = await _produtoRepository.AdicionarAsync(entity);

        _logger.LogInformation("Produto {ProdutoId} cadastrado com o nome {Nome}", produto?.Id, produto?.Nome);

        return produto;
    }

    public async Task<ProdutoEntity?> EditarProdutoAsync(int id, ProdutoDto dto)
    {
        ValidarProduto(dto);

        var existente = await _produtoRepository.ObterUmAsync(id);

        if (existente is null)
        {
            _logger.LogWarning("Produto {ProdutoId} não encontrado para edição", id);
            return null;
        }

        if (await _produtoRepository.ExisteNomeAsync(dto.Nome, id))
            throw new ArgumentException($"Já existe outro produto cadastrado com o nome '{dto.Nome}'.");

        var produto = await _produtoRepository.EditarAsync(id, dto.ToProdutoEntity());

        _logger.LogInformation("Produto {ProdutoId} atualizado", id);

        return produto;
    }

    public async Task<ProdutoEntity?> DeletarProdutoAsync(int id)
    {
        var produto = await _produtoRepository.DeletarAsync(id);

        if (produto is null)
            _logger.LogWarning("Produto {ProdutoId} não encontrado para exclusão", id);
        else
            _logger.LogWarning("Produto {ProdutoId} excluído", id);

        return produto;
    }

    /// <summary>Regras de negocio do produto.</summary>
    private static void ValidarProduto(ProdutoDto dto)
    {
        if (dto is null)
            throw new ArgumentException("Os dados do produto são obrigatórios.");

        if (string.IsNullOrWhiteSpace(dto.Nome) || dto.Nome.Trim().Length < 3)
            throw new ArgumentException("O nome do produto deve ter ao menos 3 caracteres.");

        if (string.IsNullOrWhiteSpace(dto.Categoria))
            throw new ArgumentException("A categoria do produto é obrigatória.");

        if (dto.Preco <= 0)
            throw new ArgumentException("O preço do produto deve ser maior que zero.");

        if (dto.Estoque < 0)
            throw new ArgumentException("O estoque do produto não pode ser negativo.");
    }
}
