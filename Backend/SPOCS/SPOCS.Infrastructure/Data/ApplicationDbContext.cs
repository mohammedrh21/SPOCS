using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Pgvector.EntityFrameworkCore;
using SPOCS.Domain.Entities;

namespace SPOCS.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser ,IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<VariantType> VariantTypes => Set<VariantType>();
    public DbSet<VariantOption> VariantOptions => Set<VariantOption>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantOption> ProductVariantOptions => Set<ProductVariantOption>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ProductEmbedding> ProductEmbeddings => Set<ProductEmbedding>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Enable pgvector extension
        builder.HasPostgresExtension("vector");

        // Category Configuration
        builder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(150).IsRequired();
            entity.Property(c => c.Slug).HasMaxLength(150).IsRequired();
            entity.HasIndex(c => c.Slug).IsUnique();
        });

        // Product Configuration
        var jsonOptions = new JsonSerializerOptions();

        var featuresConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>());

        var featuresComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        var specsConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<Dictionary<string, string>, string>(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => string.IsNullOrEmpty(v) ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>());

        var specsComparer = new ValueComparer<Dictionary<string, string>>(
            (d1, d2) => d1 != null && d2 != null && d1.Count == d2.Count && !d1.Except(d2).Any(),
            d => d.Aggregate(0, (a, p) => HashCode.Combine(a, p.Key.GetHashCode(), p.Value.GetHashCode())),
            d => new Dictionary<string, string>(d));

        builder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(250).IsRequired();
            entity.Property(p => p.Slug).HasMaxLength(250).IsRequired();
            entity.Property(p => p.Sku).HasMaxLength(100).IsRequired();
            entity.Property(p => p.BasePrice).HasPrecision(18, 2);

            entity.Property(p => p.Features)
                .HasConversion(featuresConverter)
                .Metadata.SetValueComparer(featuresComparer);

            entity.Property(p => p.Specifications)
                .HasConversion(specsConverter)
                .Metadata.SetValueComparer(specsComparer);

            entity.HasIndex(p => p.Slug).IsUnique();
            entity.HasIndex(p => p.Sku).IsUnique();

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ProductImage Configuration
        builder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(pi => pi.Id);
            entity.Property(pi => pi.ImageUrl).IsRequired();

            entity.HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // VariantType Configuration
        builder.Entity<VariantType>(entity =>
        {
            entity.HasKey(vt => vt.Id);
            entity.Property(vt => vt.Name).HasMaxLength(100).IsRequired();
        });

        // VariantOption Configuration
        builder.Entity<VariantOption>(entity =>
        {
            entity.HasKey(vo => vo.Id);
            entity.Property(vo => vo.Value).HasMaxLength(100).IsRequired();

            entity.HasOne(vo => vo.VariantType)
                .WithMany(vt => vt.Options)
                .HasForeignKey(vo => vo.VariantTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductVariant Configuration
        builder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(pv => pv.Id);
            entity.Property(pv => pv.Sku).HasMaxLength(100).IsRequired();
            entity.Property(pv => pv.Price).HasPrecision(18, 2);

            entity.HasIndex(pv => pv.Sku).IsUnique();

            entity.HasOne(pv => pv.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductVariantOption (Many-to-Many join table)
        builder.Entity<ProductVariantOption>(entity =>
        {
            entity.HasKey(pvo => new { pvo.ProductVariantId, pvo.VariantOptionId });

            entity.HasOne(pvo => pvo.ProductVariant)
                .WithMany(pv => pv.ProductVariantOptions)
                .HasForeignKey(pvo => pvo.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pvo => pvo.VariantOption)
                .WithMany(vo => vo.ProductVariantOptions)
                .HasForeignKey(pvo => pvo.VariantOptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Cart Configuration
        builder.Entity<Cart>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.HasOne(c => c.Customer)
                .WithOne(u => u.Cart)
                .HasForeignKey<Cart>(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CartItem Configuration
        builder.Entity<CartItem>(entity =>
        {
            entity.HasKey(ci => ci.Id);
            entity.Property(ci => ci.UnitPrice).HasPrecision(18, 2);

            entity.HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ci => ci.ProductVariant)
                .WithMany()
                .HasForeignKey(ci => ci.ProductVariantId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Order Configuration
        builder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.OrderNumber).HasMaxLength(50).IsRequired();
            entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
            entity.HasIndex(o => o.OrderNumber).IsUnique();

            entity.HasOne(o => o.Customer)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OrderItem Configuration
        builder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.Id);
            entity.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            entity.Property(oi => oi.TotalPrice).HasPrecision(18, 2);
            entity.Property(oi => oi.ProductName).HasMaxLength(250).IsRequired();

            entity.HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(oi => oi.ProductVariant)
                .WithMany()
                .HasForeignKey(oi => oi.ProductVariantId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ProductEmbedding Configuration
        builder.Entity<ProductEmbedding>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Product)
                .WithOne()
                .HasForeignKey<ProductEmbedding>(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.PgVector)
                .HasColumnType("vector(1536)")
                .IsRequired();

            entity.Ignore(e => e.Vector);

            entity.Property(e => e.EmbeddingText).IsRequired();
            entity.Property(e => e.ModelName).HasMaxLength(100);

            entity.HasIndex(e => e.ProductId).IsUnique();
        });
    }
}
