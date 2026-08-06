using InventoryApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockLevel> StockLevels => Set<StockLevel>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Sku).HasMaxLength(32).IsRequired();
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
            entity.HasIndex(p => p.Sku).IsUnique();
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.Property(w => w.Code).HasMaxLength(16).IsRequired();
            entity.Property(w => w.Name).HasMaxLength(120).IsRequired();
            entity.HasIndex(w => w.Code).IsUnique();
        });

        modelBuilder.Entity<StockLevel>(entity =>
        {
            entity.HasIndex(s => new { s.ProductId, s.WarehouseId }).IsUnique();
            entity.HasOne(s => s.Product).WithMany(p => p.StockLevels)
                  .HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.Warehouse).WithMany(w => w.StockLevels)
                  .HasForeignKey(s => s.WarehouseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockAdjustment>(entity =>
        {
            entity.Property(a => a.Reason).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(a => new { a.ProductId, a.WarehouseId });
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Email).HasMaxLength(320).IsRequired();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(16);
            entity.HasOne(o => o.Customer).WithMany()
                  .HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(o => o.Status);
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(l => l.DiscountPercent).HasColumnType("decimal(5,2)");
            entity.HasOne(l => l.Order).WithMany(o => o.Lines)
                  .HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(l => l.Product).WithMany()
                  .HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.Property(s => s.TrackingNumber).HasMaxLength(64).IsRequired();
            entity.HasOne(s => s.Order).WithMany(o => o.Shipments)
                  .HasForeignKey(s => s.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.Warehouse).WithMany()
                  .HasForeignKey(s => s.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
