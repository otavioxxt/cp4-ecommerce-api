using CP4.ECommerce.Application.Dtos;
using CP4.ECommerce.Application.UseCases;
using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Domain.Interfaces;
using CP4.ECommerce.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CP4.ECommerce.Tests.Unit.APP;

/// <summary>
/// Testes do ProdutoUseCase. O repositorio e substituido por um Mock, para
/// o teste validar so a logica do caso de uso, sem tocar no banco.
/// </summary>
public class ProdutoUseCaseTest
{
    private readonly Mock<IProdutoRepository> _produtoRepository;
    private readonly ProdutoUseCase _produtoUseCase;

    public ProdutoUseCaseTest()
    {
        _produtoRepository = new Mock<IProdutoRepository>();
        _produtoUseCase = new ProdutoUseCase(_produtoRepository.Object, NullLogger<ProdutoUseCase>.Instance);
    }

    private static ProdutoDto NovoDto(string nome = "Teclado Mecanico", decimal preco = 349.90m, int estoque = 25)
        => new(nome, "Descricao de teste", "Perifericos", preco, estoque);

    [Fact]
    [Trait("UseCase", "Produtos")]
    public async Task ObterTodosProdutos_DeveRetornarAPaginaDoRepositorio()
    {
        // Arrange
        var pagina = new PageResultModel<IEnumerable<ProdutoEntity>>
        {
            Data = new List<ProdutoEntity>
            {
                new() { Id = 1, Nome = "Teclado", Categoria = "Perifericos", Preco = 150m, Estoque = 10 },
                new() { Id = 2, Nome = "Mouse", Categoria = "Perifericos", Preco = 90m, Estoque = 20 }
            },
            Deslocamento = 0,
            RegistroRetornado = 3,
            TotalRegistros = 48
        };

        _produtoRepository
            .Setup(obj => obj.ObterTodosAsync(0, 3, null))
            .Returns(Task.FromResult(pagina));

        // Act
        var resultado = await _produtoUseCase.ObterTodosProdutosAsync(0, 3);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(2, resultado.Data.Count());
        Assert.Equal(48, resultado.TotalRegistros);

        var produto = resultado.Data.SingleOrDefault(x => x.Id == 1);

        Assert.NotNull(produto);
        Assert.Equal("Teclado", produto!.Nome);
    }

    [Fact]
    [Trait("UseCase", "Produtos")]
    public async Task ObterUmProduto_ComIdInexistente_DeveRetornarNulo()
    {
        _produtoRepository
            .Setup(obj => obj.ObterUmAsync(It.IsAny<int>()))
            .Returns(Task.FromResult<ProdutoEntity?>(null));

        var resultado = await _produtoUseCase.ObterUmProdutoAsync(999);

        Assert.Null(resultado);
    }

    [Fact]
    [Trait("UseCase", "Produtos")]
    public async Task AdicionarProduto_ComDadosValidos_DevePersistir()
    {
        // Arrange
        var dto = NovoDto();

        _produtoRepository
            .Setup(obj => obj.ExisteNomeAsync(dto.Nome, null))
            .Returns(Task.FromResult(false));

        _produtoRepository
            .Setup(obj => obj.AdicionarAsync(It.IsAny<ProdutoEntity>()))
            .Returns((ProdutoEntity e) => Task.FromResult<ProdutoEntity?>(e));

        // Act
        var resultado = await _produtoUseCase.AdicionarProdutoAsync(dto);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(dto.Nome, resultado!.Nome);
        Assert.Equal(dto.Preco, resultado.Preco);
        Assert.True(resultado.Ativo);

        _produtoRepository.Verify(obj => obj.AdicionarAsync(It.IsAny<ProdutoEntity>()), Times.Once);
    }

    [Fact]
    [Trait("UseCase", "Produtos")]
    public async Task AdicionarProduto_ComNomeDuplicado_DeveLancarExcecao()
    {
        // Arrange
        var dto = NovoDto("Produto Repetido");

        _produtoRepository
            .Setup(obj => obj.ExisteNomeAsync(dto.Nome, null))
            .Returns(Task.FromResult(true));

        // Act + Assert
        var excecao = await Assert.ThrowsAsync<ArgumentException>(
            () => _produtoUseCase.AdicionarProdutoAsync(dto));

        Assert.Contains("Produto Repetido", excecao.Message);
        _produtoRepository.Verify(obj => obj.AdicionarAsync(It.IsAny<ProdutoEntity>()), Times.Never);
    }

    [Theory]
    [InlineData("", 100, 10)]
    [InlineData("ab", 100, 10)]
    [InlineData("Produto Valido", 0, 10)]
    [InlineData("Produto Valido", 100, -1)]
    [Trait("UseCase", "Produtos")]
    public async Task AdicionarProduto_ComDadosInvalidos_DeveLancarExcecao(string nome, decimal preco, int estoque)
    {
        var dto = NovoDto(nome, preco, estoque);

        await Assert.ThrowsAsync<ArgumentException>(() => _produtoUseCase.AdicionarProdutoAsync(dto));

        _produtoRepository.Verify(obj => obj.AdicionarAsync(It.IsAny<ProdutoEntity>()), Times.Never);
    }

    [Fact]
    [Trait("UseCase", "Produtos")]
    public async Task EditarProduto_ComIdInexistente_DeveRetornarNulo()
    {
        _produtoRepository
            .Setup(obj => obj.ObterUmAsync(It.IsAny<int>()))
            .Returns(Task.FromResult<ProdutoEntity?>(null));

        var resultado = await _produtoUseCase.EditarProdutoAsync(999, NovoDto());

        Assert.Null(resultado);
        _produtoRepository.Verify(obj => obj.EditarAsync(It.IsAny<int>(), It.IsAny<ProdutoEntity>()), Times.Never);
    }
}
