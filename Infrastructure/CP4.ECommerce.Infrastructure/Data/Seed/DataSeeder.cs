using CP4.ECommerce.Domain.Entities;
using CP4.ECommerce.Infrastructure.Data.AppData;
using Microsoft.EntityFrameworkCore;

namespace CP4.ECommerce.Infrastructure.Data.Seed;

/// <summary>
/// Popula a base com volume suficiente para demonstrar a paginação
/// durante a apresentação (48 produtos e alguns pedidos).
/// </summary>
public static class DataSeeder
{
    private static readonly (string Categoria, string[] Nomes)[] Catalogo =
    {
        ("Perifericos", new[]
        {
            "Teclado Mecanico RGB", "Mouse Gamer 16000 DPI", "Headset 7.1 Surround", "Mousepad Speed XL",
            "Webcam Full HD", "Microfone Condensador USB", "Controle Sem Fio", "Hub USB-C 7 Portas"
        }),
        ("Monitores", new[]
        {
            "Monitor 24 144Hz", "Monitor 27 QHD", "Monitor Ultrawide 34", "Monitor 32 4K HDR",
            "Suporte Articulado", "Monitor Portatil 15.6", "Monitor 21.5 IPS", "Braco Duplo VESA"
        }),
        ("Notebooks", new[]
        {
            "Notebook Ryzen 5 16GB", "Notebook i7 512GB SSD", "Notebook Gamer RTX 4060", "Ultrabook 14 Polegadas",
            "Notebook i5 8GB", "Base Refrigerada", "Mochila Executiva 15", "Dock Station Thunderbolt"
        }),
        ("Armazenamento", new[]
        {
            "SSD NVMe 1TB", "SSD SATA 480GB", "HD Externo 2TB", "Pendrive 128GB USB 3.2",
            "Cartao microSD 256GB", "SSD NVMe 2TB", "Gaveta para HD 2.5", "Case NVMe USB-C"
        }),
        ("Redes", new[]
        {
            "Roteador Wi-Fi 6 AX1800", "Switch Gigabit 8 Portas", "Repetidor Mesh", "Adaptador USB Wi-Fi",
            "Cabo Rede Cat6 5m", "Access Point PoE", "Nobreak 1200VA", "Filtro de Linha 6 Tomadas"
        }),
        ("Acessorios", new[]
        {
            "Cabo HDMI 2.1 2m", "Adaptador USB-C HDMI", "Carregador GaN 65W", "Suporte para Notebook",
            "Organizador de Cabos", "Kit Limpa Telas", "Cadeira Ergonomica", "Luminaria de Mesa LED"
        })
    };

    public static async Task SeedAsync(ApplicationContext context)
    {
        if (await context.Produto.AnyAsync())
            return;

        var aleatorio = new Random(20260916);
        var produtos = new List<ProdutoEntity>();

        foreach (var (categoria, nomes) in Catalogo)
        {
            foreach (var nome in nomes)
            {
                produtos.Add(new ProdutoEntity
                {
                    Nome = nome,
                    Descricao = $"{nome} - linha {categoria.ToLower()}, garantia de 12 meses.",
                    Categoria = categoria,
                    Preco = Math.Round((decimal)(aleatorio.NextDouble() * 4500 + 49.9), 2),
                    Estoque = aleatorio.Next(5, 140),
                    Ativo = true,
                    DataCadastro = DateTime.Now
                });
            }
        }

        context.Produto.AddRange(produtos);
        await context.SaveChangesAsync();

        var clientes = new[]
        {
            ("Otavio Santos", "otavio@exemplo.com"),
            ("Marina Duarte", "marina@exemplo.com"),
            ("Rafael Lima", "rafael@exemplo.com"),
            ("Beatriz Nunes", "beatriz@exemplo.com"),
            ("Carlos Prado", "carlos@exemplo.com")
        };

        var pedidos = new List<PedidoEntity>();
        var indice = 0;

        foreach (var (nome, email) in clientes)
        {
            for (var n = 0; n < 3; n++)
            {
                var pedido = new PedidoEntity
                {
                    NumeroPedido = $"PED-{DateTime.Now:yyyyMMdd}-{indice:D4}{n}",
                    ClienteNome = nome,
                    ClienteEmail = email,
                    Status = indice % 3 == 0 ? StatusPedido.Confirmado : StatusPedido.Rascunho,
                    DataPedido = DateTime.Now.AddDays(-indice),
                    Itens = new List<ItemPedidoEntity>()
                };

                var quantosItens = aleatorio.Next(1, 4);

                for (var k = 0; k < quantosItens; k++)
                {
                    var produto = produtos[(indice * 7 + k * 3) % produtos.Count];
                    var quantidade = aleatorio.Next(1, 4);

                    produto.Estoque -= quantidade;

                    pedido.Itens.Add(new ItemPedidoEntity
                    {
                        ProdutoId = produto.Id,
                        ProdutoNome = produto.Nome,
                        Quantidade = quantidade,
                        PrecoUnitario = produto.Preco
                    });
                }

                pedido.ValorTotal = pedido.Itens.Sum(i => i.Quantidade * i.PrecoUnitario);

                pedidos.Add(pedido);
                indice++;
            }
        }

        context.Pedido.AddRange(pedidos);
        await context.SaveChangesAsync();
    }
}
