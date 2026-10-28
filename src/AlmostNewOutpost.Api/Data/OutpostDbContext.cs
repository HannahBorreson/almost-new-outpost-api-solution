using AlmostNewOutpost.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlmostNewOutpost.Api.Data;

/// <summary>
/// The EF Core context for the existing almost_new_outpost database. The schema is owned by
/// database/almost_new_outpost.sql (no migrations). Application code only uses this context to
/// call stored procedures from the repositories; the DbSets let tests arrange and check data.
/// </summary>
public class OutpostDbContext : DbContext
{
    public OutpostDbContext(DbContextOptions<OutpostDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Purchase> Purchases => Set<Purchase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Table and column names match the SQL script exactly.
        modelBuilder.Entity<Role>(role =>
        {
            role.ToTable("Roles");
            role.HasKey(r => r.RoleId);
            role.Property(r => r.RoleName).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("Users");
            user.HasKey(u => u.UserId);
            user.Property(u => u.Username).HasMaxLength(50).IsRequired();
            user.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            user.HasOne<Role>().WithMany().HasForeignKey(u => u.RoleId);
        });

        modelBuilder.Entity<Category>(category =>
        {
            category.ToTable("Categories");
            category.HasKey(c => c.CategoryId);
            category.Property(c => c.CategoryName).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Product>(product =>
        {
            product.ToTable("Products");
            product.HasKey(p => p.ProductId);
            product.Property(p => p.Name).HasMaxLength(100).IsRequired();
            product.Property(p => p.Description).HasMaxLength(500);
            product.Property(p => p.IconKey).HasMaxLength(50);
            product.HasOne<Category>().WithMany().HasForeignKey(p => p.CategoryId);
        });

        modelBuilder.Entity<Purchase>(purchase =>
        {
            purchase.ToTable("Purchases");
            purchase.HasKey(p => p.PurchaseId);
            purchase.HasOne<User>().WithMany().HasForeignKey(p => p.UserId);
            purchase.HasOne<Product>().WithMany().HasForeignKey(p => p.ProductId);
        });
    }
}
