using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Infrastructure.Data.AppData;
using CP4.ECommerce.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CP4.ECommerce.Tests.Unit.APP;

/// <summary>
/// Testes do repositorio de Produto com o provedor InMemory do EF Core.
/// O banco e criado em memoria a cada teste, sem depender do Oracle.
/// </summary>
public class ProdutoRepositoryTest
{
    private readonly ApplicationContext _applicationContext;
    private readonly ProdutoRepository _produtoRepository;

    public ProdutoRepositoryTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: $"TestDatabase_Produto_{Guid.NewGuid()}")
            .Options;

        _applicationContext = new ApplicationContext(options);

        _applicationContext.Database.EnsureDeleted();
        _applicationContext.Database.EnsureCreated();

        _produtoRepository = new ProdutoRepository(_applicationContext);
    }

    private static ProdutoEntity NovoProduto(
        string nome, string categoria = "Perifericos", decimal preco = 100m, int estoque = 10)
        => new()
        {
            Nome = nome,
            Descricao = $"Descricao de {nome}",
            Categoria = categoria,
            Preco = preco,
            Estoque = estoque,
            Ativo = true,
            DataCadastro = DateTime.Now
        };

    [Fact]
    [Trait("Repository", "Produtos")]
    public async Task ObterTodos_DeveRetornarOsProdutos()
    {
        // Arrange
        _applicationContext.Produto.AddRange(
            NovoProduto("Teclado", preco: 150m, estoque: 30),
            NovoProduto("Mouse", preco: 90m, estoque: 25),
            NovoProduto("Headset", preco: 300m, estoque: 40));

        await _applicationContext.SaveChangesAsync();

        // Act
        var resultado = await _produtoRepository.ObterTodosAsync(0, 10);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(3, resultado.TotalRegistros);

        Assert.Collection(resultado.Data,
            produto =>
            {
                Assert.Equal("Teclado", produto.Nome);
                Assert.Equal(150m, produto.Preco);
            },
            produto => Assert.Equal("Mouse", produto.Nome),
            produto => Assert.Equal("Headset", produto.Nome));
    }

    [Theory]
    [InlineData(0, 2, 2)]
    [InlineData(2, 2, 2)]
    [InlineData(4, 2, 1)]
    [Trait("Repository", "Produtos")]
    public async Task ObterTodos_DeveRespeitarODeslocamento(int deslocamento, int registros, int esperado)
    {
        // Arrange
        for (var i = 1; i <= 5; i++)
            _applicationContext.Produto.Add(NovoProduto($"Produto {i}"));

        await _applicationContext.SaveChangesAsync();

        // Act
        var resultado = await _produtoRepository.ObterTodosAsync(deslocamento, registros);

        // Assert
        Assert.Equal(esperado, resultado.Data.Count());
        Assert.Equal(5, resultado.TotalRegistros);
    }

    [Fact]
    [Trait("Repository", "Produtos")]
    public async Task ObterTodos_ComFiltroDeCategoria_DeveFiltrar()
    {
        // Arrange
        _applicationContext.Produto.AddRange(
            NovoProduto("Monitor 24", "Monitores"),
            NovoProduto("Monitor 27", "Monitores"),
            NovoProduto("Teclado", "Perifericos"));

        await _applicationContext.SaveChangesAsync();

        // Act
        var resultado = await _produtoRepository.ObterTodosAsync(0, 10, "Monitores");

        // Assert
        Assert.Equal(2, resultado.TotalRegistros);
        Assert.All(resultado.Data, produto => Assert.Equal("Monitores", produto.Categoria));
    }

    [Fact]
    [Trait("Repository", "Produtos")]
    public async Task ObterUm_DeveRetornarUmProduto()
    {
        // Arrange
        var produto = NovoProduto("Webcam Full HD", preco: 250m, estoque: 8);

        _applicationContext.Produto.Add(produto);
        await _applicationContext.SaveChangesAsync();

        // Act
        var resultado = await _produtoRepository.ObterUmAsync(produto.Id);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("Webcam Full HD", resultado!.Nome);
        Assert.Equal(250m, resultado.Preco);
    }

    [Fact]
    [Trait("Repository", "Produtos")]
    public async Task Adicionar_DeveGravarOProdutoNoBanco()
    {
        // Arrange
        var produto = NovoProduto("SSD NVMe 1TB", "Armazenamento", 499m, 15);

        // Act
        var resultado = await _produtoRepository.AdicionarAsync(produto);

        // Assert
        var idProduto = resultado?.Id ?? -1;
        var produtoNoDb = _applicationContext.Produto.FirstOrDefault(p => p.Id == idProduto);

        Assert.NotNull(produtoNoDb);
        Assert.Equal("SSD NVMe 1TB", produtoNoDb!.Nome);
        Assert.Equal(499m, produtoNoDb.Preco);
        Assert.True(produtoNoDb.Ativo);
    }

    [Fact]
    [Trait("Repository", "Produtos")]
    public async Task Editar_DeveAtualizarOsDados()
    {
        // Arrange
        var produto = NovoProduto("Nome Antigo", preco: 10m, estoque: 1);

        _applicationContext.Produto.Add(produto);
        await _applicationContext.SaveChangesAsync();

        // Act
        var resultado = await _produtoRepository.EditarAsync(
            produto.Id, NovoProduto("Nome Novo", "Acessorios", 555.55m, 42));

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("Nome Novo", resultado!.Nome);
        Assert.Equal(555.55m, resultado.Preco);
        Assert.Equal(42, resultado.Estoque);
    }

    [Fact]
    [Trait("Repository", "Produtos")]
    public async Task Deletar_DeveInativarOProduto()
    {
        // Arrange
        var produto = NovoProduto("Produto a Inativar");

        _applicationContext.Produto.Add(produto);
        await _applicationContext.SaveChangesAsync();

        // Act
        var resultado = await _produtoRepository.DeletarAsync(produto.Id);

        // Assert - exclusao logica: o registro continua na base, porem inativo
        Assert.NotNull(resultado);
        Assert.False(resultado!.Ativo);
    }

    [Fact]
    [Trait("Repository", "Produtos")]
    public async Task ExisteNome_DeveIdentificarNomeDuplicado()
    {
        // Arrange
        var produto = NovoProduto("Nome Repetido");

        _applicationContext.Produto.Add(produto);
        await _applicationContext.SaveChangesAsync();

        // Act + Assert
        Assert.True(await _produtoRepository.ExisteNomeAsync("nome repetido"));
        Assert.False(await _produtoRepository.ExisteNomeAsync("Nome Repetido", produto.Id));
        Assert.False(await _produtoRepository.ExisteNomeAsync("Outro Nome"));
    }
}
