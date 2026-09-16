using CP4.ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CP4.ECommerce.Infrastructure.Data.AppData;

/// <summary>
/// Contexto do Entity Framework Core. As tabelas e os indices sao
/// declarados por atributos nas proprias entidades.
/// </summary>
public class ApplicationContext : DbContext
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
    {
    }

    public DbSet<ProdutoEntity> Produto { get; set; } = null!;
    public DbSet<PedidoEntity> Pedido { get; set; } = null!;
    public DbSet<ItemPedidoEntity> ItemPedido { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ao excluir um pedido, os itens vao junto.
        modelBuilder.Entity<PedidoEntity>()
            .HasMany(p => p.Itens)
            .WithOne()
            .HasForeignKey(i => i.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        // O Oracle 19c nao tem o tipo BOOLEAN, entao o Ativo e gravado
        // como NUMBER(1): 1 para ativo e 0 para inativo.
        modelBuilder.Entity<ProdutoEntity>()
            .Property(p => p.Ativo)
            .HasConversion<int>()
            .HasColumnType("NUMBER(1)");
    }
}
