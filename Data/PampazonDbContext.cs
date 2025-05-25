using Microsoft.EntityFrameworkCore;
using Pampazon.Api.Models;

namespace Pampazon.Api.Data;

public class PampazonDbContext : DbContext
{
    public PampazonDbContext(DbContextOptions<PampazonDbContext> options) : base(options) { }

    public DbSet<Producto> Productos { get; set; }
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Posicion> Posiciones { get; set; }
    public DbSet<StockItem> StockItems { get; set; }
    public DbSet<Remito> Remitos { get; set; }
    public DbSet<RemitoItem> RemitoItems { get; set; }
    public DbSet<Orden> Ordenes { get; set; }
    public DbSet<OrdenItem> OrdenItems { get; set; }
    public DbSet<Despacho> Despachos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unicidad
        modelBuilder.Entity<Producto>()
            .HasIndex(p => p.CodigoProducto)
            .IsUnique();

        modelBuilder.Entity<Cliente>()
            .HasIndex(c => c.CUIT)
            .IsUnique();

        modelBuilder.Entity<Posicion>()
            .HasIndex(p => new { p.Pasillo, p.Seccion, p.Estanteria, p.Nivel })
            .IsUnique()
            .HasDatabaseName("IX_Posicion_UbicacionUnica");

        modelBuilder.Entity<StockItem>()
            .HasIndex(s => new { s.ProductoId, s.PosicionId })
            .IsUnique()
            .HasDatabaseName("IX_StockItem_ProductoPosicionUnica");

        modelBuilder.Entity<Orden>()
            .HasIndex(o => o.NumeroOrden)
            .IsUnique();

        modelBuilder.Entity<Despacho>()
            .HasIndex(d => d.NumeroDespacho)
            .IsUnique();


        // Relaciones y comportamiento de eliminación
        // Cliente -> Posiciones
        modelBuilder.Entity<Cliente>()
            .HasMany(c => c.PosicionesAlquiladas)
            .WithOne(p => p.Cliente)
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.Restrict); // No se puede borrar cliente si tiene posiciones

        // RemitoItem -> PosicionIngreso
        modelBuilder.Entity<RemitoItem>()
            .HasOne(ri => ri.PosicionIngreso)
            .WithMany(p => p.RemitoItemsIngreso)
            .HasForeignKey(ri => ri.PosicionId)
            .OnDelete(DeleteBehavior.SetNull); // Si se borra la posición, el RemitoItem.PosicionId queda null

        // OrdenItem -> PosicionEgreso
        modelBuilder.Entity<OrdenItem>()
            .HasOne(oi => oi.PosicionEgreso)
            .WithMany(p => p.OrdenItemsEgreso)
            .HasForeignKey(oi => oi.PosicionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Despacho -> OrdenesAsociadas
        modelBuilder.Entity<Despacho>()
           .HasMany(d => d.OrdenesAsociadas)
           .WithOne(o => o.Despacho)
           .HasForeignKey(o => o.DespachoId)
           .OnDelete(DeleteBehavior.SetNull); // Si se borra el despacho, la orden.DespachoId se vuelve null.

        // Conversión de Enums a string en DB
        modelBuilder.Entity<Remito>()
            .Property(r => r.Estado)
            .HasConversion<string>();

        modelBuilder.Entity<Orden>()
            .Property(o => o.Estado)
            .HasConversion<string>();

        modelBuilder.Entity<Despacho>()
            .Property(d => d.Estado)
            .HasConversion<string>();
    }
}
