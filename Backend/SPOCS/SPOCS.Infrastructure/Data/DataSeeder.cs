using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SPOCS.Application.Contracts;
using SPOCS.Application.Contracts.AI;
using SPOCS.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace SPOCS.Infrastructure.Data;

public class DataSeeder : IDataSeeder
{
    private readonly ApplicationDbContext context;
    private readonly RoleManager<IdentityRole<Guid>> roleManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<DataSeeder> logger;
    private static readonly string[] Roles = { "Admin" };

    public DataSeeder(
        ApplicationDbContext _context,
        UserManager<ApplicationUser> _userManager,
        RoleManager<IdentityRole<Guid>> _roleManager,
        IServiceProvider _serviceProvider,
        ILogger<DataSeeder> _logger)
    {
        this.context = _context;
        this.userManager = _userManager;
        this.roleManager = _roleManager;
        this.serviceProvider = _serviceProvider;
        this.logger = _logger;
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();

        // 1. Ensure Database Created or Migrated
        await context.Database.MigrateAsync();

        // 2. Seed Test Customer if not exists
        var testEmail = "customer@spocs.com";
        var existingUser = await userManager.FindByEmailAsync(testEmail);
        if (existingUser == null)
        {
            var customer = new ApplicationUser
            {
                UserName = testEmail,
                Email = testEmail,
                EmailConfirmed = true,
                FullName = "Demo Customer",
                Address = "123 Tech Avenue",
                City = "San Francisco",
                PostalCode = "94105",
                Country = "USA",
                CreatedAt = DateTime.UtcNow
            };

            await userManager.CreateAsync(customer, "Customer123!");
        }

        // 3. Seed Categories (ensure all 8 categories exist)
        var categories = GetCategories();
        foreach (var cat in categories)
        {
            if (!await context.Categories.AnyAsync(c => c.Id == cat.Id))
            {
                await context.Categories.AddAsync(cat);
            }
        }
        await context.SaveChangesAsync();

        // 4. Seed Variant Types and Options
        await EnsureVariantTypesAndOptionsAsync();

        // 5. Seed Products (upsert missing products idempotently)
        var allProducts = GetAllSeedProducts();
        var existingSkus = (await context.Products.Select(p => p.Sku).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var productsToInsert = allProducts.Where(p => !existingSkus.Contains(p.Sku)).ToList();

        if (productsToInsert.Any())
        {
            logger.LogInformation("Seeding {Count} products into database...", productsToInsert.Count);
            await context.Products.AddRangeAsync(productsToInsert);
            await context.SaveChangesAsync();
            logger.LogInformation("Successfully seeded {Count} products.", productsToInsert.Count);
        }
        else
        {
            logger.LogInformation("All {Total} products are already seeded.", allProducts.Count);
        }

        // 6. Seed Product Variants for key products
        await EnsureProductVariantsAsync();

        // 7. Seed product embeddings
        await TryGenerateEmbeddingsAsync();
    }

    private async Task EnsureVariantTypesAndOptionsAsync()
    {
        if (!await context.VariantTypes.AnyAsync(vt => vt.Name == "Storage"))
        {
            var vtStorage = new VariantType { Id = Guid.NewGuid(), Name = "Storage", DisplayName = "Internal Storage" };
            var vtRam = new VariantType { Id = Guid.NewGuid(), Name = "RAM", DisplayName = "Memory" };
            var vtColor = new VariantType { Id = Guid.NewGuid(), Name = "Color", DisplayName = "Color" };

            var opt256Gb = new VariantOption { Id = Guid.NewGuid(), VariantType = vtStorage, Value = "256GB SSD" };
            var opt512Gb = new VariantOption { Id = Guid.NewGuid(), VariantType = vtStorage, Value = "512GB SSD" };
            var opt1Tb = new VariantOption { Id = Guid.NewGuid(), VariantType = vtStorage, Value = "1TB SSD" };

            var opt16GbRam = new VariantOption { Id = Guid.NewGuid(), VariantType = vtRam, Value = "16GB RAM" };
            var opt32GbRam = new VariantOption { Id = Guid.NewGuid(), VariantType = vtRam, Value = "32GB RAM" };

            var optSpaceGray = new VariantOption { Id = Guid.NewGuid(), VariantType = vtColor, Value = "Space Gray" };
            var optSilver = new VariantOption { Id = Guid.NewGuid(), VariantType = vtColor, Value = "Silver" };
            var optBlack = new VariantOption { Id = Guid.NewGuid(), VariantType = vtColor, Value = "Phantom Black" };

            await context.VariantTypes.AddRangeAsync(vtStorage, vtRam, vtColor);
            await context.VariantOptions.AddRangeAsync(opt256Gb, opt512Gb, opt1Tb, opt16GbRam, opt32GbRam, optSpaceGray, optSilver, optBlack);
            await context.SaveChangesAsync();
        }
    }

    private async Task EnsureProductVariantsAsync()
    {
        if (await context.ProductVariants.AnyAsync()) return;

        var macbookId = Guid.Parse("aaaaaaaa-1111-0000-0000-000000000001");
        var s24Id = Guid.Parse("bbbbbbbb-2222-0000-0000-000000000001");
        var macbook = await context.Products.FirstOrDefaultAsync(p => p.Id == macbookId);
        var s24 = await context.Products.FirstOrDefaultAsync(p => p.Id == s24Id);

        var opt512Gb = await context.VariantOptions.FirstOrDefaultAsync(o => o.Value == "512GB SSD");
        var opt1Tb = await context.VariantOptions.FirstOrDefaultAsync(o => o.Value == "1TB SSD");
        var opt256Gb = await context.VariantOptions.FirstOrDefaultAsync(o => o.Value == "256GB SSD");
        var opt16Gb = await context.VariantOptions.FirstOrDefaultAsync(o => o.Value == "16GB RAM");
        var opt32Gb = await context.VariantOptions.FirstOrDefaultAsync(o => o.Value == "32GB RAM");
        var optSpaceGray = await context.VariantOptions.FirstOrDefaultAsync(o => o.Value == "Space Gray");
        var optSilver = await context.VariantOptions.FirstOrDefaultAsync(o => o.Value == "Silver");
        var optBlack = await context.VariantOptions.FirstOrDefaultAsync(o => o.Value == "Phantom Black");

        if (macbook != null && opt512Gb != null && optSpaceGray != null)
        {
            var mv1 = new ProductVariant { Id = Guid.NewGuid(), ProductId = macbook.Id, Sku = "MBP-14-M3P-18-512-SG", Price = 1999.00m, StockQuantity = 20 };
            mv1.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = mv1, VariantOptionId = opt512Gb.Id });
            mv1.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = mv1, VariantOptionId = optSpaceGray.Id });

            var mv2 = new ProductVariant { Id = Guid.NewGuid(), ProductId = macbook.Id, Sku = "MBP-14-M3P-36-1TB-SLV", Price = 2499.00m, StockQuantity = 15 };
            if (opt1Tb != null) mv2.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = mv2, VariantOptionId = opt1Tb.Id });
            if (opt32Gb != null) mv2.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = mv2, VariantOptionId = opt32Gb.Id });
            if (optSilver != null) mv2.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = mv2, VariantOptionId = optSilver.Id });

            await context.ProductVariants.AddRangeAsync(mv1, mv2);
        }

        if (s24 != null && opt256Gb != null && optBlack != null)
        {
            var sv1 = new ProductVariant { Id = Guid.NewGuid(), ProductId = s24.Id, Sku = "S24U-256-BLK", Price = 1299.00m, StockQuantity = 25 };
            sv1.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = sv1, VariantOptionId = opt256Gb.Id });
            sv1.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = sv1, VariantOptionId = optBlack.Id });

            var sv2 = new ProductVariant { Id = Guid.NewGuid(), ProductId = s24.Id, Sku = "S24U-512-TI", Price = 1419.00m, StockQuantity = 15 };
            if (opt512Gb != null) sv2.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = sv2, VariantOptionId = opt512Gb.Id });
            if (optSilver != null) sv2.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = sv2, VariantOptionId = optSilver.Id });

            await context.ProductVariants.AddRangeAsync(sv1, sv2);
        }

        await context.SaveChangesAsync();
    }

    private async Task TryGenerateEmbeddingsAsync()
    {
        try
        {
            var embeddingService = serviceProvider.GetService<IProductEmbeddingService>();
            if (embeddingService != null)
            {
                logger.LogInformation("Checking / generating embeddings for seeded products...");
                await embeddingService.GenerateAllEmbeddingsAsync();
                logger.LogInformation("Product embeddings check/generation completed.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Product embeddings could not be generated during database seeding ({Message}). Embeddings can be generated later via POST /api/embeddings/generate-all.", ex.Message);
        }
    }

    public static List<Category> GetCategories() => new()
    {
        new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Laptops", Slug = "laptops", Description = "High-performance laptops for programming, gaming, study, and creative workflows.", ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&auto=format&fit=crop&q=80" },
        new Category { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Smartphones", Slug = "smartphones", Description = "Flagship and budget smartphones featuring outstanding camera optics and battery life.", ImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=800&auto=format&fit=crop&q=80" },
        new Category { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Headphones & Audio", Slug = "headphones-audio", Description = "Wireless noise-canceling headphones, gaming headsets, and studio monitors.", ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&auto=format&fit=crop&q=80" },
        new Category { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Wearables & Smartwatches", Slug = "wearables-smartwatches", Description = "Smartwatches, fitness trackers, and rugged outdoor smart devices.", ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&auto=format&fit=crop&q=80" },
        new Category { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Tablets & E-Readers", Slug = "tablets-ereaders", Description = "Productivity tablets, digital drawing pads, and glare-free e-readers.", ImageUrl = "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=800&auto=format&fit=crop&q=80" },
        new Category { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Gaming & Consoles", Slug = "gaming-consoles", Description = "Next-gen gaming consoles, handheld gaming PCs, controllers, and VR headsets.", ImageUrl = "https://images.unsplash.com/photo-1607604276583-eef5d076aa5f?w=800&auto=format&fit=crop&q=80" },
        new Category { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), Name = "Computer Accessories", Slug = "computer-accessories", Description = "Mechanical keyboards, precision mice, 4K monitors, docking stations, and webcams.", ImageUrl = "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?w=800&auto=format&fit=crop&q=80" },
        new Category { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), Name = "Smart Home & Networking", Slug = "smart-home-networking", Description = "Wi-Fi 7 mesh routers, smart displays, home automation hubs, and smart lighting.", ImageUrl = "https://images.unsplash.com/photo-1558002038-1055907df827?w=800&auto=format&fit=crop&q=80" },
    };

    private static Product CreateProduct(
        Guid id, string name, string slug, decimal price, string sku, int stock, Guid categoryId,
        string description, string shortDescription, string imageUrl,
        string[] features, (string Key, string Value)[] specs)
    {
        var p = new Product
        {
            Id = id,
            Name = name,
            Slug = slug,
            BasePrice = price,
            Sku = sku,
            StockQuantity = stock,
            CategoryId = categoryId,
            Description = description,
            ShortDescription = shortDescription,
            Features = features.ToList(),
            Specifications = specs.ToDictionary(s => s.Key, s => s.Value),
            CreatedAt = DateTime.UtcNow
        };
        p.Images.Add(new ProductImage { ImageUrl = imageUrl, IsPrimary = true, AltText = name });
        return p;
    }

    public static List<Product> GetAllSeedProducts() => new()
    {
        CreateProduct(
            Guid.Parse("aaaaaaaa-1111-0000-0000-000000000001"),
            "Apple MacBook Pro 14\" (M3 Pro, 2024)", "macbook-pro-14-m3-pro", 1999.00m, "MBP-14-M3PRO", 35,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "The MacBook Pro 14-inch with M3 Pro chip delivers phenomenal performance for software developers, programmers, and data scientists. Features an 11-core CPU, 14-core GPU, and up to 18 hours of battery life with Liquid Retina XDR display.",
            "Ultimate development laptop with M3 Pro chip, 18GB unified memory, and 120Hz Liquid Retina XDR display.",
            "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&auto=format&fit=crop&q=80",
            new[] { "Apple M3 Pro chip with 11-core CPU and 14-core GPU", "Ideal for software development, Docker, compiling code, and machine learning", "14.2-inch Liquid Retina XDR display (3024x1964) with 120Hz ProMotion", "Up to 18 hours battery life with fast charging", "MagSafe 3, three Thunderbolt 4 / USB-C ports, HDMI, SDXC card slot" },
            new[] { ("Processor", "Apple M3 Pro (11-Core)"), ("Memory", "18GB Unified Memory"), ("Storage", "512GB NVMe SSD"), ("Display", "14.2-inch Liquid Retina XDR (120Hz)"), ("Battery Life", "Up to 18 hours"), ("Weight", "3.5 lbs (1.61 kg)"), ("Operating System", "macOS") }
        ),
        CreateProduct(
            Guid.Parse("aaaaaaaa-1111-0000-0000-000000000002"),
            "HP OMEN Transcend 16 Gaming Laptop (RTX 4070)", "hp-omen-transcend-16-rtx4070", 1699.99m, "HP-OMEN-16-4070", 25,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "High-performance HP OMEN Transcend 16 gaming powerhouse featuring an Intel Core i7-14700HX and NVIDIA GeForce RTX 4070 GPU. Boasts a breathtaking 16-inch 240Hz 2.5K display, OMEN Tempest Cooling, and RGB per-key keyboard for tournament-grade gaming and video editing.",
            "HP OMEN 16 gaming laptop with RTX 4070, Intel Core i7 14th Gen, and 240Hz 2.5K gaming display.",
            "https://images.unsplash.com/photo-1572605045937-565372815cea?w=800&auto=format&fit=crop&q=80",
            new[] { "NVIDIA GeForce RTX 4070 8GB GDDR6 GPU with DLSS 3.5 frame generation", "Intel Core i7-14700HX 20-core processor (up to 5.5 GHz)", "16-inch 2.5K (2560x1600) 240Hz 7ms IPS display with 100% sRGB", "OMEN Tempest Cooling technology with advanced thermal airflow", "Per-key RGB backlit gaming keyboard with anti-ghosting" },
            new[] { ("Processor", "Intel Core i7-14700HX"), ("Graphics", "NVIDIA RTX 4070 (8GB)"), ("Memory", "32GB DDR5-5600"), ("Storage", "1TB PCIe Gen4 NVMe"), ("Display", "16\" 2.5K 240Hz IPS"), ("Weight", "4.62 lbs (2.09 kg)"), ("Operating System", "Windows 11 Home") }
        ),
        CreateProduct(
            Guid.Parse("aaaaaaaa-1111-0000-0000-000000000003"),
            "Dell XPS 15 (9530) OLED Creator Laptop", "dell-xps-15-9530-oled", 1899.00m, "DELL-XPS15-OLED", 20,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "A masterclass in industrial design, combining high performance with a breathtaking 3.5K OLED touchscreen. Armed with an Intel Core i7-13700H and NVIDIA RTX 4060 GPU, it effortlessly handles 4K video editing, CAD design, and software engineering.",
            "Premium creator laptop with 15.6\" 3.5K OLED touch display, Intel i7, and RTX 4060 graphics.",
            "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=800&auto=format&fit=crop&q=80",
            new[] { "15.6-inch 3.5K (3456x2160) OLED InfinityEdge touchscreen with 100% DCI-P3", "Intel Core i7-13700H 14-core processor", "NVIDIA GeForce RTX 4060 8GB GDDR6 graphics", "CNC machined aluminum with carbon fiber palm rest", "Studio-quality quad speakers with Waves Nx 3D audio" },
            new[] { ("Processor", "Intel Core i7-13700H"), ("Graphics", "NVIDIA RTX 4060 (8GB)"), ("Memory", "32GB DDR5"), ("Storage", "1TB M.2 PCIe Gen4 SSD"), ("Display", "15.6\" 3.5K OLED Touch"), ("Weight", "4.23 lbs (1.92 kg)"), ("Operating System", "Windows 11 Pro") }
        ),
        CreateProduct(
            Guid.Parse("aaaaaaaa-1111-0000-0000-000000000004"),
            "Lenovo ThinkPad X1 Carbon Gen 12", "lenovo-thinkpad-x1-carbon-gen12", 1499.00m, "LEN-TP-X1C-G12", 25,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "The quintessential business ultrabook crafted with carbon fiber and magnesium alloy. Features an Intel Core Ultra 7 processor, legendary spill-resistant ThinkPad keyboard, TrackPoint, and enterprise security with dTPM 2.0 and fingerprint reader.",
            "Legendary business ultrabook weighing just 2.4 lbs with Intel Core Ultra 7 and MIL-STD durability.",
            "https://images.unsplash.com/photo-1541807084-5c52b6b3adef?w=800&auto=format&fit=crop&q=80",
            new[] { "Ultra-lightweight aerospace carbon fiber design weighing just 2.4 lbs", "Legendary ThinkPad ergonomic keyboard with TrackPoint and glass touchpad", "Intel Core Ultra 7 155H with Intel Evo certification", "14-inch 2.8K OLED display (400 nits, 100% DCI-P3)", "Enterprise security: Match-on-chip fingerprint reader, IR webcam with privacy shutter" },
            new[] { ("Processor", "Intel Core Ultra 7 155H"), ("Memory", "16GB LPDDR5X"), ("Storage", "512GB PCIe 4.0 SSD"), ("Display", "14\" 2.8K (2880x1800) OLED"), ("Weight", "2.42 lbs (1.09 kg)"), ("Operating System", "Windows 11 Pro") }
        ),
        CreateProduct(
            Guid.Parse("aaaaaaaa-1111-0000-0000-000000000005"),
            "ASUS ROG Zephyrus G16 (2024) Gaming Laptop", "asus-rog-zephyrus-g16", 2299.00m, "ASUS-ROG-G16-RTX4080", 18,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Precision-machined CNC aluminum gaming laptop boasting an Intel Core Ultra 9 processor and NVIDIA GeForce RTX 4080 GPU. Equipped with a stunning 16-inch 2.5K 240Hz OLED ROG Nebula display with 0.2ms response time.",
            "Top-tier gaming laptop with Core Ultra 9, NVIDIA RTX 4080, and 240Hz OLED ROG Nebula display.",
            "https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=800&auto=format&fit=crop&q=80",
            new[] { "NVIDIA GeForce RTX 4080 12GB GDDR6 GPU with DLSS 3.5 and full ray tracing", "Intel Core Ultra 9 185H with dedicated AI NPU acceleration", "16-inch 2.5K (2560x1600) 240Hz OLED Display with G-Sync and 0.2ms response time", "Advanced ROG Intelligent Cooling with liquid metal and vapor chamber", "Sleek thin-and-light CNC unibody chassis weighing only 4.3 lbs" },
            new[] { ("Processor", "Intel Core Ultra 9 185H (16 Cores)"), ("Graphics", "NVIDIA GeForce RTX 4080 (12GB GDDR6)"), ("Memory", "32GB LPDDR5X"), ("Storage", "1TB PCIe Gen4 SSD"), ("Display", "16\" 2.5K 240Hz OLED ROG Nebula"), ("Weight", "4.3 lbs (1.95 kg)"), ("Operating System", "Windows 11 Pro") }
        ),
        CreateProduct(
            Guid.Parse("aaaaaaaa-1111-0000-0000-000000000006"),
            "Apple MacBook Air 13\" (M3, Midnight)", "apple-macbook-air-13-m3-midnight", 1099.00m, "APPLE-MBA-13-M3", 40,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Incredibly thin and fast MacBook Air powered by the M3 chip. Offers up to 18 hours of battery life, a fanless silent design, and supports dual external monitors with the lid closed. Ideal for college students, travel, and front-end web development.",
            "Fanless ultraportable laptop with Apple M3 chip, 13.6\" Liquid Retina display, and 18-hour battery.",
            "https://images.unsplash.com/photo-1611186871348-b1ce696e52c9?w=800&auto=format&fit=crop&q=80",
            new[] { "Apple M3 8-core CPU with 8-core GPU", "Completely fanless, 100% silent operation", "13.6-inch Liquid Retina display with 500 nits brightness", "MagSafe 3 charging and two Thunderbolt / USB 4 ports", "Super slim aluminum unibody weighing only 2.7 lbs" },
            new[] { ("Processor", "Apple M3 (8-Core)"), ("Memory", "8GB Unified Memory"), ("Storage", "256GB SSD"), ("Display", "13.6\" Liquid Retina (2560x1664)"), ("Battery Life", "Up to 18 hours"), ("Weight", "2.7 lbs (1.24 kg)"), ("Operating System", "macOS") }
        ),
        CreateProduct(
            Guid.Parse("aaaaaaaa-1111-0000-0000-000000000007"),
            "Razer Blade 16 (2024) Gaming Laptop (RTX 4090)", "razer-blade-16-rtx4090-2024", 3799.00m, "RAZER-BLADE-16-4090", 12,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "The pinnacle of portable PC gaming, enclosed in an anodized CNC T6 aluminum unibody. Armed with NVIDIA GeForce RTX 4090 (175W TGP), Intel Core i9-14900HX, and the world's first 16-inch QHD+ 240Hz OLED gaming display.",
            "Ultra-enthusiast gaming laptop with RTX 4090, Intel Core i9-14900HX, and 240Hz OLED screen.",
            "https://images.unsplash.com/photo-1525547719571-a2d4ac8945e2?w=800&auto=format&fit=crop&q=80",
            new[] { "NVIDIA GeForce RTX 4090 16GB GDDR6 with 175W max TGP", "Intel Core i9-14900HX 24-core processor (up to 5.8 GHz)", "16-inch QHD+ (2560x1600) 240Hz 0.2ms OLED display with 100% DCI-P3", "Patented vacuum-sealed copper vapor chamber cooling", "CNC anodized aluminum unibody with anti-fingerprint coating" },
            new[] { ("Processor", "Intel Core i9-14900HX"), ("Graphics", "NVIDIA RTX 4090 (16GB)"), ("Memory", "32GB DDR5-5600"), ("Storage", "2TB PCIe Gen4 SSD"), ("Display", "16\" QHD+ 240Hz OLED"), ("Weight", "5.4 lbs (2.45 kg)"), ("Operating System", "Windows 11 Home") }
        ),
        CreateProduct(
            Guid.Parse("aaaaaaaa-1111-0000-0000-000000000008"),
            "Acer Aspire 5 15.6\" FHD Laptop (Core i5)", "acer-aspire-5-15-fhd", 649.00m, "ACER-ASP5-I5-FHD", 50,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "An affordable and capable laptop engineered for university students, budget programming, web development, and daily productivity. Powered by an Intel Core i5 13th Gen processor with 16GB DDR4 RAM and a fast 512GB PCIe SSD.",
            "Affordable budget laptop under $700 with Intel Core i5, 16GB RAM, perfect for programming and students.",
            "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=800&auto=format&fit=crop&q=80",
            new[] { "Affordable price under $700 for students and beginner coders", "Intel Core i5-1335U 10-core processor", "16GB RAM enables smooth multitasking with VS Code, browser tabs, and local development servers", "512GB PCIe NVMe SSD for snappy boot and load times", "15.6-inch Full HD (1920 x 1080) IPS anti-glare display" },
            new[] { ("Processor", "Intel Core i5-1335U (10 Cores)"), ("Memory", "16GB DDR4"), ("Storage", "512GB M.2 NVMe SSD"), ("Display", "15.6\" Full HD IPS"), ("Battery Life", "Up to 9 hours"), ("Weight", "3.88 lbs (1.76 kg)"), ("Operating System", "Windows 11 Home") }
        ),
        CreateProduct(
            Guid.Parse("aaaaaaaa-1111-0000-0000-000000000009"),
            "Framework Laptop 13 (Modular, Intel Core Ultra)", "framework-laptop-13-ultra-modular", 1049.00m, "FRAMEWORK-13-MOD", 25,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "The world's most modular and user-repairable ultrabook. Swap ports (USB-C, USB-A, HDMI, MicroSD, DisplayPort) in seconds with hot-swappable Expansion Cards. Features an Intel Core Ultra 7 processor and a 2.8K 120Hz 3:2 productivity display.",
            "100% modular and repairable 13.5\" laptop with swappable ports, Intel Core Ultra 7, and 3:2 screen.",
            "https://images.unsplash.com/photo-1531297484001-80022131f5a1?w=800&auto=format&fit=crop&q=80",
            new[] { "User-swappable modular Expansion Card system for customizable I/O ports", "Intel Core Ultra 7 155H processor with upgradable mainboard", "13.5-inch 2.8K (2880x1920) 120Hz 3:2 aspect ratio display", "100% repairable: Replacement parts, schematics, and upgrade guides freely available", "Hardware privacy switches for webcam and dual microphones" },
            new[] { ("Processor", "Intel Core Ultra 7 155H"), ("Memory", "16GB DDR5 (Socketed, Upgradable)"), ("Storage", "512GB M.2 2280 NVMe SSD"), ("Display", "13.5\" 2.8K 120Hz 3:2"), ("Weight", "2.86 lbs (1.3 kg)"), ("Operating System", "Windows 11 Home / Linux Ready") }
        ),
        CreateProduct(
            Guid.Parse("aaaaaaaa-1111-0000-0000-000000000010"),
            "Microsoft Surface Laptop 7 Copilot+ PC (Snapdragon X Elite)", "microsoft-surface-laptop-7-x-elite", 1199.00m, "MS-SURFACE-L7-ELITE", 30,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Next-generation Copilot+ PC powered by Qualcomm Snapdragon X Elite with 45 TOPS NPU for on-device AI. Features a 13.8-inch PixelSense Flow touchscreen with 120Hz refresh rate, ultra-thin aluminum chassis, and up to 20 hours of battery life.",
            "Next-gen AI Copilot+ laptop with Snapdragon X Elite, 120Hz PixelSense screen, and 20-hour battery.",
            "https://images.unsplash.com/photo-1515378791036-0648a3ef77b2?w=800&auto=format&fit=crop&q=80",
            new[] { "Qualcomm Snapdragon X Elite 12-core processor with 45 TOPS Hexagon NPU", "Up to 20 hours of continuous video playback battery life", "13.8-inch PixelSense Flow touchscreen (2304x1536) with 120Hz dynamic refresh", "Copilot key for instant access to AI productivity assistance", "Two USB-C / USB4 ports with support for up to three 4K monitors" },
            new[] { ("Processor", "Snapdragon X Elite (12-Core)"), ("Memory", "16GB LPDDR5X"), ("Storage", "512GB Gen4 SSD"), ("Display", "13.8\" PixelSense 120Hz Touch"), ("Weight", "2.96 lbs (1.34 kg)"), ("Operating System", "Windows 11 Home (Copilot+ PC)") }
        ),
        CreateProduct(
            Guid.Parse("aaaaaaaa-1111-0000-0000-000000000011"),
            "HP Spectre x360 14 (2024) 2-in-1 OLED Laptop", "hp-spectre-x360-14-oled-2024", 1449.00m, "HP-SPECTRE-14-2024", 22,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Luxurious 2-in-1 convertible with gem-cut aluminum chassis and a breathtaking 2.8K 120Hz OLED touchscreen. Features an Intel Core Ultra 7 processor, 9MP AI-enhanced webcam with auto-framing, and included rechargeable stylus pen.",
            "Premium 2-in-1 OLED convertible with Intel Core Ultra 7, 9MP AI webcam, and 120Hz OLED display.",
            "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=800&auto=format&fit=crop&q=80",
            new[] { "14-inch 2.8K (2880x1800) 120Hz OLED touch display with IMAX Enhanced certification", "Intel Core Ultra 7 155H processor with dedicated NPU", "9MP AI camera with hardware night vision and background blur", "Dual Thunderbolt 4 ports with USB-C charging", "Poly Studio quad speakers with DTS:X Ultra sound" },
            new[] { ("Processor", "Intel Core Ultra 7 155H"), ("Memory", "16GB LPDDR5X"), ("Storage", "1TB PCIe Gen4 NVMe"), ("Display", "14\" 2.8K 120Hz OLED Touch"), ("Weight", "3.19 lbs (1.45 kg)"), ("Operating System", "Windows 11 Home") }
        ),
        CreateProduct(
            Guid.Parse("aaaaaaaa-1111-0000-0000-000000000012"),
            "Lenovo Legion Pro 7i Gen 9 Gaming Laptop (RTX 4080)", "lenovo-legion-pro-7i-gen9", 2399.00m, "LEN-LEGION-PRO7I-4080", 16,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Competitive esports beast with AI-tuned Lenovo Legion ColdFront 5.0 cooling. Powered by an Intel Core i9-14900HX and full TGP NVIDIA GeForce RTX 4080. Features a 16\" WQXGA 240Hz PureSight Gaming display with 100% sRGB and G-Sync.",
            "Extreme performance gaming laptop with Core i9-14900HX, RTX 4080, and 240Hz PureSight display.",
            "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800&auto=format&fit=crop&q=80",
            new[] { "NVIDIA GeForce RTX 4080 12GB GDDR6 (175W max TGP)", "Intel Core i9-14900HX 24-core flagship processor", "16-inch WQXGA (2560x1600) 240Hz 500 nits PureSight display", "Lenovo LA-2 AI chip dynamically optimizes in-game FPS", "Legion TrueStrike per-key RGB gaming keyboard" },
            new[] { ("Processor", "Intel Core i9-14900HX"), ("Graphics", "NVIDIA GeForce RTX 4080 (12GB)"), ("Memory", "32GB DDR5-5600MHz"), ("Storage", "1TB PCIe Gen4 SSD"), ("Display", "16\" WQXGA 240Hz IPS"), ("Weight", "5.73 lbs (2.6 kg)"), ("Operating System", "Windows 11 Home") }
        ),
        CreateProduct(
            Guid.Parse("aaaaaaaa-1111-0000-0000-000000000013"),
            "MSI Stealth 16 AI Studio (Thin Workstation)", "msi-stealth-16-ai-studio-4070", 1899.00m, "MSI-STEALTH-16-4070", 20,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Slim magnesium-aluminum alloy workstation engineered for both creative studios and hardcore gaming. Fitted with Intel Core Ultra 9 185H, NVIDIA RTX 4070, and a 4K 120Hz Mini-LED display with 1000 nits peak brightness.",
            "Featherlight magnesium workstation with Core Ultra 9, RTX 4070, and 4K 120Hz Mini-LED display.",
            "https://images.unsplash.com/photo-1593642634367-d91a135587b5?w=800&auto=format&fit=crop&q=80",
            new[] { "Intel Core Ultra 9 185H processor with Intel AI Boost", "NVIDIA GeForce RTX 4070 8GB GDDR6 Laptop GPU (NVIDIA Studio certified)", "16-inch 4K (3840x2400) 120Hz Mini-LED display with VESA DisplayHDR 1000", "Magnesium-aluminum alloy chassis weighing only 4.38 lbs", "Dynaudio 6-speaker sound system with dual force-canceling woofers" },
            new[] { ("Processor", "Intel Core Ultra 9 185H"), ("Graphics", "NVIDIA RTX 4070 (8GB)"), ("Memory", "32GB DDR5"), ("Storage", "1TB NVMe Gen4 SSD"), ("Display", "16\" 4K 120Hz Mini-LED"), ("Weight", "4.38 lbs (1.99 kg)"), ("Operating System", "Windows 11 Pro") }
        ),
        CreateProduct(
            Guid.Parse("aaaaaaaa-1111-0000-0000-000000000014"),
            "ASUS Zenbook 14 OLED (Intel Core Ultra 7)", "asus-zenbook-14-oled-ultra7", 999.00m, "ASUS-ZENBOOK-14-U7", 35,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Sleek, featherlight premium ultrabook equipped with an Intel Core Ultra 7 AI processor and a gorgeous 3K 120Hz ASUS Lumina OLED display. Weighs under 2.8 lbs with a massive 75Wh battery that lasts up to 15 hours.",
            "Thin and light 14\" 3K OLED ultrabook with Intel Core Ultra 7, 75Wh battery, and 120Hz Lumina display.",
            "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=800&auto=format&fit=crop&q=80",
            new[] { "14-inch 3K (2880x1800) 120Hz ASUS Lumina OLED display with 0.2ms response time", "Intel Core Ultra 7 155H with integrated Intel Arc graphics", "Massive 75Wh battery provides up to 15 hours of real-world productivity", "Super slim 0.58\" aluminum chassis weighing only 2.82 lbs", "Superlinear speakers tuned by Harman Kardon with Dolby Atmos" },
            new[] { ("Processor", "Intel Core Ultra 7 155H"), ("Memory", "16GB LPDDR5X"), ("Storage", "1TB PCIe 4.0 SSD"), ("Display", "14\" 3K 120Hz OLED"), ("Weight", "2.82 lbs (1.28 kg)"), ("Operating System", "Windows 11 Home") }
        ),
        CreateProduct(
            Guid.Parse("aaaaaaaa-1111-0000-0000-000000000015"),
            "Samsung Galaxy Book4 Pro 360 (AMOLED 2-in-1)", "samsung-galaxy-book4-pro-360-amoled", 1549.00m, "SAMSUNG-BOOK4-PRO360", 20,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Stunning 16-inch 3K 120Hz Dynamic AMOLED 2X convertible with included S Pen stylus. Seamlessly connects with Galaxy ecosystem devices like phones and tablets for second screen multitasking and file transfer.",
            "16\" 3K 120Hz AMOLED 2-in-1 convertible laptop with S Pen and deep Samsung Galaxy ecosystem integration.",
            "https://images.unsplash.com/photo-1587614382346-4ec70e388b28?w=800&auto=format&fit=crop&q=80",
            new[] { "16-inch 3K (2880x1800) Dynamic AMOLED 2X 120Hz touchscreen with anti-reflective glass", "Intel Core Ultra 7 155H processor with Intel Arc graphics", "Included responsive S Pen for sketching, note taking, and diagramming", "Galaxy Connected Experience: Phone Link, Multi Control, Second Screen with Tab S9", "AKG quad-speaker system with Dolby Atmos" },
            new[] { ("Processor", "Intel Core Ultra 7 155H"), ("Memory", "16GB LPDDR5X"), ("Storage", "1TB PCIe SSD"), ("Display", "16\" 3K 120Hz AMOLED Touch"), ("Weight", "3.66 lbs (1.66 kg)"), ("Operating System", "Windows 11 Home") }
        ),
        CreateProduct(
            Guid.Parse("bbbbbbbb-2222-0000-0000-000000000001"),
            "Samsung Galaxy S24 Ultra 5G (Titanium Gray)", "samsung-galaxy-s24-ultra-5g", 1299.00m, "SAMSUNG-S24U-TI", 40,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "The ultimate Android camera phone and AI powerhouse. Equipped with a revolutionary quad camera system headlined by a 200MP wide sensor, 50MP 5x periscope optical zoom, and 100x Space Zoom. Features an anti-reflective flat 6.8\" Dynamic AMOLED 2X 120Hz display, titanium frame, integrated S Pen stylus, and Galaxy AI photo editing tools.",
            "Industry-leading smartphone camera with 200MP sensor, 100x Space Zoom, titanium frame, and built-in S Pen.",
            "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?w=800&auto=format&fit=crop&q=80",
            new[] { "Pro-grade 200MP main camera captures extraordinary detail even in low light", "50MP periscope telephoto lens with 5x optical zoom and 100x digital Space Zoom", "Galaxy AI photo assistance: Generative edit, object eraser, instant slow-mo", "6.8-inch QHD+ Dynamic AMOLED 2X 1-120Hz display with Corning Gorilla Armor anti-reflective glass", "Built-in S Pen for drawing, note-taking, and camera remote control", "Snapdragon 8 Gen 3 for Galaxy with massive 5,000 mAh all-day battery" },
            new[] { ("Camera System", "200MP Wide + 50MP 5x Telephoto + 10MP 3x Telephoto + 12MP Ultra-wide"), ("Front Camera", "12MP Dual Pixel AF"), ("Display", "6.8-inch Dynamic AMOLED 2X (3120x1440, 120Hz)"), ("Processor", "Snapdragon 8 Gen 3 for Galaxy"), ("Battery", "5000 mAh (45W fast charging)"), ("Water Resistance", "IP68 dust/water resistant") }
        ),
        CreateProduct(
            Guid.Parse("bbbbbbbb-2222-0000-0000-000000000002"),
            "Apple iPhone 15 Pro Max (Natural Titanium)", "apple-iphone-15-pro-max-natural-titanium", 1199.00m, "APPLE-IPHONE-15PM-TI", 45,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Forged in aerospace-grade titanium with contoured edges and the Action button. Features the groundbreaking A17 Pro chip, 5x telephoto optical zoom with tetraprism design, USB-C with 10Gbps transfer speeds, and ProRes 4K60 video recording to external drives.",
            "Flagship iPhone in lightweight titanium with A17 Pro chip, 5x optical zoom, and 48MP main camera.",
            "https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=800&auto=format&fit=crop&q=80",
            new[] { "Aerospace-grade titanium design with textured matte glass back", "A17 Pro chip with 6-core GPU and hardware-accelerated ray tracing", "48MP Main camera with 24MP super-high-resolution default and 5x Telephoto optical zoom", "Customizable Action button for camera, flashlight, or shortcuts", "USB-C connector with USB 3 speeds up to 10Gb/s" },
            new[] { ("Camera System", "48MP Main + 12MP Ultra-wide + 12MP 5x Telephoto"), ("Processor", "Apple A17 Pro"), ("Display", "6.7-inch Super Retina XDR OLED (120Hz ProMotion)"), ("Battery Life", "Up to 29 hours video playback"), ("Water Resistance", "IP68 (6m up to 30 mins)") }
        ),
        CreateProduct(
            Guid.Parse("bbbbbbbb-2222-0000-0000-000000000003"),
            "Google Pixel 9 Pro (Tensor G4)", "google-pixel-9-pro-tensor-g4", 999.00m, "GOOGLE-PIXEL-9P-T4", 30,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Google's premier smartphone engineered for computational photography, AI features, and clean pure Android experience. The 50MP triple camera system with Super Res Zoom, Night Sight Video, and Add Me AI photography captures true-to-life colors and stunning portrait shots in every lighting condition.",
            "Top smartphone camera powered by Google Tensor G4 AI, Night Sight Video, and 50MP triple camera.",
            "https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=800&auto=format&fit=crop&q=80",
            new[] { "Award-winning computational photography with natural skin tones via Real Tone", "50MP Octa PD main camera, 48MP ultra-wide with Macro Focus, 48MP 5x telephoto", "Night Sight for photos and 8K Video Boost", "Powered by Google Tensor G4 with 16GB RAM for on-device Gemini AI", "7 years of OS, security, and Pixel Drop updates" },
            new[] { ("Camera System", "50MP Wide + 48MP Ultra-Wide + 48MP 5x Telephoto"), ("Processor", "Google Tensor G4 with Titan M2 security"), ("Display", "6.3-inch Super Actua LTPO OLED (1-120Hz, 3000 nits)"), ("Memory", "16GB RAM"), ("Battery", "4700 mAh"), ("Operating System", "Android 15") }
        ),
        CreateProduct(
            Guid.Parse("bbbbbbbb-2222-0000-0000-000000000004"),
            "OnePlus 12 5G (Hasselblad Camera)", "oneplus-12-5g-hasselblad", 799.00m, "ONEPLUS-12-5G", 35,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Flagship killer specs at a competitive price. Offers Snapdragon 8 Gen 3, 4th Gen Hasselblad Camera System, 5400 mAh battery with blistering 80W SUPERVOOC wired charging, and 50W AIRVOOC wireless charging.",
            "High-spec flagship phone with 4th Gen Hasselblad camera, Snapdragon 8 Gen 3, and 80W fast charging.",
            "https://images.unsplash.com/photo-1565849904461-04a58ad377e0?w=800&auto=format&fit=crop&q=80",
            new[] { "Qualcomm Snapdragon 8 Gen 3 with Trinity Engine performance tuning", "4th Gen Hasselblad Camera System: 50MP Sony LYT-808, 64MP 3x Periscope, 48MP Ultra-wide", "6.82-inch 2K 120Hz ProXDR display with 4500 nits peak brightness", "Ultra-large 5,400 mAh battery reaches 100% in 30 minutes via 80W SUPERVOOC", "Aqua Touch ensures responsive touch even with wet hands or in rain" },
            new[] { ("Camera System", "50MP LYT-808 + 64MP 3x Periscope + 48MP Ultra-wide"), ("Processor", "Snapdragon 8 Gen 3"), ("Display", "6.82-inch 2K 120Hz LTPO AMOLED (4500 nits)"), ("Battery", "5400 mAh (80W wired / 50W wireless)"), ("Memory", "12GB LPDDR5X") }
        ),
        CreateProduct(
            Guid.Parse("bbbbbbbb-2222-0000-0000-000000000005"),
            "Apple iPhone 15 (Blue, Dynamic Island)", "apple-iphone-15-blue-dynamic-island", 799.00m, "APPLE-IPHONE-15-BLUE", 50,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Equipped with Dynamic Island for interactive alerts, a 48MP Main camera with 2x optical-quality telephoto, color-infused glass back, and USB-C. Powered by the proven A16 Bionic chip.",
            "Modern standard iPhone with Dynamic Island, 48MP camera, and USB-C connectivity.",
            "https://images.unsplash.com/photo-1592750475338-74b7b21085ab?w=800&auto=format&fit=crop&q=80",
            new[] { "Dynamic Island bubbles up alerts and live activities so you do not miss them", "48MP Main camera captures super-high-resolution photos with 2x telephoto crop", "Color-infused back glass with ceramic shield front", "Apple A16 Bionic chip delivers smooth performance and efficiency", "Universal USB-C charging connector" },
            new[] { ("Camera System", "48MP Main + 12MP Ultra-wide"), ("Processor", "Apple A16 Bionic"), ("Display", "6.1-inch Super Retina XDR OLED (2000 nits peak)"), ("Battery Life", "Up to 20 hours video playback"), ("Water Resistance", "IP68") }
        ),
        CreateProduct(
            Guid.Parse("bbbbbbbb-2222-0000-0000-000000000006"),
            "Samsung Galaxy Z Fold6 5G (Silver Shadow)", "samsung-galaxy-z-fold6-silver", 1899.00m, "SAMSUNG-ZFOLD-6-SILVER", 15,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Refined book-style foldable that opens into a tablet-sized 7.6\" Dynamic AMOLED 2X display. Thinner and lighter with improved dual-rail hinge, IP48 rating, S Pen Fold Edition support, and AI multitasking.",
            "Flagship book-style foldable phone with 7.6\" inner screen, S Pen support, and Snapdragon 8 Gen 3.",
            "https://images.unsplash.com/photo-1580910051074-3eb694886505?w=800&auto=format&fit=crop&q=80",
            new[] { "7.6-inch inner foldable Dynamic AMOLED 2X 120Hz display with 2600 nits brightness", "6.3-inch outer cover screen with comfortable 22:9 aspect ratio", "Slimmer and lighter design at 239g with dual-rail FlexHinge", "Enhanced multitasking: Run up to three apps simultaneously", "Snapdragon 8 Gen 3 with enlarged vapor chamber cooling" },
            new[] { ("Displays", "7.6\" Inner QXGA+ 120Hz + 6.3\" Outer HD+ 120Hz"), ("Camera", "50MP Main + 10MP 3x Telephoto + 12MP Ultra-wide"), ("Processor", "Snapdragon 8 Gen 3"), ("Memory", "12GB RAM"), ("Battery", "4400 mAh (25W wired / 15W wireless)") }
        ),
        CreateProduct(
            Guid.Parse("bbbbbbbb-2222-0000-0000-000000000007"),
            "Google Pixel 8a (Best Budget Camera Phone)", "google-pixel-8a-budget-camera", 499.00m, "GOOGLE-PIXEL-8A-OLED", 45,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "The undisputed champion of smartphone photography under $500. Features a 64MP dual rear camera system with Google Tensor G3, Best Take, Audio Magic Eraser, and a 120Hz Actua display.",
            "Best camera phone under $500 with 64MP sensor, Google Tensor G3, and 120Hz OLED screen.",
            "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=800&auto=format&fit=crop&q=80",
            new[] { "Unbeatable camera performance under $500 with 64MP Quad PD main sensor", "Google Tensor G3 chip brings flagship AI photo editing tools", "Smooth 6.1-inch Actua OLED display with 120Hz refresh rate", "Best Take merges group photos so everyone looks their best", "7 years of OS updates and Feature Drops" },
            new[] { ("Camera System", "64MP Main (OIS) + 13MP Ultra-wide"), ("Processor", "Google Tensor G3"), ("Display", "6.1-inch Actua OLED 120Hz (1080x2400)"), ("Battery", "4492 mAh"), ("Water Resistance", "IP67") }
        ),
        CreateProduct(
            Guid.Parse("bbbbbbbb-2222-0000-0000-000000000008"),
            "Motorola Razr+ 2024 (Flip Phone, 4\" Cover Display)", "motorola-razr-plus-2024-flip", 999.00m, "MOTO-RAZR-PLUS-FLIP", 25,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Showstopping flip phone featuring the industry's largest 4.0\" 165Hz LTPO cover display, IPX8 underwater protection, telephoto portrait camera, and Snapdragon 8s Gen 3 processor.",
            "Fashion-forward flip phone with giant 4.0\" 165Hz cover screen and telephoto portrait camera.",
            "https://images.unsplash.com/photo-1574944985070-8f3ebc6b79d2?w=800&auto=format&fit=crop&q=80",
            new[] { "Industry-leading 4.0-inch 165Hz LTPO outer display runs virtually any Android app", "6.9-inch 165Hz FHD+ pOLED foldable main screen with Dolby Vision", "50MP main camera + 50MP 2x telephoto camera with AI photo enhancement", "IPX8 water resistance rating allows submersion in fresh water", "Premium vegan leather finish in iconic Pantone colors" },
            new[] { ("Displays", "6.9\" 165Hz pOLED Inner + 4.0\" 165Hz pOLED Outer"), ("Camera", "50MP Main (OIS) + 50MP 2x Telephoto"), ("Processor", "Snapdragon 8s Gen 3"), ("Battery", "4000 mAh (45W wired / 15W wireless)"), ("Water Resistance", "IPX8") }
        ),
        CreateProduct(
            Guid.Parse("bbbbbbbb-2222-0000-0000-000000000009"),
            "Nothing Phone (2) (Transparent Glyph Interface)", "nothing-phone-2-glyph-transparent", 599.00m, "NOTHING-PHONE-2-GLYPH", 35,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Striking semi-transparent smartphone featuring the iconic Glyph Interface with customizable LED notification light strips. Powered by Snapdragon 8+ Gen 1 with bloat-free Nothing OS 2.5 and dual 50MP Sony cameras.",
            "Designer semi-transparent phone with interactive Glyph LED lighting and clean Nothing OS.",
            "https://images.unsplash.com/photo-1585060544812-6b45742d762f?w=800&auto=format&fit=crop&q=80",
            new[] { "Unique Glyph Interface LED light strips convey timer progress, volume, and essential notifications", "Clean Nothing OS offers a monochromatic, bloat-free Android experience", "Dual 50MP camera system with Sony IMX890 OIS main sensor", "6.7-inch flexible LTPO OLED 120Hz display", "Sustainably built using 100% recycled aluminum frame" },
            new[] { ("Camera System", "50MP Sony IMX890 (OIS) + 50MP Samsung JN1 Ultra-wide"), ("Processor", "Snapdragon 8+ Gen 1"), ("Display", "6.7-inch LTPO OLED 120Hz (1600 nits)"), ("Battery", "4700 mAh (45W wired / 15W wireless)"), ("Unique Feature", "Glyph LED notification matrix") }
        ),
        CreateProduct(
            Guid.Parse("bbbbbbbb-2222-0000-0000-000000000010"),
            "Sony Xperia 1 VI (Continuous Optical Zoom)", "sony-xperia-1-vi-optical-zoom", 1299.00m, "SONY-XPERIA-1-VI", 18,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Professional photography and video creation smartphone featuring true continuous optical zoom (85mm-170mm), Exmor T for mobile sensor, dedicated two-stage camera shutter button, and audiophile 3.5mm jack.",
            "Pro camera smartphone with 85-170mm continuous optical zoom, dedicated shutter button, and 3.5mm jack.",
            "https://images.unsplash.com/photo-1567581935884-3349723552ca?w=800&auto=format&fit=crop&q=80",
            new[] { "True optical continuous zoom lens ranging from 85mm to 170mm (3.5x to 7.1x)", "Exmor T for mobile stacked sensor delivers full-frame-inspired low-light noise performance", "Dedicated mechanical two-stage camera shutter button on edge", "Audiophile-grade 3.5mm headphone jack with Hi-Res Audio and LDAC", "Snapdragon 8 Gen 3 with 5,000 mAh battery offering 2-day battery life" },
            new[] { ("Camera System", "48MP 24mm Exmor T + 12MP 85-170mm Continuous Optical Zoom + 12MP Ultra-wide"), ("Display", "6.5-inch FHD+ 1-120Hz LTPO OLED (19.5:9, 1500 nits)"), ("Processor", "Snapdragon 8 Gen 3"), ("Battery", "5000 mAh (30W)"), ("Audio", "3.5mm jack + Full-stage stereo speakers") }
        ),
        CreateProduct(
            Guid.Parse("bbbbbbbb-2222-0000-0000-000000000011"),
            "ASUS ROG Phone 8 Pro (Dedicated Gaming Phone)", "asus-rog-phone-8-pro-165hz", 1199.00m, "ASUS-ROG-PHONE-8P", 20,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "The ultimate mobile esports gaming weapon. Features an ultra-fast 165Hz Samsung AMOLED screen, ultrasonic AirTrigger shoulder buttons, AniMe Vision customizable mini-LED rear display, and GameCool 8 thermal system.",
            "Ultimate gaming smartphone with 165Hz AMOLED, ultrasonic AirTriggers, and Snapdragon 8 Gen 3.",
            "https://images.unsplash.com/photo-1546868871-7041f2a55e12?w=800&auto=format&fit=crop&q=80",
            new[] { "6.78-inch 165Hz Samsung E6 AMOLED display with 720Hz touch sampling rate", "Ultrasonic AirTrigger buttons provide console-like L1/R1 shoulder buttons", "AniMe Vision matrix display on back with 341 programmable mini-LEDs", "Dual USB-C ports (one on bottom, one on side for gaming while charging)", "IP68 water and dust resistant gaming chassis" },
            new[] { ("Processor", "Snapdragon 8 Gen 3 (Overclocked)"), ("Display", "6.78-inch 165Hz AMOLED (2500 nits)"), ("Memory", "16GB LPDDR5X"), ("Storage", "512GB UFS 4.0"), ("Battery", "5500 mAh (65W HyperCharge)"), ("Gaming Features", "AirTrigger Ultrasonic buttons + Side USB-C") }
        ),
        CreateProduct(
            Guid.Parse("bbbbbbbb-2222-0000-0000-000000000012"),
            "Xiaomi 14 Ultra (Leica 1-Inch Sensor)", "xiaomi-14-ultra-leica-quad", 1299.00m, "XIAOMI-14-ULTRA-LEICA", 22,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Photography powerhouse developed with Leica. Features a massive 1-inch Sony LYT-900 sensor with stepless variable aperture (f/1.63 to f/4.0), dual telephoto cameras (3.2x and 5x periscope), and titanium edition chassis.",
            "Pro photography flagship with 1-inch LYT-900 sensor, Leica quad cameras, and stepless variable aperture.",
            "https://images.unsplash.com/photo-1598327776483-e8ec4d8e8749?w=800&auto=format&fit=crop&q=80",
            new[] { "1-inch Sony LYT-900 main sensor with stepless f/1.63 - f/4.0 variable aperture", "Leica Summilux optical lenses across all four 50MP sensors", "Dual periscope/telephoto lenses: 75mm floating telephoto + 120mm periscope", "6.73-inch 2K 120Hz LTPO AMOLED display with 3000 nits peak brightness", "90W HyperCharge wired and 80W wireless charging" },
            new[] { ("Camera System", "50MP 1\" Variable Aperture + 50MP 3.2x Telephoto + 50MP 5x Periscope + 50MP Ultra-wide"), ("Processor", "Snapdragon 8 Gen 3"), ("Display", "6.73-inch WQHD+ AMOLED (1-120Hz)"), ("Battery", "5000 mAh (90W wired / 80W wireless)"), ("Water Resistance", "IP68") }
        ),
        CreateProduct(
            Guid.Parse("bbbbbbbb-2222-0000-0000-000000000013"),
            "Samsung Galaxy A54 5G (Awesome Graphite)", "samsung-galaxy-a54-5g-graphite", 399.00m, "SAMSUNG-A54-GRAPHITE", 60,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "An affordable mid-range smartphone that punches well above its price tag, especially in camera quality. Features a 50MP main sensor with Optical Image Stabilization (OIS) and Nightography, a vibrant 6.4\" Super AMOLED 120Hz screen, and IP67 water resistance.",
            "Affordable budget smartphone under $450 with 50MP OIS camera, 120Hz AMOLED screen, and IP67 rating.",
            "https://images.unsplash.com/photo-1605236453806-6ff36851218e?w=800&auto=format&fit=crop&q=80",
            new[] { "Budget-friendly price under $450 with flagship-inspired design", "50MP main camera with Optical Image Stabilization (OIS) for sharp blur-free shots", "Nightography low-light camera capabilities in an affordable phone", "6.4-inch FHD+ Super AMOLED 120Hz display", "5,000 mAh battery offering up to 2 days of typical usage", "IP67 dust and water resistance rating" },
            new[] { ("Camera System", "50MP Main (OIS) + 12MP Ultra-wide + 5MP Macro"), ("Front Camera", "32MP Selfie Camera"), ("Display", "6.4-inch Super AMOLED 120Hz"), ("Battery", "5000 mAh"), ("Storage", "128GB (MicroSD expandable up to 1TB)"), ("Water Resistance", "IP67") }
        ),
        CreateProduct(
            Guid.Parse("bbbbbbbb-2222-0000-0000-000000000014"),
            "Apple iPhone 13 (128GB, Midnight)", "apple-iphone-13-128gb-midnight", 599.00m, "APPLE-IPHONE-13-MIDNIGHT", 45,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "An accessible entry into the Apple ecosystem offering incredible value. Dual camera system with sensor-shift optical image stabilization, A15 Bionic chip, and all-day battery life.",
            "Best value budget iPhone under $600 with A15 Bionic and sensor-shift OIS camera.",
            "https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?w=800&auto=format&fit=crop&q=80",
            new[] { "Great budget iPhone entry under $600", "A15 Bionic chip with 4-core GPU", "Dual 12MP camera system with Cinematic mode (1080p at 30 fps)", "Sensor-shift optical image stabilization keeps shots steady", "Durable Ceramic Shield front and IP68 water resistance" },
            new[] { ("Camera System", "12MP Wide + 12MP Ultra-wide"), ("Processor", "Apple A15 Bionic"), ("Display", "6.1-inch Super Retina XDR OLED"), ("Battery Life", "Up to 19 hours video playback"), ("Operating System", "iOS 18") }
        ),
        CreateProduct(
            Guid.Parse("bbbbbbbb-2222-0000-0000-000000000015"),
            "Motorola Moto G Power 5G (5000mAh Battery)", "motorola-moto-g-power-5g-2024", 279.00m, "MOTO-G-POWER-5G-2024", 65,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Endurance champion with a stylish vegan leather back, 5,000 mAh battery delivering up to 2 days of battery life, 50MP Quad Pixel camera with OIS, and 30W TurboPower fast charging.",
            "Sub-$300 battery monster phone with 2-day battery life, vegan leather back, and 50MP OIS camera.",
            "https://images.unsplash.com/photo-1565630916779-e303be97b6f5?w=800&auto=format&fit=crop&q=80",
            new[] { "Multi-day 5,000 mAh battery easily lasts through 2 full days of use", "50MP camera with Optical Image Stabilization (OIS) for sharp night shots", "6.7-inch Full HD+ 120Hz display with stereo speakers and Dolby Atmos", "Supports both 30W wired charging and 15W wireless charging", "Soft-touch vegan leather back prevents fingerprint smudges" },
            new[] { ("Camera System", "50MP Main (OIS) + 8MP Ultra-wide/Macro"), ("Processor", "MediaTek Dimensity 7020 5G"), ("Display", "6.7-inch FHD+ 120Hz (1080x2400)"), ("Battery", "5000 mAh (30W TurboPower)"), ("Storage", "128GB (Expandable up to 1TB)") }
        ),
        CreateProduct(
            Guid.Parse("cccccccc-3333-0000-0000-000000000001"),
            "Sony WH-1000XM5 Wireless Noise-Canceling Headphones", "sony-wh-1000xm5-wireless-headphones", 399.99m, "SONY-WH1000XM5-BLK", 50,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Industry-leading noise-canceling headphones powered by two processors and 8 microphones. Delivers superlative Hi-Res wireless audio quality with LDAC, crystal clear hands-free calling, and exceptional 30-hour battery life with 3-minute quick charging.",
            "Industry-standard noise-canceling wireless headphones with 30-hour battery and Hi-Res LDAC sound.",
            "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&auto=format&fit=crop&q=80",
            new[] { "Two processors and 8 microphones control industry-leading active noise cancellation", "Ultra-comfortable lightweight design with soft fit leather", "Hi-Res Audio and Hi-Res Audio Wireless supported with Sony LDAC codec", "Up to 30 hours of continuous battery life on a single charge", "Multipoint Bluetooth pairing: Seamlessly switch between laptop and phone" },
            new[] { ("Battery Life", "Up to 30 hours (ANC On), up to 40 hours (ANC Off)"), ("Fast Charging", "3 min charge = 3 hours playback"), ("Bluetooth Version", "Bluetooth 5.2 (LDAC, AAC, SBC)"), ("Weight", "250 grams (8.8 oz)"), ("Noise Cancellation", "Integrated Processor V1 + HD QN1 processor") }
        ),
        CreateProduct(
            Guid.Parse("cccccccc-3333-0000-0000-000000000002"),
            "Apple AirPods Pro (2nd Generation, USB-C)", "apple-airpods-pro-2-usb-c", 249.00m, "APPLE-APP2-USBC", 75,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "AirPods Pro 2 feature up to 2x more Active Noise Cancellation, Adaptive Audio that tailors noise control to your environment, and Transparency mode to hear the world around you. Includes personalized Spatial Audio with dynamic head tracking and a MagSafe Charging Case (USB-C).",
            "Pro wireless in-ear earbuds with up to 2x more ANC, Adaptive Audio, and USB-C MagSafe case.",
            "https://images.unsplash.com/photo-1600294037681-c80b4cb5b434?w=800&auto=format&fit=crop&q=80",
            new[] { "Apple H2 headphone chip powers computational audio and advanced ANC", "Up to 2x more active noise cancellation than previous generation", "Adaptive Audio automatically blends ANC and Transparency mode", "Personalized Spatial Audio with dynamic head tracking", "MagSafe Case (USB-C) with Precision Finding via U1 chip and built-in speaker" },
            new[] { ("Chip", "Apple H2 headphone chip, Apple U1 in case"), ("Battery Life", "Up to 6 hours listening (up to 30 hours with case)"), ("Charging", "USB-C, MagSafe, Apple Watch charger, Qi-certified"), ("Water Resistance", "IP54 for earbuds and case") }
        ),
        CreateProduct(
            Guid.Parse("cccccccc-3333-0000-0000-000000000003"),
            "Bose QuietComfort Ultra Wireless Noise-Canceling Headphones", "bose-quietcomfort-ultra-headphones", 429.00m, "BOSE-QC-ULTRA-HP", 35,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "World-class noise cancellation, breakthrough spatialized Bose Immersive Audio, and elevated materials for maximum comfort. CustomTune technology personalizes sound to your ear shape.",
            "Bose flagship headphones featuring Bose Immersive Audio and custom-tuned noise cancellation.",
            "https://images.unsplash.com/photo-1546435770-a3e426bf472b?w=800&auto=format&fit=crop&q=80",
            new[] { "Bose Immersive Audio pushes boundaries with spatialized soundstage", "CustomTune technology analyzes your ears to adapt audio and active noise cancelling", "Quiet Mode, Aware Mode with ActiveSense, and Immersion Mode", "Up to 24 hours of battery life (up to 18 hours with Immersive Audio)", "Ultra-soft protein leather ear cushions and lightweight aluminum arms" },
            new[] { ("Battery Life", "Up to 24 hours"), ("Audio Features", "Bose Immersive Spatial Audio + CustomTune"), ("Bluetooth", "Bluetooth 5.3 with Snapdragon Sound"), ("Microphones", "Advanced noise-rejecting microphone array"), ("Weight", "252 grams") }
        ),
        CreateProduct(
            Guid.Parse("cccccccc-3333-0000-0000-000000000004"),
            "Apple AirPods Max Wireless Over-Ear Headphones (Space Gray)", "apple-airpods-max-space-gray", 549.00m, "APPLE-AIRPODS-MAX-SG", 25,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Computational over-ear luxury headphones with custom acoustic design, Apple H1 chip in each cup, high-fidelity audio, Active Noise Cancellation with Transparency mode, and knit-mesh canopy headband for pillow-soft comfort.",
            "Luxury over-ear headphones with custom dynamic driver, computational audio, and spatial sound.",
            "https://images.unsplash.com/photo-1583394838336-acd977736f90?w=800&auto=format&fit=crop&q=80",
            new[] { "Apple-designed dynamic driver provides high-fidelity audio reproduction", "Active Noise Cancellation with Transparency mode to hear the world around you", "Personalized Spatial Audio with dynamic head tracking gives theater-like sound", "Knit-mesh canopy and acoustically engineered memory foam ear cushions", "Digital Crown allows precise volume control, track skipping, and phone calls" },
            new[] { ("Battery Life", "Up to 20 hours listening with ANC enabled"), ("Chips", "Dual Apple H1 headphone chips (one per earcup)"), ("Weight", "384.8 grams"), ("Charging", "Smart Case power saving + USB-C/Lightning"), ("Color", "Space Gray") }
        ),
        CreateProduct(
            Guid.Parse("cccccccc-3333-0000-0000-000000000005"),
            "Sennheiser Momentum 4 Wireless Audiophile Headphones", "sennheiser-momentum-4-wireless", 349.95m, "SENN-MOMENTUM-4", 30,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Audiophile-inspired 42mm transducer system delivering Sennheiser signature sound. Features an astonishing 60-hour battery life, Adaptive Noise Cancellation, Transparency Mode, and lightweight ergonomic design.",
            "Audiophile-grade wireless headphones with unrivaled 60-hour battery life and 42mm sound system.",
            "https://images.unsplash.com/photo-1590658268037-6bf12165a8df?w=800&auto=format&fit=crop&q=80",
            new[] { "Sennheiser signature sound powered by audiophile-grade 42mm transducer system", "Incredible 60-hour battery life on a single charge with fast charging", "Next-generation Adaptive Noise Cancellation automatically adjusts to ambient noise", "Built-in EQ, sound presets, and Sound Personalization via Sennheiser Smart Control app", "Lightweight design with padded headband and deeply cushioned earpads" },
            new[] { ("Battery Life", "Up to 60 hours with Bluetooth and ANC"), ("Transducer Size", "42mm diameter dynamic"), ("Codecs", "aptX, aptX Adaptive, AAC, SBC"), ("Weight", "293 grams"), ("Frequency Response", "6 Hz to 22,000 Hz") }
        ),
        CreateProduct(
            Guid.Parse("cccccccc-3333-0000-0000-000000000006"),
            "SteelSeries Arctis Nova Pro Wireless Gaming Headset", "steelseries-arctis-nova-pro-wireless", 349.99m, "SS-NOVA-PRO-WL", 45,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "The benchmark wireless gaming headset built specifically for PC and console gamers. Features ultra-low latency 2.4GHz wireless audio, simultaneous Bluetooth audio mixing, Active Noise Cancellation (ANC), and an Infinity Power System with hot-swappable dual batteries.",
            "Elite wireless gaming headset with 2.4GHz lag-free wireless, ANC, dual hot-swap batteries, and Sonar spatial audio.",
            "https://images.unsplash.com/photo-1618366712010-f4ae9c647dcb?w=800&auto=format&fit=crop&q=80",
            new[] { "Engineered specifically for gaming with ultra-low latency 2.4GHz wireless connection", "Simultaneous 2.4GHz and Bluetooth to game while chatting on Discord or mobile", "Active Noise Cancellation (ANC) with 4-mic hybrid system isolates in-game sound", "Infinity Power System: Two hot-swappable batteries for unlimited continuous gaming", "ClearCast Gen 2 AI-powered bidirectional retractable noise-canceling microphone" },
            new[] { ("Audio Connectivity", "Lag-free 2.4GHz Wireless + Bluetooth 5.0 + 3.5mm wired"), ("Battery Life", "Up to 44 hours (22 hours per battery, hot-swappable)"), ("Drivers", "40mm Neodymium Drivers (Hi-Res certified)"), ("Frequency Response", "10–40,000 Hz"), ("Microphone", "ClearCast Gen 2 fully retractable with AI noise cancel") }
        ),
        CreateProduct(
            Guid.Parse("cccccccc-3333-0000-0000-000000000007"),
            "Audio-Technica ATH-M50xBT2 Studio Monitor Headphones", "audio-technica-ath-m50xbt2", 199.00m, "AT-ATH-M50XBT2", 40,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "The wireless iteration of the legendary ATH-M50x professional studio monitor headphones. Features proprietary 45mm large-aperture drivers, AK4331 advanced audio DAC, low-latency mode for gaming and video, and 50 hours of battery life.",
            "Legendary studio monitor sound now wireless with 45mm drivers, LDAC support, and 50h battery.",
            "https://images.unsplash.com/photo-1577174881658-0f30ed549adc?w=800&auto=format&fit=crop&q=80",
            new[] { "Proprietary 45mm large-aperture drivers deliver critically acclaimed sonic performance", "AK4331 advanced audio DAC with internal headphone amp supports LDAC codec", "Dual beamforming microphones ensure crystal-clear telephone and meeting audio", "Low Latency Mode improves synchronicity between audio and video", "Up to 50 hours of continuous use on a full charge" },
            new[] { ("Driver Diameter", "45mm Neodymium magnets"), ("Battery Life", "Up to 50 hours"), ("Bluetooth Codecs", "LDAC, AAC, SBC"), ("Frequency Response", "15 - 28,000 Hz"), ("Weight", "307 grams") }
        ),
        CreateProduct(
            Guid.Parse("cccccccc-3333-0000-0000-000000000008"),
            "Sony WF-1000XM5 True Wireless Earbuds", "sony-wf-1000xm5-wireless-earbuds", 299.99m, "SONY-WF1000XM5-EARBUD", 55,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Flagship noise-canceling true wireless earbuds equipped with Sony's Dynamic Driver X, dual proprietary processors (HD Noise Canceling Processor QN2e and Integrated Processor V2), bone conduction sensors, and AI-based noise reduction.",
            "Sony flagship noise-canceling earbuds with Dynamic Driver X, dual processors, and bone conduction mic.",
            "https://images.unsplash.com/photo-1590658006821-04f4008d5717?w=800&auto=format&fit=crop&q=80",
            new[] { "Dynamic Driver X reproduces rich vocals and deep, undistorted bass", "Two proprietary chips deliver the world's best true wireless active noise cancellation", "AI-based noise reduction algorithm with bone conduction sensors for ultra-clear calls", "Multipoint connection links two Bluetooth devices simultaneously", "Water resistant IPX4 rating against sweat and rain splashes" },
            new[] { ("Battery Life", "8 hours in earbuds + 16 hours in case (24 hours total)"), ("Quick Charge", "3 min charge = up to 60 min playback"), ("Audio Codecs", "LDAC, LC3, AAC, SBC"), ("Water Resistance", "IPX4"), ("Weight", "5.9 grams per earbud") }
        ),
        CreateProduct(
            Guid.Parse("cccccccc-3333-0000-0000-000000000009"),
            "Bose QuietComfort Ultra Wireless Earbuds", "bose-quietcomfort-ultra-earbuds", 299.00m, "BOSE-QC-ULTRA-EARBUDS", 45,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Premium wireless noise-canceling earbuds featuring Bose Immersive Audio spatialized sound, CustomTune ear personalization, and nine combinations of ear tips and stability bands for all-day comfort.",
            "Bose premium noise-canceling earbuds with spatial Immersive Audio and customized ear stability fit.",
            "https://images.unsplash.com/photo-1606220588913-b3aacb4d2f46?w=800&auto=format&fit=crop&q=80",
            new[] { "Bose Immersive Audio places sound directly in front of you for spatial realism", "CustomTune technology personalizes the noise cancellation and sound performance to your ears", "Nine combinations of ear tips and stability bands provide a secure, comfortable seal", "Clear calls with noise-rejecting microphones that filter out background chatter", "Up to 6 hours listening time (up to 24 hours with wireless charging case)" },
            new[] { ("Battery Life", "Up to 6 hours (up to 24 hours with case)"), ("Fast Charge", "20 min charge = 2 hours playback"), ("Bluetooth Version", "5.3 (aptX Adaptive)"), ("Water Resistance", "IPX4"), ("Touch Controls", "Capacitive touch sensors on both earbuds") }
        ),
        CreateProduct(
            Guid.Parse("cccccccc-3333-0000-0000-000000000010"),
            "Beats Studio Pro Wireless Bluetooth Headphones", "beats-studio-pro-wireless-headphones", 349.99m, "BEATS-STUDIO-PRO", 35,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Beats flagship over-ear headphones with custom 40mm active drivers, fully adaptive Active Noise Cancelling, Transparency mode, Personalized Spatial Audio, and lossless USB-C audio with 3 distinct sound profiles.",
            "Beats flagship headphones with USB-C lossless audio, Personalized Spatial Audio, and 40h battery.",
            "https://images.unsplash.com/photo-1524678606370-a47ad25cb82a?w=800&auto=format&fit=crop&q=80",
            new[] { "Custom 40mm active drivers reduce distortion up to 80% compared to Studio3", "USB-C lossless audio input with 3 built-in EQ profiles (Beats Signature, Entertainment, Conversation)", "Fully adaptive Active Noise Cancelling continuously monitors ambient environment", "Personalized Spatial Audio with dynamic head tracking for immersive 360-degree sound", "Up to 40 hours of battery life (up to 24 hours with ANC enabled)" },
            new[] { ("Battery Life", "Up to 40 hours (ANC off) / 24 hours (ANC on)"), ("Connectivity", "Class 1 Bluetooth, USB-C Lossless, 3.5mm analog"), ("Fast Fuel", "10 min charge = 4 hours playback"), ("Compatibility", "One-touch pairing for Apple & Android"), ("Weight", "260 grams") }
        ),
        CreateProduct(
            Guid.Parse("cccccccc-3333-0000-0000-000000000011"),
            "Marshall Major IV On-Ear Wireless Bluetooth Headphones", "marshall-major-iv-wireless", 149.99m, "MARSHALL-MAJOR-IV", 40,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Iconic rock-and-roll styled on-ear headphones delivering 80+ solid hours of wireless playtime, custom-tuned dynamic drivers, wireless charging, and multidirectional control knob.",
            "Retro rock-and-roll styled headphones with massive 80+ hour battery life and wireless charging.",
            "https://images.unsplash.com/photo-1545454675-3531b543be5d?w=800&auto=format&fit=crop&q=80",
            new[] { "Over 80 hours of wireless playtime on a single charge", "Quick-charge capability: 15 minutes of charging gives 15 hours of listening", "Custom-tuned 40mm dynamic drivers produce roaring bass and smooth mids", "Multi-directional brass control knob handles playback, volume, and calls", "Collapsible design with wireless charging support on earcup" },
            new[] { ("Playtime", "80+ hours"), ("Driver Size", "40mm dynamic"), ("Wireless Charging", "Yes (Qi compatible)"), ("Weight", "165 grams"), ("Frequency Response", "20 Hz – 20 kHz") }
        ),
        CreateProduct(
            Guid.Parse("cccccccc-3333-0000-0000-000000000012"),
            "Shure AONIC 50 Gen 2 Wireless Studio Headphones", "shure-aonic-50-gen-2-studio", 349.00m, "SHURE-AONIC-50-G2", 20,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Engineered from decades of stage and studio experience. Features custom 50mm dynamic drivers, hybrid Active Noise Cancellation, spatial audio technology, high-resolution USB streaming (up to 32-bit/384kHz), and 45-hour battery life.",
            "Studio-grade wireless audiophile headphones with 50mm drivers and 32-bit/384kHz USB-C streaming.",
            "https://images.unsplash.com/photo-1484704849700-f032a568e944?w=800&auto=format&fit=crop&q=80",
            new[] { "Custom 50mm dynamic drivers deliver studio-accurate soundstage with deep bass", "Spatialized Audio algorithm with Cinema, Music, and Podcast acoustic modes", "Hybrid Active Noise Cancellation with adjustable Environment Mode", "High-resolution USB streaming up to 32-bit / 384 kHz via USB-C cable", "Up to 45 hours battery life with quick-charge functionality" },
            new[] { ("Driver Size", "50mm Neodymium"), ("Battery Life", "Up to 45 hours"), ("Codecs", "Qualcomm aptX Adaptive, LDAC, AAC, SBC"), ("Hi-Res Audio", "Up to 32-bit/384kHz via USB"), ("Weight", "340 grams") }
        ),
        CreateProduct(
            Guid.Parse("cccccccc-3333-0000-0000-000000000013"),
            "JBL Tour One M2 Wireless Noise-Canceling Headphones", "jbl-tour-one-m2-headphones", 299.95m, "JBL-TOUR-ONE-M2", 35,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "True Adaptive Noise Cancelling headphones featuring 40mm dynamic drivers tuned by legendary JBL Pro Sound. Includes 4-mic crystal clear call technology, Smart Ambient awareness, and up to 50 hours of playtime.",
            "JBL Pro Sound headphones with True Adaptive Noise Cancelling and 50-hour maximum playback.",
            "https://images.unsplash.com/photo-1585298723682-7115561c51b7?w=800&auto=format&fit=crop&q=80",
            new[] { "True Adaptive Noise Cancelling technology uses 4 noise-sensing mics to adjust in real time", "JBL Spatial Sound makes music and movies feel like you are in a theater", "Smart Talk feature pauses music automatically when you begin speaking", "4-mic crystal call technology for clear telephone and business conference calls", "Up to 50 hours of playback (up to 30 hours with ANC activated)" },
            new[] { ("Battery Life", "Up to 50 hours (ANC Off) / 30 hours (ANC On)"), ("Driver Size", "40mm Dynamic Driver"), ("Bluetooth", "Bluetooth 5.3 LE Audio"), ("Weight", "278 grams"), ("Voice Assistant", "Hey Google & Amazon Alexa built-in") }
        ),
        CreateProduct(
            Guid.Parse("cccccccc-3333-0000-0000-000000000014"),
            "HyperX Cloud Alpha Wireless Gaming Headset (300Hr Battery)", "hyperx-cloud-alpha-wireless-300hr", 199.99m, "HYPERX-CLOUD-ALPHA-WL", 50,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Unbelievable 300-hour battery life on a single charge allows over a month of daily gaming without recharging. Features dual chamber 50mm drivers, DTS Headphone:X Spatial Audio, and signature HyperX memory foam comfort.",
            "World's longest battery gaming headset with 300 hours per charge and DTS Headphone:X spatial audio.",
            "https://images.unsplash.com/photo-1612444530582-fc66183b16f7?w=800&auto=format&fit=crop&q=80",
            new[] { "Massive 300-hour battery life lets you game for weeks on a single charge", "HyperX Dual Chamber Drivers separate bass from mids and highs for cleaner audio", "DTS Headphone:X Spatial Audio delivers precise sound localization in FPS games", "Signature HyperX comfort with plush memory foam and breathable leatherette", "Detachable noise-canceling microphone with LED mute indicator" },
            new[] { ("Battery Life", "Up to 300 hours"), ("Wireless Type", "Lag-free 2.4GHz USB Dongle"), ("Drivers", "50mm Dual Chamber with Neodymium magnets"), ("Audio Format", "DTS Headphone:X Spatial Audio"), ("Weight", "322 grams with mic") }
        ),
        CreateProduct(
            Guid.Parse("cccccccc-3333-0000-0000-000000000015"),
            "Anker Soundcore Space Q45 Wireless ANC Headphones", "anker-soundcore-space-q45-anc", 149.99m, "ANKER-SPACE-Q45", 60,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "High-value noise cancelling headphones that reduce noise by up to 98%. Features 40mm silk and ceramic drivers, LDAC Hi-Res Wireless sound, and up to 50 hours of playtime with ANC enabled (65 hours with ANC off).",
            "Best budget ANC headphones under $150 with 98% noise reduction and 50-hour ANC playtime.",
            "https://images.unsplash.com/photo-1578319439584-104c94d37305?w=800&auto=format&fit=crop&q=80",
            new[] { "Adaptive active noise cancelling automatically selects suitable level for your space", "Pioneering double-layer diaphragm drivers produce clear sound with intense bass", "Up to 50 hours of playtime in ANC mode and 65 hours in standard mode", "5-minute ultra-fast charge yields 4 hours of music listening", "Multipoint connection pairs to two devices at the same time" },
            new[] { ("Playtime", "50 hours (ANC on) / 65 hours (ANC off)"), ("Noise Reduction", "Up to 98% ambient isolation"), ("Codecs", "LDAC, AAC, SBC"), ("Fast Charging", "5 min = 4 hours"), ("Drivers", "40mm Silk & Ceramic diaphragms") }
        ),
        CreateProduct(
            Guid.Parse("dddddddd-4444-0000-0000-000000000001"),
            "Apple Watch Series 9 GPS 45mm (Midnight Aluminum)", "apple-watch-series-9-gps-45mm", 429.00m, "APPLE-WATCH-S9-45", 45,
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "Powered by the S9 SiP chip with revolutionary double-tap gesture control, brighter 2000-nit Always-On Retina display, on-device Siri with health data access, Precision Finding for iPhone, and comprehensive ECG and blood oxygen monitoring.",
            "Apple Watch Series 9 featuring S9 chip, innovative Double Tap gesture, and 2000 nits Retina display.",
            "https://images.unsplash.com/photo-1508685096489-7aacd43bd3b1?w=800&auto=format&fit=crop&q=80",
            new[] { "Innovative Double Tap gesture allows one-handed control without touching the screen", "Apple S9 SiP dual-core processor with 4-core Neural Engine", "2000-nit edge-to-edge Always-On Retina OLED display", "Advanced health sensors: ECG, Blood Oxygen, Temperature sensing, Heart Rate", "Crash Detection and Fall Detection with automatic Emergency SOS calling" },
            new[] { ("Case Size", "45mm (Aluminum Midnight)"), ("Display", "Always-On Retina LTPO OLED (2000 nits)"), ("Processor", "Apple S9 SiP (64-bit dual-core)"), ("Battery Life", "Up to 18 hours (up to 36 hours in Low Power Mode)"), ("Water Resistance", "50 meters (Swimproof)"), ("Sensors", "ECG, SpO2, Optical Heart, Temp, Compass, Altimeter") }
        ),
        CreateProduct(
            Guid.Parse("dddddddd-4444-0000-0000-000000000002"),
            "Apple Watch Ultra 2 GPS + Cellular 49mm (Titanium)", "apple-watch-ultra-2-cellular-49mm", 799.00m, "APPLE-WATCH-ULTRA-2", 25,
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "The ultimate rugged sports and expedition watch. Crafted with aerospace-grade titanium, a blindingly bright 3000-nit flat sapphire crystal display, dual-frequency precision GPS, 86-decibel emergency siren, and EN13319 dive computer certification.",
            "Rugged titanium expedition smartwatch with 3000-nit screen, dual-frequency GPS, and 36-hour battery.",
            "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&auto=format&fit=crop&q=80",
            new[] { "49mm corrosion-resistant aerospace titanium case with raised bezel edge protection", "Blindingly bright 3000-nit display is readable in direct blistering sunlight", "Precision dual-frequency GPS (L1 and L5) calculates accurate pace and distance", "Up to 36 hours of battery life with normal use (up to 72 hours in Low Power Mode)", "Certified dive computer capability up to 40 meters with Oceanic+ app" },
            new[] { ("Case Size", "49mm Aerospace Titanium"), ("Display Brightness", "3000 nits peak"), ("Battery Life", "Up to 36 hours (72 hours Low Power)"), ("Water Resistance", "100 meters (Dive certified EN13319 to 40m)"), ("Weight", "61.4 grams (Case only)") }
        ),
        CreateProduct(
            Guid.Parse("dddddddd-4444-0000-0000-000000000003"),
            "Samsung Galaxy Watch 6 Classic 47mm (Rotating Bezel)", "samsung-galaxy-watch-6-classic-47mm", 399.99m, "SAMSUNG-WATCH-6-CLASSIC", 35,
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "Timeless smartwatch design boasting the iconic physical rotating bezel, larger Super AMOLED display with sapphire crystal, BioActive 3-in-1 sensor (ECG, BIA body composition, optical heart rate), and comprehensive sleep coaching.",
            "Classic smartwatch with mechanical rotating bezel, body composition BIA analysis, and sapphire crystal.",
            "https://images.unsplash.com/photo-1509042239860-f550ce710b93?w=800&auto=format&fit=crop&q=80",
            new[] { "Physical rotating bezel allows effortless navigation through apps and widgets", "BioActive sensor reads Body Composition (BIA): Body fat percentage, skeletal muscle, water", "Advanced Sleep Coaching with personalized animal sleep symbols and snoring detection", "Sapphire crystal glass protects against scratches and impacts", "Powered by Wear OS Powered by Samsung with Google Play Store apps" },
            new[] { ("Size", "47mm Stainless Steel"), ("Display", "1.5-inch Super AMOLED (480x480, Sapphire Crystal)"), ("Processor", "Exynos W930 dual-core 1.4GHz"), ("Battery", "425 mAh (WPC wireless charging)"), ("Durability", "5ATM + IP68 / MIL-STD-810H") }
        ),
        CreateProduct(
            Guid.Parse("dddddddd-4444-0000-0000-000000000004"),
            "Garmin Fenix 7 Pro Sapphire Solar GPS Watch", "garmin-fenix-7-pro-sapphire-solar", 799.99m, "GARMIN-FENIX-7PRO-SOLAR", 20,
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "Ultimate multisport GPS smartwatch with solar charging lens that yields up to 22 days of battery life. Includes built-in LED flashlight, multi-band GPS with SatIQ technology, TopoActive maps, and endurance score tracking.",
            "Multisport solar GPS outdoor watch with built-in LED flashlight, TopoActive maps, and 22-day battery.",
            "https://images.unsplash.com/photo-1524805444758-089113d48a6d?w=800&auto=format&fit=crop&q=80",
            new[] { "Power Sapphire solar charging lens harvests sunlight to extend battery up to 22 days", "Built-in multi-LED flashlight with variable intensities and red safety strobe", "Multi-band GNSS with SatIQ technology secures pinpoint GPS tracking", "Preloaded TopoActive maps with Up Ahead navigational course checkpoints", "Endurance Score and Hill Score metrics quantify training progress" },
            new[] { ("Battery Life", "Up to 22 days in smartwatch mode with solar"), ("Display", "1.3-inch sunlight-visible memory-in-pixel (MIP)"), ("Case Material", "Fiber-reinforced polymer with titanium bezel"), ("Water Rating", "10 ATM (100 meters)"), ("Weight", "73 grams") }
        ),
        CreateProduct(
            Guid.Parse("dddddddd-4444-0000-0000-000000000005"),
            "Google Pixel Watch 2 (Fitbit Health Integration)", "google-pixel-watch-2-fitbit", 349.99m, "GOOGLE-PIXEL-WATCH-2", 30,
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "Sleek domed glass smartwatch seamlessly integrating Fitbit health metrics with Google intelligence. Features a multi-path heart rate sensor, cEDA continuous stress sensor, skin temperature tracking, and 24 hours of battery life with Always-On display.",
            "Elegantly domed smartwatch with multi-path Fitbit heart rate tracking and Google Assistant AI.",
            "https://images.unsplash.com/photo-1510017803434-a899398421b3?w=800&auto=format&fit=crop&q=80",
            new[] { "Multi-path heart rate sensor delivers up to 40% more accurate heart rate tracking during intense exercise", "Continuous electrodermal activity (cEDA) sensor identifies potential signs of stress", "Skin temperature tracking spots changes related to sleep environment and wellness", "Seamless Google ecosystem: Gmail, Google Calendar, Google Wallet, Google Maps", "Fast charging: 30 minutes of charging delivers 12 hours of battery life" },
            new[] { ("Case Size", "41mm 100% recycled aluminum"), ("Display", "320 ppi AMOLED with DCI-P3 color (1000 nits)"), ("Processor", "Qualcomm 5100 with Cortex M33 co-processor"), ("Battery", "306 mAh (24 hours with Always-On display)"), ("Water Resistance", "5 ATM (50 meters)") }
        ),
        CreateProduct(
            Guid.Parse("dddddddd-4444-0000-0000-000000000006"),
            "Garmin Forerunner 265 Running Smartwatch (AMOLED)", "garmin-forerunner-265-amoled", 449.99m, "GARMIN-FR-265-AMOLED", 25,
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "Dedicated running and triathlon smartwatch featuring a colorful, high-contrast AMOLED touchscreen, Training Readiness score, wrist-based running dynamics, morning report, and up to 13 days of battery life.",
            "Vibrant AMOLED running smartwatch with Training Readiness, HRV status, and race widget.",
            "https://images.unsplash.com/photo-1517502474097-f9b30659dadb?w=800&auto=format&fit=crop&q=80",
            new[] { "Brilliant 1.3-inch AMOLED touchscreen with traditional 5-button running controls", "Training Readiness score tells you whether today is a day to go hard or take it easy", "Wrist-based running dynamics: Cadence, stride length, ground contact time without a pod", "Morning Report summarizes sleep, recovery outlook, HRV status, and daily workout suggestions", "Multi-band GNSS with SatIQ offers superior positional accuracy" },
            new[] { ("Battery Life", "Up to 13 days in smartwatch mode / 20 hours GPS"), ("Display", "1.3-inch AMOLED (416x416)"), ("Lens Material", "Corning Gorilla Glass 3"), ("Water Rating", "5 ATM"), ("Weight", "47 grams") }
        ),
        CreateProduct(
            Guid.Parse("dddddddd-4444-0000-0000-000000000007"),
            "Apple Watch SE (2nd Gen) GPS 40mm (Starlight)", "apple-watch-se-2nd-gen-40mm", 249.00m, "APPLE-WATCH-SE2-40", 50,
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "All the essential fitness, connectivity, and safety features at an accessible entry price. Powered by the same S8 SiP chip as Series 8, featuring Crash Detection, Retina display, and water resistance up to 50 meters.",
            "Best value Apple Watch under $250 with Crash Detection, Heart Rate alerts, and S8 chip.",
            "https://images.unsplash.com/photo-1579586337278-3befd40fd17a?w=800&auto=format&fit=crop&q=80",
            new[] { "High-value Apple Watch experience under $250", "Apple S8 SiP dual-core processor delivers speedy performance", "Crash Detection, Fall Detection, and Emergency SOS for peace of mind", "Track daily activity rings and deep workout metrics in the Apple Fitness app", "Swimproof design water resistant to 50 meters" },
            new[] { ("Case Size", "40mm Starlight Aluminum"), ("Display", "Retina OLED display (up to 1000 nits)"), ("Processor", "Apple S8 SiP"), ("Battery Life", "Up to 18 hours"), ("Water Resistance", "50 meters") }
        ),
        CreateProduct(
            Guid.Parse("dddddddd-4444-0000-0000-000000000008"),
            "Fitbit Charge 6 Advanced Fitness Tracker with ECG", "fitbit-charge-6-fitness-tracker", 159.95m, "FITBIT-CHARGE-6", 45,
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "Google-powered fitness band with Fitbit's most accurate heart rate tracking yet. Connects with gym equipment via Bluetooth, offers YouTube Music controls, Google Maps directions on wrist, built-in GPS, and 7-day battery life.",
            "Advanced slim fitness band with 60% more accurate heart rate, built-in GPS, and 7-day battery.",
            "https://images.unsplash.com/photo-1575311373937-040b8e1fd5b6?w=800&auto=format&fit=crop&q=80",
            new[] { "Fitbit's most accurate heart rate tracking on a tracker with machine learning algorithms", "Built-in Google Maps navigation prompts and Google Wallet tap-to-pay", "ECG app for assessing heart rhythm for atrial fibrillation (AFib)", "EDA Scan sensor detects electrodermal response to track physical stress", "Up to 7 days of continuous battery life without needing a charge" },
            new[] { ("Battery Life", "Up to 7 days"), ("Sensors", "Optical Heart Rate, SpO2, ECG, EDA, Skin Temp"), ("GPS", "Built-in GPS + GLONASS"), ("Water Resistance", "50 meters"), ("Weight", "37 grams with band") }
        ),
        CreateProduct(
            Guid.Parse("dddddddd-4444-0000-0000-000000000009"),
            "Whoop 4.0 Health & Performance Screenless Tracker", "whoop-4-health-performance-band", 239.00m, "WHOOP-4-BAND", 30,
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "Screen-free 24/7 biometric health sensor designed for high performers and athletes. Calculates precise daily Strain scores, Sleep Performance, Recovery percentages, and Skin Temperature without distracting screen notifications.",
            "Screenless 24/7 fitness wearable measuring Strain, Sleep, Recovery, and HRV for athletes.",
            "https://images.unsplash.com/photo-1510017198469-a4294273c4c3?w=800&auto=format&fit=crop&q=80",
            new[] { "Screenless distraction-free design focuses 100% on physiological data collection", "Calculates daily Recovery Score based on HRV, resting heart rate, and sleep quality", "Strain Coach provides real-time exertion targets during workouts", "Wireless battery pack slides on top of band to charge while being worn", "Any-Wear technology allows sensor to be worn in wristbands, bicep bands, or garments" },
            new[] { ("Battery Life", "4-5 days (On-the-go wireless battery pack)"), ("Sensors", "5 LEDs, 4 photodiodes, Body Temp, Pulse Oximeter"), ("Water Rating", "IP68 (Up to 10m for 2 hours)"), ("Connectivity", "Bluetooth Low Energy"), ("Weight", "27 grams") }
        ),
        CreateProduct(
            Guid.Parse("dddddddd-4444-0000-0000-000000000010"),
            "Amazfit GTR 4 Smartwatch with Dual-Band GPS", "amazfit-gtr-4-dual-band-gps", 199.99m, "AMAZFIT-GTR-4", 40,
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "Long-lasting smartwatch featuring a 1.43\" HD AMOLED screen, industry-first circularly-polarized dual-band GPS antenna, 14-day battery life, 150+ sports modes, and Bluetooth phone calls.",
            "Value smartwatch with 14-day battery, circular-polarized dual-band GPS, and 1.43\" AMOLED.",
            "https://images.unsplash.com/photo-1544117519-31a4b719223d?w=800&auto=format&fit=crop&q=80",
            new[] { "Incredible 14-day typical battery life and up to 24 days in battery saver mode", "Dual-band circularly-polarized GPS antenna ensures 99% accuracy identical to top handheld GPS units", "Large 1.43-inch HD AMOLED display with 200+ watch faces and matching always-on displays", "BioTracker 4.0 PPG biometric sensor tracks 24/7 heart rate, blood-oxygen, and stress", "Bluetooth phone calls and music storage directly on watch" },
            new[] { ("Battery Life", "Up to 14 days typical (24 days battery saver)"), ("Display", "1.43-inch AMOLED (466x466)"), ("Positioning", "Dual-band & 6 satellite positioning systems"), ("Water Resistance", "5 ATM (50 meters)"), ("Body Material", "Aluminum alloy middle frame") }
        ),
        CreateProduct(
            Guid.Parse("dddddddd-4444-0000-0000-000000000011"),
            "Suunto Race GPS Multisport Outdoor Watch", "suunto-race-gps-multisport-watch", 449.00m, "SUUNTO-RACE-GPS", 20,
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "Designed for competitive athletes and trail runners. Boasts a crystal-clear 1.43\" 1000-nit AMOLED display, digital crown for fast browsing, free offline outdoor maps, HRV recovery measurements, and up to 26 days of battery life.",
            "Endurance sports watch with 1.43\" AMOLED, digital crown, free offline maps, and titanium/steel bezel.",
            "https://images.unsplash.com/photo-1522335789203-aabd1fc54bc9?w=800&auto=format&fit=crop&q=80",
            new[] { "High-definition 1.43-inch AMOLED display with rotating digital crown for quick map zoom", "Free global offline topographic maps downloaded directly to watch memory", "Heart Rate Variability (HRV) tracking calculates recovery status", "Up to 40 hours of continuous training with most accurate dual-band GNSS", "Stainless steel or titanium bezel with sapphire crystal glass" },
            new[] { ("Battery Life", "Up to 26 days (smartwatch) / 40 hours (All-systems GNSS)"), ("Display", "1.43-inch AMOLED (466x466, 1000 nits)"), ("Glass Material", "Sapphire crystal"), ("Water Resistance", "100 meters (10 ATM)"), ("Weight", "83 grams (Steel)") }
        ),
        CreateProduct(
            Guid.Parse("dddddddd-4444-0000-0000-000000000012"),
            "Withings ScanWatch 2 Hybrid Smartwatch with Medical ECG", "withings-scanwatch-2-hybrid-ecg", 349.95m, "WITHINGS-SCANWATCH-2", 25,
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "A timeless analog timepiece packed with state-of-the-art medical technology. Features a surgical-grade stainless steel case, sapphire glass, miniaturized PMOLED sub-dial, 30-day battery life, FDA-cleared ECG, and continuous body temperature variation tracking.",
            "Classic analog hybrid watch with 30-day battery, medical-grade ECG, and continuous body temperature.",
            "https://images.unsplash.com/photo-1533139502658-0198f920d8e8?w=800&auto=format&fit=crop&q=80",
            new[] { "Classic luxury analog design with mechanical clock hands and discrete OLED display", "30-day battery life eliminates nightly charging hassles", "Medical-grade 1-lead electrocardiogram (ECG) detects atrial fibrillation in 30 seconds", "TempTech24/7 module tracks baseline day and night body temperature fluctuations", "Water resistant to 50 meters with sapphire glass and stainless steel casing" },
            new[] { ("Battery Life", "Up to 30 days"), ("Case Size", "42mm Stainless Steel"), ("Lens", "Anti-reflective Sapphire glass"), ("Sensors", "Medical ECG, TempTech 24/7, PPG Heart Rate, SpO2"), ("Water Resistance", "5 ATM (50 meters)") }
        ),
        CreateProduct(
            Guid.Parse("eeeeeeee-5555-0000-0000-000000000001"),
            "Apple iPad Pro 13\" (M4 OLED Display, 256GB)", "apple-ipad-pro-13-m4-oled", 1299.00m, "APPLE-IPAD-PRO-13-M4", 30,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Impossibly thin (5.1mm) iPad Pro equipped with revolutionary Ultra Retina XDR tandem OLED display technology. Powered by the next-gen Apple M4 chip with 38 TOPS Neural Engine, supporting Apple Pencil Pro and Magic Keyboard.",
            "Ultra-thin 5.1mm iPad Pro with M4 chip, Tandem OLED Ultra Retina XDR display, and Apple Pencil Pro support.",
            "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=800&auto=format&fit=crop&q=80",
            new[] { "Breakthrough Tandem OLED Ultra Retina XDR display delivers 1000 nits full-screen brightness and 1600 nits peak HDR", "Next-generation Apple M4 chip with hardware ray tracing and 38 TOPS Neural Engine", "Incredibly thin 5.1mm aluminum enclosure - the thinnest Apple product ever made", "Landscape 12MP Ultra-wide front camera with Center Stage for natural video calling", "Supports new Apple Pencil Pro with barrel roll and haptic squeeze gestures" },
            new[] { ("Processor", "Apple M4 (9-core CPU, 10-core GPU)"), ("Display", "13-inch Ultra Retina XDR Tandem OLED (2752x2064, 120Hz)"), ("Storage", "256GB SSD"), ("Thickness", "5.1 mm"), ("Weight", "1.28 lbs (579 grams)"), ("Operating System", "iPadOS 18") }
        ),
        CreateProduct(
            Guid.Parse("eeeeeeee-5555-0000-0000-000000000002"),
            "Apple iPad Air 11\" (M2 Chip, Space Gray)", "apple-ipad-air-11-m2-space-gray", 599.00m, "APPLE-IPAD-AIR-11-M2", 45,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Versatile powerhouse tablet driven by Apple's fast M2 chip. Features an 11-inch Liquid Retina display with P3 wide color, landscape front camera with Center Stage, Touch ID on top button, and Apple Pencil Pro compatibility.",
            "Capable and portable iPad Air with Apple M2 chip, 11\" Liquid Retina screen, and Apple Pencil Pro support.",
            "https://images.unsplash.com/photo-1561154464-82e9adf32764?w=800&auto=format&fit=crop&q=80",
            new[] { "Apple M2 chip delivers 50% faster performance than previous generation", "11-inch Liquid Retina display with True Tone, P3 wide color, and anti-reflective coating", "Landscape 12MP Ultra Wide front camera with Center Stage for video calls", "Touch ID integrated into top power button for fast, secure authentication", "Compatible with Apple Pencil Pro and Magic Keyboard" },
            new[] { ("Processor", "Apple M2 (8-core CPU, 10-core GPU)"), ("Display", "11-inch Liquid Retina (2360x1640)"), ("Storage", "128GB"), ("Weight", "1.02 lbs (462 grams)"), ("Connectivity", "Wi-Fi 6E, USB-C 3.1 Gen 2"), ("Operating System", "iPadOS 18") }
        ),
        CreateProduct(
            Guid.Parse("eeeeeeee-5555-0000-0000-000000000003"),
            "Apple iPad mini (6th Generation, Purple)", "apple-ipad-mini-6th-gen-purple", 499.00m, "APPLE-IPAD-MINI-6", 35,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Mega power in mini size. The 8.3-inch Liquid Retina display fits comfortably in one hand. Powered by the A15 Bionic chip with Neural Engine, USB-C, Apple Pencil 2 magnetic attachment, and 12MP cameras.",
            "Pocket-sized 8.3\" iPad mini with A15 Bionic chip, edge-to-edge Liquid Retina screen, and USB-C.",
            "https://images.unsplash.com/photo-1585790050230-5dd28404ccb9?w=800&auto=format&fit=crop&q=80",
            new[] { "Compact 8.3-inch Liquid Retina display with True Tone and wide color gamut", "Apple A15 Bionic chip provides console-level gaming and photo editing power", "Magnetic side connector charges and holds Apple Pencil (2nd Gen)", "USB-C port allows connection to cameras, external drives, and monitors", "Ultra-lightweight 293g body slips easily into coat pockets or small bags" },
            new[] { ("Processor", "Apple A15 Bionic"), ("Display", "8.3-inch Liquid Retina (2266x1488)"), ("Weight", "0.65 lb (293 grams)"), ("Camera", "12MP Wide back + 12MP Ultra Wide front with Center Stage"), ("Storage", "64GB") }
        ),
        CreateProduct(
            Guid.Parse("eeeeeeee-5555-0000-0000-000000000004"),
            "Samsung Galaxy Tab S9 Ultra 14.6\" Dynamic AMOLED 2X", "samsung-galaxy-tab-s9-ultra", 1199.99m, "SAMSUNG-TAB-S9-ULTRA", 20,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Colossal 14.6\" tablet providing laptop-replacement screen real estate. Dynamic AMOLED 2X 120Hz display with Vision Booster, IP68 water/dust resistance for both tablet and included S Pen, and Snapdragon 8 Gen 2 for Galaxy.",
            "Giant 14.6\" AMOLED tablet with IP68 water resistance, included S Pen, and Samsung DeX desktop mode.",
            "https://images.unsplash.com/photo-1542751110-97427bbecf20?w=800&auto=format&fit=crop&q=80",
            new[] { "Massive 14.6-inch Dynamic AMOLED 2X 120Hz screen with HDR10+ and deep blacks", "First Galaxy Tab S series with IP68 water and dust resistance", "Included IP68 water-resistant S Pen with bi-directional magnetic charging", "Samsung DeX creates a desktop PC-like workspace with resizable floating windows", "Quad AKG speakers with Dolby Atmos surround sound" },
            new[] { ("Display", "14.6\" Dynamic AMOLED 2X (2960x1848, 120Hz)"), ("Processor", "Snapdragon 8 Gen 2 for Galaxy"), ("Memory / Storage", "12GB RAM / 256GB (MicroSD up to 1TB)"), ("Battery", "11,200 mAh (45W fast charging)"), ("Weight", "732 grams") }
        ),
        CreateProduct(
            Guid.Parse("eeeeeeee-5555-0000-0000-000000000005"),
            "Samsung Galaxy Tab S9 FE 10.9\" Tablet with S Pen", "samsung-galaxy-tab-s9-fe", 449.99m, "SAMSUNG-TAB-S9-FE", 40,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Affordable productivity tablet featuring a 10.9\" 90Hz display, IP68 water/dust resistance, included S Pen for note taking and sketching, and a long-lasting 8,000 mAh battery with fast charging.",
            "Sub-$450 water-resistant tablet with included S Pen, 90Hz screen, and long-lasting 8,000 mAh battery.",
            "https://images.unsplash.com/photo-1589739900243-4b52cd9b104e?w=800&auto=format&fit=crop&q=80",
            new[] { "Great value under $450 with included magnetic S Pen", "IP68 water and dust resistant rating withstands spills and outdoor usage", "10.9-inch 90Hz smooth display with Vision Booster for outdoor sunlight readability", "Dual speakers sound tuned by AKG with Dolby Atmos", "Up to 18 hours of video playback with 8,000 mAh battery" },
            new[] { ("Display", "10.9\" LCD 90Hz (2304x1440)"), ("Processor", "Exynos 1380 Octa-core"), ("Storage", "128GB (MicroSD expandable up to 1TB)"), ("Battery", "8,000 mAh"), ("Water Resistance", "IP68") }
        ),
        CreateProduct(
            Guid.Parse("eeeeeeee-5555-0000-0000-000000000006"),
            "Amazon Kindle Paperwhite (11th Gen, 6.8\" Glare-free Display)", "amazon-kindle-paperwhite-11th-gen", 149.99m, "AMZN-KINDLE-PW11", 70,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "The gold standard of digital reading. Features a 6.8\" 300 ppi glare-free display that reads like real paper even in bright sunlight, adjustable warm light, IPX8 waterproofing for bathtub reading, and up to 10 weeks of battery life.",
            "Best-selling 6.8\" 300 ppi e-reader with warm light, IPX8 waterproof rating, and 10-week battery.",
            "https://images.unsplash.com/photo-1592496431122-2349e0fbc666?w=800&auto=format&fit=crop&q=80",
            new[] { "6.8-inch 300 ppi glare-free display renders laser-sharp typography", "Adjustable warm light shifts screen shade from white to amber for night reading", "IPX8 waterproof rating withstands accidental immersion in up to 2 meters of fresh water", "Up to 10 weeks of battery life on a single USB-C charge", "Instant access to millions of books, magazines, and audiobooks on Kindle Store" },
            new[] { ("Display", "6.8\" Paperwhite 300 ppi glare-free E-Ink"), ("Battery Life", "Up to 10 weeks"), ("Waterproofing", "IPX8 (2m fresh water for 60 min)"), ("Storage", "16GB (Holds thousands of books)"), ("Weight", "205 grams") }
        ),
        CreateProduct(
            Guid.Parse("eeeeeeee-5555-0000-0000-000000000007"),
            "Amazon Kindle Scribe (10.2\" Paper Display with Basic Pen)", "amazon-kindle-scribe-10-inch", 339.99m, "AMZN-KINDLE-SCRIBE", 25,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "The first Kindle for reading and writing. Features a giant 10.2\" 300 ppi glare-free Front-lit Paperwhite display, included battery-free pen for handwritten sticky notes in books, journaling, and PDF annotation.",
            "10.2\" digital notebook and e-reader with included stylus pen for sketching and handwritten notes.",
            "https://images.unsplash.com/photo-1588702547923-7093a6c3ba33?w=800&auto=format&fit=crop&q=80",
            new[] { "World's first 10.2-inch 300 ppi glare-free front-lit E-Ink display", "Read and write as naturally as you do on paper with included battery-free stylus pen", "Take handwritten notes directly inside millions of Kindle books", "Create notebooks, journals, and to-do lists with customizable templates (ruled, grid, checklist)", "Months of reading battery life and weeks of daily writing on one charge" },
            new[] { ("Display", "10.2\" 300 ppi glare-free Paperwhite"), ("Pen", "Included Basic Pen (No charging required)"), ("Storage", "16GB"), ("Front Light", "35 LEDs with auto-adjusting warm light"), ("Weight", "433 grams") }
        ),
        CreateProduct(
            Guid.Parse("eeeeeeee-5555-0000-0000-000000000008"),
            "Kobo Clara 2E Eco-conscious E-Reader", "kobo-clara-2e-ereader", 139.99m, "KOBO-CLARA-2E", 35,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Eco-conscious e-reader crafted with over 85% recycled plastic. Features a 6\" HD E Ink Carta 1200 glare-free touchscreen, ComfortLight PRO blue light reduction, IPX8 waterproofing, and Bluetooth for audiobooks.",
            "Eco-friendly waterproof e-reader with 6\" HD Carta 1200 screen, ComfortLight PRO, and audiobook support.",
            "https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=800&auto=format&fit=crop&q=80",
            new[] { "Exterior shell made with over 85% recycled and ocean-bound plastic", "6-inch HD E Ink Carta 1200 glare-free screen with faster page turns", "ComfortLight PRO adjusts brightness and color temperature to reduce blue light", "Full IPX8 waterproof rating for worry-free reading by the pool or in the bath", "Supports OverDrive library borrowing directly from the device" },
            new[] { ("Display", "6\" HD E Ink Carta 1200 (300 ppi)"), ("Storage", "16GB (Up to 12,000 eBooks)"), ("Waterproofing", "IPX8 (Up to 60 min in 2m water)"), ("Connectivity", "Wi-Fi, Bluetooth for Kobo Audiobooks, USB-C"), ("Weight", "171 grams") }
        ),
        CreateProduct(
            Guid.Parse("eeeeeeee-5555-0000-0000-000000000009"),
            "reMarkable 2 Digital Paper Writing & Sketching Tablet", "remarkable-2-digital-paper-tablet", 399.00m, "REMARKABLE-2-TABLET", 25,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "The world's thinnest digital paper tablet (4.7mm). Replaces notebooks and printed documents with a paper-like writing feel, paper texture CANVAS display, zero distractions (no notifications or social media), and 2-week battery life.",
            "Distraction-free 4.7mm paper tablet with authentic pen-on-paper friction and weeks of battery.",
            "https://images.unsplash.com/photo-1517842645767-c639042777db?w=800&auto=format&fit=crop&q=80",
            new[] { "CANVAS display mimics the tactile surface friction and response of real paper", "Ultra-thin 4.7mm aluminum body - slips easily into notebook folios", "Distraction-free: No notifications, social media, pop-ups, or email distractions", "Convert handwritten notes into clean typed text with a single tap", "Organize notebooks, folders, and annotated PDFs seamlessly with cloud sync" },
            new[] { ("Display", "10.3\" monochrome digital paper CANVAS (226 DPI)"), ("Thickness", "4.7 mm"), ("Weight", "403 grams"), ("Battery", "Up to 2 weeks of typical daily writing"), ("Storage", "8GB internal memory") }
        ),
        CreateProduct(
            Guid.Parse("eeeeeeee-5555-0000-0000-000000000010"),
            "Microsoft Surface Pro 10 Copilot+ 2-in-1 Tablet PC", "microsoft-surface-pro-10-copilot-plus", 1199.99m, "MS-SURFACE-PRO10", 25,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "Next-generation 2-in-1 AI PC tablet powered by Snapdragon X Plus processor with 45 TOPS NPU. Boasts a 13-inch PixelSense Flow 120Hz touchscreen, kickstand with 165-degree friction hinge, dual USB4 ports, and all-day battery life.",
            "Flagship 2-in-1 Copilot+ tablet PC with Snapdragon X Plus, 120Hz PixelSense screen, and kickstand.",
            "https://images.unsplash.com/photo-1527689368864-3a821dbccc34?w=800&auto=format&fit=crop&q=80",
            new[] { "Snapdragon X Plus 10-core processor with 45 TOPS NPU for on-device Copilot+ AI features", "13-inch PixelSense Flow touchscreen with 120Hz dynamic refresh rate and HDR", "Iconic built-in multi-position kickstand opens up to 165 degrees for drawing posture", "Dual USB-C / USB4 ports support multiple 4K external displays and high-speed data", "Ultra-lightweight tablet form factor weighing just 1.97 lbs" },
            new[] { ("Processor", "Snapdragon X Plus (10-Core)"), ("Memory", "16GB LPDDR5X"), ("Storage", "256GB Removable Gen4 SSD"), ("Display", "13\" PixelSense Flow (2880x1920, 120Hz)"), ("Weight", "1.97 lbs (895 grams)"), ("Operating System", "Windows 11 Home (Copilot+ PC)") }
        ),
        CreateProduct(
            Guid.Parse("ffffffff-6666-0000-0000-000000000001"),
            "Sony PlayStation 5 Slim Console (1TB SSD, 4K Gaming)", "sony-playstation-5-slim-console", 499.99m, "SONY-PS5-SLIM-1TB", 40,
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            "The redesigned slim PlayStation 5 packs powerful next-generation gaming into a 30% smaller volume. Features an ultra-high speed 1TB SSD, ray tracing, 4K gaming at up to 120 FPS, Tempest 3D AudioTech, and haptic feedback via DualSense controller.",
            "Redesigned slim PS5 console with 1TB SSD, 4K 120Hz gaming, and DualSense haptic feedback.",
            "https://images.unsplash.com/photo-1606813907291-d86efa9b94db?w=800&auto=format&fit=crop&q=80",
            new[] { "30% smaller volume and lighter weight compared to the launch PS5 model", "Custom 1TB Ultra-High Speed SSD eliminates load screens almost entirely", "Hardware-accelerated Ray Tracing for lifelike shadows and reflections", "Supports 4K 120Hz TVs, Variable Refresh Rate (VRR), and HDR gaming", "Included DualSense wireless controller with immersive haptic feedback and adaptive triggers" },
            new[] { ("Storage", "1TB Custom NVMe SSD"), ("Processor", "Custom 8-core AMD Zen 2 (up to 3.5 GHz)"), ("Graphics", "Custom AMD RDNA 2 (10.3 TFLOPS)"), ("Video Output", "HDMI 2.1 (Up to 4K 120Hz / 8K output)"), ("Audio", "Tempest 3D AudioTech") }
        ),
        CreateProduct(
            Guid.Parse("ffffffff-6666-0000-0000-000000000002"),
            "Microsoft Xbox Series X 1TB Console (4K 120FPS)", "microsoft-xbox-series-x-console", 499.99m, "MS-XBOX-SERIES-X", 35,
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            "The fastest, most powerful Xbox ever made. Delivers 12 teraflops of graphic processing power, true 4K gaming at up to 120 frames per second, Quick Resume for multiple titles simultaneously, and backward compatibility across 4 generations of Xbox games.",
            "Flagship 12 TFLOPS 4K 120FPS gaming console with Quick Resume and Xbox Game Pass integration.",
            "https://images.unsplash.com/photo-1621259182978-fbf93132d53d?w=800&auto=format&fit=crop&q=80",
            new[] { "12 Teraflops of raw graphical computing power handles true 4K resolution at up to 120 FPS", "Quick Resume seamlessly switches between multiple games in a flash without reloading", "Xbox Velocity Architecture combines custom 1TB NVMe SSD and integrated software", "Backward compatibility plays thousands of games across Xbox One, Xbox 360, and Original Xbox", "Dolby Vision and Dolby Atmos gaming audio support" },
            new[] { ("Processor", "Custom 8-Core AMD Zen 2 (3.8 GHz)"), ("Graphics", "12 TFLOPS, 52 CUs @ 1.825 GHz Custom RDNA 2"), ("Memory", "16GB GDDR6"), ("Storage", "1TB Custom NVMe SSD"), ("Resolution", "True 4K Gaming up to 120 FPS") }
        ),
        CreateProduct(
            Guid.Parse("ffffffff-6666-0000-0000-000000000003"),
            "Nintendo Switch – OLED Model (White Joy-Con)", "nintendo-switch-oled-white", 349.99m, "NTDO-SWITCH-OLED-WHT", 50,
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            "Features a vibrant 7-inch OLED screen with vivid colors and deep contrast. Includes a wide adjustable tabletop stand, dock with wired LAN port, 64GB internal storage, and enhanced audio in handheld and tabletop modes.",
            "Nintendo Switch with vibrant 7-inch OLED display, wide adjustable stand, and LAN dock.",
            "https://images.unsplash.com/photo-1578303512597-81e6cc155b3e?w=800&auto=format&fit=crop&q=80",
            new[] { "7-inch OLED screen provides vivid color reproduction and deep infinite blacks", "Wide adjustable tabletop kickstand allows comfortable viewing angles anywhere", "Dock includes a built-in wired RJ-45 LAN port for stable competitive online gaming", "64GB of internal storage with microSD card expansion slot", "Play in 3 modes: TV Mode, Tabletop Mode, and Handheld Mode" },
            new[] { ("Display", "7.0-inch OLED (1280x720 multi-touch)"), ("Video Output", "Up to 1080p via HDMI in TV mode"), ("Storage", "64GB (MicroSD up to 2TB)"), ("Battery Life", "Approx. 4.5 to 9 hours"), ("Weight", "0.93 lbs (420 grams with Joy-Cons)") }
        ),
        CreateProduct(
            Guid.Parse("ffffffff-6666-0000-0000-000000000004"),
            "Valve Steam Deck OLED 512GB Handheld Gaming PC", "valve-steam-deck-oled-512gb", 549.00m, "VALVE-STEAMDECK-OLED", 25,
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            "The definitive PC gaming handheld upgraded with a stunning 7.4\" 90Hz HDR OLED display, faster 6nm APU, Wi-Fi 6E, improved thumbsticks and trackpads, and a 50Wh battery delivering 30-50% longer battery life.",
            "Flagship handheld PC with 7.4\" 90Hz HDR OLED display, 6nm AMD APU, and full Steam library access.",
            "https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=800&auto=format&fit=crop&q=80",
            new[] { "7.4-inch 90Hz HDR OLED screen with 1000 nits peak brightness and 110% DCI-P3 color", "Efficient 6nm AMD 'Sephiroth' APU generates less heat and extends battery life", "Bigger 50Wh battery delivers 3-12 hours of gameplay on a single charge", "Tri-band Wi-Fi 6E for up to 3x faster game download speeds", "Dual square haptic trackpads for precision mouse-driven RTS and FPS navigation" },
            new[] { ("Processor", "6nm AMD APU (4-core Zen 2, 8 RDNA 2 CUs)"), ("Memory", "16GB LPDDR5 RAM (6400 MT/s)"), ("Storage", "512GB NVMe SSD"), ("Display", "7.4\" HDR OLED 90Hz (1280x800)"), ("Weight", "640 grams") }
        ),
        CreateProduct(
            Guid.Parse("ffffffff-6666-0000-0000-000000000005"),
            "ASUS ROG Ally X Handheld Gaming PC (AMD Z1 Extreme, 24GB RAM)", "asus-rog-ally-x-handheld-gaming", 799.99m, "ASUS-ROG-ALLY-X", 20,
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            "Extreme performance Windows 11 handheld gaming PC driven by AMD Ryzen Z1 Extreme processor, massive 80Wh battery, 24GB high-speed LPDDR5X-7500 RAM, 1TB M.2 2280 NVMe SSD, and ergonomic redesigned grips.",
            "High-spec Windows gaming handheld with AMD Z1 Extreme, massive 80Wh battery, and 24GB RAM.",
            "https://images.unsplash.com/photo-1538481199705-c710c4e965fc?w=800&auto=format&fit=crop&q=80",
            new[] { "AMD Ryzen Z1 Extreme processor with 8 cores, 16 threads, and 8.6 TFLOPS graphics", "Huge 80Wh battery doubles battery life compared to the original Ally", "24GB blazing fast LPDDR5X-7500 memory allows allocating 8GB+ directly to VRAM", "Standard M.2 2280 1TB SSD slot makes storage upgrades effortless", "Dual USB-C ports with Thunderbolt 4 / USB4 eGPU support" },
            new[] { ("Processor", "AMD Ryzen Z1 Extreme"), ("Memory", "24GB LPDDR5X-7500"), ("Storage", "1TB PCIe Gen4 NVMe (2280 form factor)"), ("Display", "7\" FHD (1920x1080) 120Hz 500 nits IPS FreeSync"), ("Battery", "80Wh"), ("Weight", "678 grams") }
        ),
        CreateProduct(
            Guid.Parse("ffffffff-6666-0000-0000-000000000006"),
            "Sony PlayStation VR2 (PSVR2) Virtual Reality Headset", "sony-playstation-vr2-headset", 549.99m, "SONY-PSVR2-HEADSET", 20,
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            "Next-gen virtual reality on PS5. Features twin 2000x2040 OLED HDR displays (4K resolution combined), 110-degree field of view, intelligent eye tracking with foveated rendering, headset haptics, and PS VR2 Sense controllers with adaptive triggers.",
            "4K HDR virtual reality headset for PS5 with eye tracking, headset haptics, and Sense controllers.",
            "https://images.unsplash.com/photo-1622979135225-d2ba269bc1df?w=800&auto=format&fit=crop&q=80",
            new[] { "Dual 2000 x 2040 OLED HDR displays deliver breathtaking 4K HDR visuals at up to 120Hz", "Intelligent Eye Tracking enables foveated rendering for razor-sharp visual clarity where you look", "Headset feedback motor adds subtle vibrations to simulate passing vehicles or heartbeat", "Tempest 3D AudioTech dynamically adapts sound to your head position", "Single USB-C cable connection to PS5 with inside-out camera tracking" },
            new[] { ("Display Method", "OLED (2000 x 2040 per eye)"), ("Refresh Rate", "90Hz, 120Hz"), ("Field of View", "Approx. 110 degrees"), ("Sensors", "Six-axis motion sensing system + IR proximity sensor + 4 tracking cameras"), ("Controllers", "PS VR2 Sense with haptics & adaptive triggers") }
        ),
        CreateProduct(
            Guid.Parse("ffffffff-6666-0000-0000-000000000007"),
            "Meta Quest 3 128GB Mixed Reality Headset", "meta-quest-3-mixed-reality", 499.99m, "META-QUEST-3-128", 35,
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            "Breakthrough standalone mixed reality headset. Transforms your living room into an interactive playground with full-color high-resolution passthrough, pancake optical lenses, Snapdragon XR2 Gen 2 chip with double the graphics processing power.",
            "Standalone VR and mixed reality headset with high-res full-color passthrough and pancake lenses.",
            "https://images.unsplash.com/photo-1593508512255-86ab42a8e620?w=800&auto=format&fit=crop&q=80",
            new[] { "Full-color Passthrough blends virtual objects seamlessly into physical surroundings", "Snapdragon XR2 Gen 2 platform delivers double the graphical performance of Quest 2", "Slimmer pancake lens optical profile provides 30% jump in resolution and clarity", "4K+ Infinite Display (2064x2208 per eye) renders crisp text and stunning environments", "Touch Plus ring-free controllers with TruTouch variable haptics" },
            new[] { ("Display Resolution", "2064 x 2208 pixels per eye (4K+ Infinite Display)"), ("Processor", "Qualcomm Snapdragon XR2 Gen 2"), ("Memory", "8GB RAM"), ("Field of View", "110 degrees horizontal / 96 degrees vertical"), ("Weight", "515 grams") }
        ),
        CreateProduct(
            Guid.Parse("ffffffff-6666-0000-0000-000000000008"),
            "Xbox Wireless Controller (Carbon Black, Bluetooth)", "xbox-wireless-controller-carbon-black", 59.99m, "MS-XBOX-CTRL-BLK", 75,
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            "The gold standard game controller featuring sculpted surfaces and refined geometry for enhanced comfort during gameplay. Includes hybrid D-pad, textured grip on triggers and bumpers, dedicated Share button, and cross-platform compatibility.",
            "Ergonomic gaming controller compatible with Xbox Series X|S, Xbox One, PC, Android, and iOS.",
            "https://images.unsplash.com/photo-1600080972464-8e5f35f63d08?w=800&auto=format&fit=crop&q=80",
            new[] { "Textured grip on triggers, bumpers, and back-case prevents slipping during intense sessions", "Hybrid D-pad provides accurate 8-way directional input for fighting and platforming games", "Dedicated Share button effortlessly captures and shares game clips and screenshots", "Seamlessly switch between Xbox consoles, Windows PC, Android, and iOS via Xbox Wireless and Bluetooth", "3.5mm audio jack for direct headset connection" },
            new[] { ("Connectivity", "Xbox Wireless + Bluetooth + USB-C wired"), ("Battery Life", "Up to 40 hours (2 AA batteries)"), ("Compatibility", "Xbox Series X, Series S, Xbox One, Windows 10/11, iOS, Android"), ("Weight", "287 grams") }
        ),
        CreateProduct(
            Guid.Parse("ffffffff-6666-0000-0000-000000000009"),
            "PlayStation DualSense Edge Wireless Pro Controller", "playstation-dualsense-edge-controller", 199.99m, "SONY-DUALSENSE-EDGE", 25,
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            "Ultra-customizable pro controller built for competitive gamers. Features swappable analog stick modules, remappable back buttons, customizable stick sensitivity and dead zones, trigger stops, and profile switching.",
            "Pro customizable PS5 controller with swappable stick modules, back paddles, and trigger stops.",
            "https://images.unsplash.com/photo-1607604276583-eef5d076aa5f?w=800&auto=format&fit=crop&q=80",
            new[] { "Replaceable analog stick modules extend controller lifespan indefinitely", "Two swappable sets of back buttons (half-dome and lever) remappable to any button", "Adjustable trigger stops mechanically shorten trigger pull distance for faster FPS firing", "Customizable control profiles with quick-switch Fn buttons on controller", "Included carrying case charges controller while stored via USB braided cable" },
            new[] { ("Compatibility", "PlayStation 5, PC (Windows)"), ("Features", "Haptic feedback, Adaptive Triggers, Motion Sensor, Mic, Touchpad"), ("Stick Modules", "Hot-swappable modular design"), ("Cable", "Braided USB-C with lockable housing"), ("Weight", "335 grams") }
        ),
        CreateProduct(
            Guid.Parse("ffffffff-6666-0000-0000-000000000010"),
            "Nintendo Switch Lite (Turquoise Portable Console)", "nintendo-switch-lite-turquoise", 199.99m, "NTDO-SWITCH-LITE-TURQ", 45,
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            "Compact and lightweight system dedicated strictly to handheld play. Features integrated controls with a proper +Control Pad, 5.5-inch touch screen, and access to the massive library of handheld-compatible Nintendo Switch games.",
            "Dedicated handheld Nintendo Switch console with built-in controls and +Control Pad.",
            "https://images.unsplash.com/photo-1585857188963-e38050965377?w=800&auto=format&fit=crop&q=80",
            new[] { "Dedicated exclusively to handheld play for portable on-the-go gaming", "Integrated controls with a classic +Control Pad on the left side", "Lightweight 275g compact body fits comfortably in backpacks and jackets", "Plays all Nintendo Switch games that support Handheld mode (Zelda, Mario, Pokemon)", "3.0 to 7.0 hours of battery life" },
            new[] { ("Display", "5.5\" LCD touch screen (1280x720)"), ("Weight", "0.61 lbs (275 grams)"), ("Storage", "32GB (MicroSD expandable up to 2TB)"), ("Battery", "3570 mAh Lithium-ion"), ("Audio", "Stereo speakers + 3.5mm headphone jack") }
        ),
        CreateProduct(
            Guid.Parse("ffffffff-6666-0000-0000-000000000011"),
            "Razer Kishi V2 Mobile Gaming Controller for iPhone & Android", "razer-kishi-v2-mobile-controller", 99.99m, "RAZER-KISHI-V2", 40,
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            "Universal mobile gaming controller featuring microswitch buttons, analog triggers, and programmable multifunction macros. Direct USB-C connection eliminates latency for cloud gaming and native mobile titles.",
            "Low-latency USB-C mobile controller with console-quality microswitch buttons for smartphones.",
            "https://images.unsplash.com/photo-1551103782-8ab07afd45c1?w=800&auto=format&fit=crop&q=80",
            new[] { "Console-quality microswitch buttons, analog triggers, and tactile D-pad", "Direct USB-C connection guarantees virtually zero input lag during competitive play", "Universal extendable bridge accommodates virtually all modern smartphones", "Pass-through charging keeps phone powered during marathon gaming sessions", "Razer Nexus app integrates discovery, game launch, and livestreaming" },
            new[] { ("Connection", "Direct USB-C (Zero latency)"), ("Pass-through Charging", "Yes (USB-C)"), ("Buttons", "2 clickable analog thumbsticks, D-pad, 4 face buttons, 2 triggers, 2 bumpers, 2 multifunction"), ("Weight", "123 grams"), ("Compatibility", "Android and iPhone 15/16 series") }
        ),
        CreateProduct(
            Guid.Parse("ffffffff-6666-0000-0000-000000000012"),
            "Logitech G Cloud Gaming Handheld (1080p 7\" IPS Screen)", "logitech-g-cloud-gaming-handheld", 299.99m, "LOGI-G-CLOUD-HANDHELD", 25,
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            "Cloud-first portable gaming device built for Xbox Cloud Gaming, GeForce NOW, and remote play. Delivers a spacious 7-inch 1080p 60Hz touchscreen, console-grade controls with haptics, and a 12+ hour battery life.",
            "Dedicated cloud gaming handheld with 7\" 1080p screen, 12+ hour battery, and lightweight ergonomic body.",
            "https://images.unsplash.com/photo-1511512578047-dfb367046420?w=800&auto=format&fit=crop&q=80",
            new[] { "12+ hours of battery life thanks to energy-efficient cloud streaming architecture", "7-inch Full HD (1920x1080) 60Hz IPS touchscreen with 450 nits brightness", "Full console-grade controls with linear triggers, hall effect sensors, and haptics", "Lightweight 463g ergonomic form factor designed for comfortable long sessions", "Google Play Store access to stream via Xbox Cloud Gaming, GeForce NOW, PS Remote Play, Steam Link" },
            new[] { ("Display", "7\" IPS Full HD (1920x1080, 60Hz, 16:9)"), ("Battery Life", "12+ hours (6000 mAh)"), ("Weight", "463 grams"), ("Audio", "Stereo speakers, dual mics, 3.5mm jack, Bluetooth 5.1"), ("Wireless", "Dual-band Wi-Fi 5 (2.4GHz / 5GHz)") }
        ),
        CreateProduct(
            Guid.Parse("00000000-7777-0000-0000-000000000001"),
            "Logitech MX Master 3S Wireless Performance Mouse", "logitech-mx-master-3s-mouse", 99.99m, "LOGI-MX-MASTER-3S", 65,
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            "The premier wireless mouse for developers, designers, and power users. Features 8K DPI any-surface Darkfield tracking (even on glass), Quiet Clicks (90% less noise), MagSpeed electromagnetic scroll wheel (1,000 lines/sec), and thumb wheel.",
            "Industry-standard productivity mouse with 8K DPI glass tracking, MagSpeed wheel, and Quiet Clicks.",
            "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?w=800&auto=format&fit=crop&q=80",
            new[] { "Track anywhere, even on glass, with an 8,000 DPI Darkfield optical sensor", "Quiet Click technology reduces click noise by 90% while retaining tactile click feel", "MagSpeed electromagnetic scroll wheel scrolls 1,000 lines per second silently", "Ergonomic silhouette supports palm and fingers with integrated gesture button", "Logi Options+ software allows cross-computer control and flow between Mac and Windows" },
            new[] { ("Sensor", "Darkfield high precision (200-8000 DPI)"), ("Battery Life", "Up to 70 days (1 min charge = 3 hours use)"), ("Connectivity", "Bluetooth Low Energy + Logi Bolt USB Receiver"), ("Buttons", "7 buttons (Left/Right, Back/Forward, App-Switch, Wheel mode, Middle)"), ("Weight", "141 grams") }
        ),
        CreateProduct(
            Guid.Parse("00000000-7777-0000-0000-000000000002"),
            "Apple Magic Keyboard with Touch ID and Numeric Keypad", "apple-magic-keyboard-touch-id", 199.00m, "APPLE-MAGIC-KB-NUM", 45,
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            "Wireless, rechargeable keyboard tailored for Mac with Apple silicon. Includes Touch ID for fast, easy, and secure logins and purchases, an extended layout with document navigation controls, full-size arrow keys, and numeric keypad.",
            "Full-size slim Mac keyboard with Touch ID fingerprint login, numeric keypad, and long battery life.",
            "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=800&auto=format&fit=crop&q=80",
            new[] { "Integrated Touch ID sensor provides fast, secure fingerprint authentication on Mac", "Extended layout with numeric keypad and full-size directional arrow keys", "Low-profile scissor mechanism beneath each key delivers stable, precise typing", "Rechargeable internal battery powers keyboard for about a month between charges", "Woven USB-C to Lightning charge cable included" },
            new[] { ("Connectivity", "Bluetooth + Lightning port + Wireless"), ("System Requirements", "Mac with Apple silicon running macOS 11.4 or later"), ("Weight", "369 grams"), ("Dimensions", "0.41-1.09 cm x 41.87 cm x 11.49 cm") }
        ),
        CreateProduct(
            Guid.Parse("00000000-7777-0000-0000-000000000003"),
            "Keychron Q1 Pro Wireless Custom Mechanical Keyboard (QMK/VIA)", "keychron-q1-pro-mechanical-keyboard", 199.99m, "KEYCHRON-Q1-PRO", 30,
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            "Full CNC machined 6063 aluminum 75% mechanical keyboard with wireless Bluetooth 5.1 and wired connectivity. Double-gasket mount design, south-facing RGB, hot-swappable Keychron K Pro red switches, and full QMK/VIA key remapping support.",
            "All-metal CNC custom mechanical keyboard with hot-swap switches, double-gasket dampening, and QMK/VIA.",
            "https://images.unsplash.com/photo-1595225476474-87563907a212?w=800&auto=format&fit=crop&q=80",
            new[] { "Full CNC machined 6063 aluminum body polished, anodized, and sandblasted", "Double-Gasket Design reduces acoustic resonance and delivers a deep, satisfying thock", "Fully hot-swappable PCB supports almost all 3-pin and 5-pin MX mechanical switches", "QMK and VIA open-source support enables effortless key remaps and custom macros", "Connects up to 3 devices seamlessly via Bluetooth 5.1" },
            new[] { ("Layout", "75% with programmable rotary encoder knob"), ("Body Material", "Full CNC Aluminum"), ("Switches", "Keychron K Pro Mechanical (Hot-Swappable)"), ("Battery", "4000 mAh (Up to 300 hours with RGB off)"), ("Connectivity", "Bluetooth 5.1 / Type-C Wired") }
        ),
        CreateProduct(
            Guid.Parse("00000000-7777-0000-0000-000000000004"),
            "LG UltraFine 27\" 4K Ergo IPS Monitor (USB-C 60W, HDR10)", "lg-ultrafine-27-4k-ergo-monitor", 499.99m, "LG-27UN880-ERGO", 25,
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            "Ergonomic 27-inch 4K UHD (3840x2160) IPS monitor with innovative Ergo C-Clamp arm that extends, retracts, swivels, pivots, tilts, and adjusts in height. Single USB-C cable delivers 4K video, high-speed data, and 60W laptop charging.",
            "27\" 4K IPS display with flexible Ergo desk clamp arm, 99% sRGB, and USB-C 60W one-cable dock.",
            "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=800&auto=format&fit=crop&q=80",
            new[] { "Innovative Ergo Arm with C-Clamp allows complete 3D articulation (extend, retract, pivot, tilt, height)", "27-inch 4K UHD (3840 x 2160) IPS display with 99% sRGB color gamut coverage", "Single USB-C connection transfers 4K video, USB data, and 60W Power Delivery", "HDR10 support brings vivid contrast and high dynamic range to creative workflows", "AMD FreeSync technology reduces screen tearing in games and media" },
            new[] { ("Screen Size", "27-inch IPS (3840x2160 @ 60Hz)"), ("Color Gamut", "sRGB 99% (CIE1931)"), ("Stand Type", "Ergo Arm (Extend/Retract 180mm, Swivel 280 deg, Pivot 90 deg)"), ("Ports", "USB-C (60W PD), 2x HDMI, DisplayPort, 2x USB 3.0 downstream"), ("Speakers", "MaxxAudio 5W x 2") }
        ),
        CreateProduct(
            Guid.Parse("00000000-7777-0000-0000-000000000005"),
            "Dell UltraSharp 32\" 4K USB-C Hub Monitor (U3223QE, IPS Black)", "dell-ultrasharp-32-4k-usb-c-u3223qe", 799.99m, "DELL-U3223QE-32-4K", 20,
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            "Groundbreaking 31.5-inch 4K monitor featuring IPS Black technology with a phenomenal 2,000:1 contrast ratio (double conventional IPS). Built-in USB-C Hub with 90W Power Delivery, RJ45 Ethernet, KVM switch, and 98% DCI-P3 color.",
            "Premier 32\" 4K monitor with IPS Black 2000:1 contrast, built-in USB-C hub, RJ45, and KVM switch.",
            "https://images.unsplash.com/photo-1547082299-de196ea013d6?w=800&auto=format&fit=crop&q=80",
            new[] { "IPS Black technology achieves deep 2,000:1 contrast ratio with deep blacks and gray levels", "Comprehensive USB-C Hub: 90W charging, RJ45 Ethernet, and 10Gbps USB-A ports", "Built-in KVM (Keyboard, Video, Mouse) switch lets you control 2 PCs with one mouse/keyboard", "Color accuracy: 98% DCI-P3 and 100% sRGB with factory calibration Delta E < 2", "ComfortView Plus built-in low blue light hardware reduces eye fatigue without color shift" },
            new[] { ("Display", "31.5\" 4K IPS Black (3840x2160, 60Hz)"), ("Contrast Ratio", "2,000:1"), ("Color Support", "1.07 billion colors (98% DCI-P3)"), ("Hub Connectivity", "USB-C with 90W PD, RJ45 Ethernet, 4x USB 3.2, DisplayPort 1.4, HDMI 2.0"), ("Adjustability", "Height, tilt, swivel, pivot") }
        ),
        CreateProduct(
            Guid.Parse("00000000-7777-0000-0000-000000000006"),
            "Samsung Odyssey Neo G9 49\" Dual QHD 240Hz Curved Gaming Monitor", "samsung-odyssey-neo-g9-49-curved", 1499.99m, "SAMSUNG-NEO-G9-49", 12,
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            "Colossal 49-inch 32:9 super ultrawide curved monitor matching two 27\" 1440p monitors side-by-side. Powered by Quantum Mini-LED backlighting with 2,048 local dimming zones, 2000 nits peak brightness, 240Hz refresh rate, and 1000R curvature.",
            "49\" Super Ultrawide 32:9 curved gaming monitor with Quantum Mini-LED, 240Hz, and 1ms response.",
            "https://images.unsplash.com/photo-1550751827-4bd374c3f58b?w=800&auto=format&fit=crop&q=80",
            new[] { "49-inch 32:9 super ultrawide Dual QHD (5120x1440) resolution replaces dual monitors cleanly", "Quantum Mini-LED backlighting with Quantum HDR 2000 and 2,048 local dimming zones", "Immersive 1000R deep curvature matches the human field of view", "Blistering 240Hz refresh rate with 1ms (GtG) response time and NVIDIA G-Sync / AMD FreeSync", "CoreSync lighting projects on-screen game colors onto your back wall" },
            new[] { ("Resolution", "5120 x 1440 Dual QHD (32:9)"), ("Panel", "Curved VA with Quantum Mini-LED"), ("Refresh Rate", "240Hz"), ("Response Time", "1ms (GtG)"), ("Peak Brightness", "2000 cd/m2") }
        ),
        CreateProduct(
            Guid.Parse("00000000-7777-0000-0000-000000000007"),
            "Logitech C920x HD Pro Webcam (Full HD 1080p/30fps)", "logitech-c920x-hd-pro-webcam", 69.99m, "LOGI-C920X-WEBCAM", 60,
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            "The world's most popular webcam for remote work, Zoom meetings, and streaming. Delivers crisp Full HD 1080p video at 30 fps, dual omnidirectional stereo microphones with noise reduction, and automatic HD light correction.",
            "Full HD 1080p webcam with dual stereo mics, autofocus, and automatic light correction.",
            "https://images.unsplash.com/photo-1587826080692-f439cd0b70da?w=800&auto=format&fit=crop&q=80",
            new[] { "Full HD 1080p video calling and recording at 30 frames per second", "Full HD glass lens with premium autofocus captures razor-sharp images", "Automatic HD light correction fine-tunes lighting to produce bright, natural pictures", "Dual stereo microphones with noise-filtering technology capture natural audio", "Universal tripod-ready monitor clip fits laptops, LCDs, and monitors" },
            new[] { ("Max Resolution", "1080p/30fps - 720p/30fps"), ("Focus Type", "Autofocus with five-element glass lens"), ("Field of View", "78 degrees"), ("Microphone", "Dual stereo omnidirectional"), ("Cable Length", "1.5 meters USB-A") }
        ),
        CreateProduct(
            Guid.Parse("00000000-7777-0000-0000-000000000008"),
            "Elgato Stream Deck MK.2 – 15 LCD Macro Keys Studio Controller", "elgato-stream-deck-mk2-controller", 149.99m, "ELGATO-STREAMDECK-MK2", 35,
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            "The ultimate studio command console featuring 15 customizable LCD keys to trigger unlimited actions with one tap. Control apps, launch media, adjust audio, switch scenes in OBS, and trigger developer workflow scripts.",
            "15 customizable LCD keys studio controller to automate shortcuts, OBS, audio, and coding workflows.",
            "https://images.unsplash.com/photo-1618788372246-79faff0c3742?w=800&auto=format&fit=crop&q=80",
            new[] { "15 customizable LCD keys trigger immediate actions with visual confirmation", "One-touch operation: Control OBS, Twitch, YouTube, Spotify, Philips Hue, VS Code", "Smart Profiles switch key configurations automatically based on the active application", "Detachable 45-degree desktop stand and swappable faceplate design", "Stream Deck Store plugins offer seamless integration with Discord, Zoom, Teams" },
            new[] { ("Keys", "15 customizable LCD keys"), ("Interface", "USB 2.0"), ("Dimensions", "118 x 84 x 25 mm"), ("Weight", "145 grams (without stand)"), ("Compatibility", "Windows 10/11, macOS 10.15+") }
        ),
        CreateProduct(
            Guid.Parse("00000000-7777-0000-0000-000000000009"),
            "Anker 575 USB-C Docking Station (12-in-1, 85W Charging)", "anker-575-usb-c-docking-station-12-in-1", 139.99m, "ANKER-575-DOCK-12IN1", 40,
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            "Versatile 12-in-1 desktop docking station providing 85W high-speed charging to laptops, dual HDMI and DisplayPort for triple monitor setups, Gigabit Ethernet, SD/TF card slots, and high-speed 5Gbps USB ports.",
            "12-in-1 desktop USB-C dock supporting triple displays, 85W power delivery, and Gigabit Ethernet.",
            "https://images.unsplash.com/photo-1625842268584-8f3296236761?w=800&auto=format&fit=crop&q=80",
            new[] { "Massive 12-in-1 expansion: 2x HDMI, DisplayPort, 3x USB-A, 2x USB-C, Ethernet, SD/microSD, 3.5mm audio", "Up to 85W high-speed Power Delivery charges connected laptop while powering all peripherals", "Triple monitor support: Drive up to three external displays simultaneously", "Gigabit Ethernet port ensures ultra-stable wired internet connectivity for downloads and Zoom", "Compact upright desktop footprint saves valuable desk space" },
            new[] { ("Power Delivery", "85W to laptop (via included 100W DC power supply)"), ("Video Outputs", "2x HDMI 2.0 + 1x DisplayPort 1.4"), ("Data Ports", "3x USB 3.0 (5Gbps), 1x USB-C (5Gbps), SD/microSD slots"), ("Network", "RJ45 Gigabit Ethernet (10/100/1000 Mbps)"), ("Audio", "3.5mm Audio In/Out") }
        ),
        CreateProduct(
            Guid.Parse("00000000-7777-0000-0000-000000000010"),
            "Blue Yeti USB Microphone for Recording & Streaming (Blackout)", "blue-yeti-usb-microphone-blackout", 129.99m, "BLUE-YETI-MIC-BLK", 50,
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            "The world's #1 USB microphone for podcasting, streaming, YouTube videos, and voiceovers. Proprietary tri-capsule technology offers 4 selectable pickup patterns (Cardioid, Omnidirectional, Bidirectional, and Stereo).",
            "Studio USB condenser microphone with 4 pickup patterns, zero-latency headphone monitoring, and gain control.",
            "https://images.unsplash.com/photo-1590602847861-f357a9332bbc?w=800&auto=format&fit=crop&q=80",
            new[] { "Custom three-capsule array produces clear, powerful, broadcast-quality sound", "Four pickup patterns: Cardioid (solo), Omni (room), Bidirectional (interview), Stereo", "On-board audio controls: Headphone volume, pattern selection, instant mute, mic gain", "Zero-latency monitoring via 3.5mm headphone output on base of microphone", "Plug-and-play USB setup compatible with Mac and PC without drivers" },
            new[] { ("Transducer Type", "Condenser, Pressure Gradient with USB digital output"), ("Polar Patterns", "Cardioid, Bidirectional, Omnidirectional, Stereo"), ("Sample Rate", "48 kHz / 16-bit"), ("Frequency Response", "20 Hz - 20 kHz"), ("Weight", "1.2 lbs (550 grams with stand)") }
        ),
        CreateProduct(
            Guid.Parse("00000000-7777-0000-0000-000000000011"),
            "CalDigit TS4 Thunderbolt 4 Dock (18 Ports, 98W Power Delivery)", "caldigit-ts4-thunderbolt-4-dock", 399.95m, "CALDIGIT-TS4-DOCK", 20,
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            "The undisputed king of Thunderbolt 4 docks with an unprecedented 18 ports of connectivity. Provides up to 98W laptop charging, 2.5 Gigabit Ethernet, dual 6K 60Hz display support, and UHS-II SD/microSD card readers.",
            "Ultimate 18-port Thunderbolt 4 dock with 98W charging, 2.5GbE LAN, and dual 6K display support.",
            "https://images.unsplash.com/photo-1544717305-2782549b5136?w=800&auto=format&fit=crop&q=80",
            new[] { "Unmatched 18 ports of connectivity - the most comprehensive dock on the market", "98W Power Delivery to host laptop keeps powerful MacBook Pro and Windows machines charged", "2.5 Gigabit Ethernet port provides 2.5x faster networking speeds than standard gigabit", "Single 8K display support or Dual 6K 60Hz displays on Apple silicon Mac / Windows", "Heavy-duty aluminum heat-sink housing operates silently without fans" },
            new[] { ("Ports", "3x TB4, 5x USB-A (10Gbps), 3x USB-C (10Gbps), 1x DisplayPort 1.4, 1x 2.5GbE, 1x SD, 1x microSD, 3x Audio"), ("Power Delivery", "Up to 98W host charging"), ("Network", "2.5 Gigabit Ethernet (2500 Mbps)"), ("Dimensions", "141 x 42 x 113 mm"), ("Weight", "640 grams") }
        ),
        CreateProduct(
            Guid.Parse("00000000-8888-0000-0000-000000000001"),
            "Apple HomePod (2nd Generation, Midnight Smart Speaker)", "apple-homepod-2nd-gen-midnight", 299.00m, "APPLE-HOMEPOD-2-MID", 35,
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            "A powerhouse of a smart speaker combining high-fidelity acoustic engineering with Siri intelligence. Features high-excursion woofer, beamforming 5-tweeter array, room sensing acoustic tuning, Spatial Audio with Dolby Atmos, and Matter smart home hub.",
            "High-fidelity smart speaker with Spatial Audio, Dolby Atmos, room sensing, and built-in Matter hub.",
            "https://images.unsplash.com/photo-1543512214-318c7553f230?w=800&auto=format&fit=crop&q=80",
            new[] { "High-excursion 4-inch woofer delivers deep, rich bass notes with motor-driven diaphragm", "Beamforming array of 5 horn-loaded tweeters optimizes high frequencies with directional clarity", "Room sensing automatically listens to audio reflections to calibrate sound for its room position", "Matter and Thread smart home hub integrates and automates home accessories", "Built-in temperature and humidity sensor allows creating smart home automations" },
            new[] { ("Acoustics", "4\" high-excursion woofer + 5 horn tweeters + internal low-frequency mic"), ("Audio Formats", "Spatial Audio with Dolby Atmos, Lossless audio"), ("Wireless", "802.11n Wi-Fi, Bluetooth 5.0, Thread, Matter"), ("Sensors", "Temperature and Humidity, Sound Recognition (smoke alarms)"), ("Weight", "5.16 lbs (2.3 kg)") }
        ),
        CreateProduct(
            Guid.Parse("00000000-8888-0000-0000-000000000002"),
            "Amazon Echo Dot (5th Gen) with Clock (Glacier White)", "amazon-echo-dot-5th-gen-clock", 59.99m, "AMZN-ECHODOT-5-CLK", 65,
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            "Amazon's best-sounding Echo Dot with an improved LED display showing time, weather, alarms, and song titles. Offers vibrant audio with clearer vocals and deeper bass, built-in temperature sensor, and eero built-in Wi-Fi extender.",
            "Popular smart speaker with LED clock display, Alexa voice control, and eero Wi-Fi mesh extension.",
            "https://images.unsplash.com/photo-1518444065439-e933c06ce9cd?w=800&auto=format&fit=crop&q=80",
            new[] { "Improved LED display shows the time, alarms, weather forecasts, and calendar events", "Larger 1.73-inch front-firing speaker delivers clearer vocals and 2x deeper bass", "Built-in temperature sensor triggers smart fans or AC when room temperature rises", "Eero built-in adds up to 1,000 sq ft of Wi-Fi coverage to compatible eero mesh networks", "Tap gesture controls on top to snooze alarms or pause music" },
            new[] { ("Speaker Size", "1.73\" (44 mm) front-firing speaker"), ("Display", "High-contrast LED dot matrix display"), ("Wireless", "Dual-band Wi-Fi (2.4 and 5 GHz), Bluetooth Low Energy"), ("Dimensions", "100 x 100 x 89 mm"), ("Weight", "349 grams") }
        ),
        CreateProduct(
            Guid.Parse("00000000-8888-0000-0000-000000000003"),
            "Google Nest Learning Thermostat (4th Gen, Polished Silver)", "google-nest-learning-thermostat-4th-gen", 279.99m, "GOOGLE-NEST-THERM-4", 25,
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            "Redesigned smart thermostat featuring a borderless 60% larger high-res display that floats on the wall. Dynamic Farsight shows time, temperature, and weather, while AI learns your habits to automatically save heating and cooling costs.",
            "Intelligent learning thermostat with borderless dynamic display, Matter support, and smart energy savings.",
            "https://images.unsplash.com/photo-1558002038-1055907df827?w=800&auto=format&fit=crop&q=80",
            new[] { "Borderless curved glass design with a 60% larger display that lights up as you walk near", "Smart Schedule uses AI to learn your thermal preferences and automatically adjust to save energy", "System Health Monitor spots HVAC anomalies like heating failure before serious damage occurs", "Matter compatibility allows seamless control via Apple Home, Google Home, and Alexa", "Includes Nest Temperature Sensor for localized comfort in specific bedrooms" },
            new[] { ("Display", "24-bit color LCD (600x600 resolution, borderless)"), ("Finish", "Polished Stainless Steel ring"), ("Compatibility", "Up to 3 stages of heating and 2 stages of cooling"), ("Wireless", "Wi-Fi, Bluetooth Low Energy, Matter/Thread"), ("Sensors", "Temp, Humidity, Proximity, Ambient Light, Radar motion") }
        ),
        CreateProduct(
            Guid.Parse("00000000-8888-0000-0000-000000000004"),
            "Philips Hue White & Color Ambiance Starter Kit (3 Bulbs + Bridge)", "philips-hue-color-starter-kit", 189.99m, "PHILIPS-HUE-KIT-3PK", 35,
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            "Transform your home lighting with 16 million colors and 50,000 shades of warm-to-cool white light. Includes 3 smart A19 1100-lumen color bulbs, Smart Button, and the Hue Bridge for rock-solid Zigbee network reliability and automation.",
            "Smart lighting starter kit with 3 color 1100-lumen bulbs, Hue Bridge, and 16 million customizable colors.",
            "https://images.unsplash.com/photo-1507646227500-4d389b0012be?w=800&auto=format&fit=crop&q=80",
            new[] { "16 million vibrant colors and 50,000 shades of warm-to-cool white light create instant mood ambiance", "Hue Bridge uses dedicated Zigbee protocol so lights never slow down your home Wi-Fi", "Sync lights with music, PC games, and TV movies via Hue Sync software", "Automate daily sunrise wake-ups and sunset wind-downs", "Works with Apple HomeKit, Google Assistant, Amazon Alexa, and SmartThings" },
            new[] { ("Brightness", "1100 lumens per bulb (75W equivalent)"), ("Fitting", "Standard E26 base"), ("Color Temperature", "2000K-6500K + 16 million colors"), ("Included", "3x A19 Color Bulbs, 1x Hue Bridge, 1x Smart Button, Power/Ethernet"), ("Protocol", "Zigbee + Bluetooth") }
        ),
        CreateProduct(
            Guid.Parse("00000000-8888-0000-0000-000000000005"),
            "Roborock S8 Pro Ultra Robot Vacuum & Mop (RockDock Ultra)", "roborock-s8-pro-ultra-robot-vacuum", 1599.99m, "ROBOROCK-S8-PRO-ULTRA", 15,
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            "Flagship automated floor cleaning robot with RockDock Ultra all-in-one docking station (auto-emptying, auto-washing mop, warm air drying, auto-tank refilling). Boasts 6000 Pa suction and dual rubber DuoRoller brushes.",
            "Flagship robot vacuum and mop with RockDock Ultra self-emptying, self-washing, and warm air drying.",
            "https://images.unsplash.com/photo-1628191010210-a59de33e5941?w=800&auto=format&fit=crop&q=80",
            new[] { "RockDock Ultra automatically washes the mop pad, self-empties dustbin for 7 weeks, and dries pad with warm air", "Extreme 6,000 Pa suction effortlessly lifts pet hair and deep carpet debris", "DuoRoller Riser dual rubber brushes rotate in reverse to prevent hair tangles", "VibraRise 2.0 mopping system scrubs floors at 3000 cycles per minute and auto-lifts 5mm over carpets", "Reactive 3D Obstacle Avoidance navigates around pet toys, shoes, and power cords" },
            new[] { ("Suction Power", "6,000 Pa"), ("Mopping", "VibraRise 2.0 sonic vibration (3000 scrubs/min with auto-lift)"), ("Docking Station", "RockDock Ultra (Auto Wash, Empty, Dry, Refill, Clean)"), ("Navigation", "PreciSense LiDAR + Reactive 3D Obstacle Avoidance"), ("Battery", "5200 mAh (Up to 180 min runtime)") }
        ),
        CreateProduct(
            Guid.Parse("00000000-8888-0000-0000-000000000006"),
            "Ring Video Doorbell Pro 2 (Head-to-Toe HD+ Video)", "ring-video-doorbell-pro-2", 249.99m, "RING-VDB-PRO2", 30,
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            "Premium wired video doorbell featuring 1536p HD+ Head-to-Toe video, 3D Motion Detection with radar-powered Bird's Eye View, Two-Way Talk with Audio+, and Alexa Greetings.",
            "Wired video doorbell with 1536p Head-to-Toe view, 3D radar motion detection, and two-way talk.",
            "https://images.unsplash.com/photo-1585771724684-38269d6639fd?w=800&auto=format&fit=crop&q=80",
            new[] { "1536p HD+ Head-to-Toe video lets you see visitors from head to toe or check packages on porch", "3D Motion Detection uses radar technology to pinpoint motion distance up to 30 feet away", "Bird's Eye View provides an aerial map view of motion events around your front yard", "Two-Way Talk with Audio+ suppresses background wind noise for crystal-clear conversations", "Hardwired installation connects to existing doorbell wiring for non-stop power" },
            new[] { ("Video Resolution", "1536p HD+ Video, Live View, Color Night Vision"), ("Field of View", "150 degrees horizontal, 150 degrees vertical"), ("Audio", "Two-way audio with noise cancellation"), ("Power", "Hardwired (16-24 VAC doorbell transformer)"), ("Dimensions", "114 mm x 49 mm x 22 mm") }
        ),
        CreateProduct(
            Guid.Parse("00000000-8888-0000-0000-000000000007"),
            "Google Nest Cam Outdoor/Indoor (Weatherproof Battery Smart Cam)", "google-nest-cam-outdoor-indoor-battery", 179.99m, "GOOGLE-NEST-CAM-BATT", 40,
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            "Wire-free outdoor or indoor smart security camera with intelligent alerts that differentiate between people, vehicles, and animals. Features 1080p HDR video with night vision and magnetic wall mount.",
            "Wire-free battery security camera with intelligent person/vehicle detection and weather resistance.",
            "https://images.unsplash.com/photo-1557597774-9d273605dfa9?w=800&auto=format&fit=crop&q=80",
            new[] { "Intelligent alerts distinguish between people, animals, and vehicles without a subscription", "Wire-free battery operation allows mounting on any wall, fence, or eave with magnetic base", "1080p HDR video with Night Vision provides clear recordings in daylight or pitch darkness", "Stores up to 3 hours of free event video history locally if Wi-Fi or power goes down", "IP54 weather resistant withstands rain, snow, and extreme summer temperatures" },
            new[] { ("Camera", "1/2.8-inch, 2-megapixel sensor, 1080p HDR at 30 fps"), ("Field of View", "130-degree diagonal"), ("Night Vision", "High-power 850 nm infrared LEDs"), ("Battery", "Built-in rechargeable 6 Ah lithium-ion"), ("Audio", "High-quality speaker and microphone with noise reduction") }
        ),
        CreateProduct(
            Guid.Parse("00000000-8888-0000-0000-000000000008"),
            "TP-Link Deco BE85 Wi-Fi 7 Tri-Band Mesh System (2-Pack, 22 Gbps)", "tp-link-deco-be85-wifi-7-mesh-2pack", 999.99m, "TPLINK-DECO-BE85-2PK", 15,
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            "Next-generation Wi-Fi 7 Whole Home Mesh system with speeds up to 22 Gbps. Tri-band architecture (6GHz, 5GHz, 2.4GHz), dual 10Gbps Ethernet ports per unit, Multi-Link Operation (MLO), and coverage for up to 7,600 sq ft.",
            "Next-gen Wi-Fi 7 mesh system delivering 22 Gbps tri-band speeds and dual 10Gbps Ethernet ports.",
            "https://images.unsplash.com/photo-1544197150-b99a580bb7a8?w=800&auto=format&fit=crop&q=80",
            new[] { "Blazing Wi-Fi 7 speeds up to 22 Gbps across 12 streams on 6GHz, 5GHz, and 2.4GHz bands", "Multi-Link Operation (MLO) enables simultaneous transmission across bands for low latency", "Two 10 Gbps Ethernet/SFP+ combo ports and two 2.5 Gbps ports on each node for multi-gig fiber", "Covers homes up to 7,600 square feet with seamless roaming for over 200 connected devices", "TP-Link HomeShield delivers enterprise-grade network security and robust parental controls" },
            new[] { ("Speed Rating", "BE22000 (11520 Mbps on 6GHz + 8640 Mbps on 5GHz + 1376 Mbps on 2.4GHz)"), ("Ethernet Ports", "1x 10Gbps RJ45/SFP+ Combo, 1x 10Gbps RJ45, 2x 2.5Gbps RJ45, 1x USB 3.0"), ("Coverage", "Up to 7,600 sq. ft. (2-pack)"), ("Antennas", "8x High-Gain internal antennas with beamforming"), ("Processor", "Quad-Core 2.2 GHz CPU") }
        ),
        CreateProduct(
            Guid.Parse("00000000-8888-0000-0000-000000000009"),
            "ASUS ROG Rapture GT-AXE16000 Quad-Band Wi-Fi 6E Gaming Router", "asus-rog-rapture-gt-axe16000", 599.99m, "ASUS-ROG-GT-AXE16000", 20,
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            "The world's first quad-band Wi-Fi 6E gaming router delivering speeds up to 16,000 Mbps. Features dual 10G ports, four 1G ports, dedicated 2.5G WAN port, quad-core 2.0GHz 64-bit CPU, and triple-level game acceleration.",
            "Flagship quad-band gaming router with dual 10G ports, 16 Gbps bandwidth, and ROG game acceleration.",
            "https://images.unsplash.com/photo-1563770660941-20978e870e26?w=800&auto=format&fit=crop&q=80",
            new[] { "World's first quad-band gaming router: 6GHz, two 5GHz bands, and 2.4GHz for zero interference", "Dual 10 Gbps ports and 2.5 Gbps WAN port unleash ultra-high-speed fiber internet and NAS connections", "Triple-level game acceleration optimizes gaming packet priority from device to game server", "Quad-core 2.0 GHz 64-bit flagship processor handles heavy traffic and VPN encryption", "Commercial-grade AiProtection Pro powered by Trend Micro shields all network devices" },
            new[] { ("Bandwidth", "16,000 Mbps (Quad-Band: 6GHz + 5GHz-1 + 5GHz-2 + 2.4GHz)"), ("Ports", "2x 10G, 1x 2.5G WAN, 4x 1G LAN, 1x USB 3.2, 1x USB 2.0"), ("Processor", "2.0 GHz quad-core 64-bit CPU"), ("Memory", "2GB DDR4 RAM + 256MB Flash"), ("Weight", "2.42 kg") }
        ),
        CreateProduct(
            Guid.Parse("00000000-8888-0000-0000-000000000010"),
            "Kasa Smart Wi-Fi Plug Mini (4-Pack by TP-Link)", "kasa-smart-wi-fi-plug-mini-4pack", 29.99m, "KASA-SMART-PLUG-4PK", 80,
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            "Compact smart plugs under $30 that let you turn lamps, coffee makers, and fans on/off from anywhere. Voice control via Alexa and Google Assistant, scheduling, timer countdowns, and no hub required.",
            "Sub-$30 4-pack smart Wi-Fi plugs with Alexa/Google voice control, away mode, and scheduling.",
            "https://images.unsplash.com/photo-1546483875-ad9014c88eba?w=800&auto=format&fit=crop&q=80",
            new[] { "Unbeatable smart home entry price under $30 for a pack of 4 smart plugs", "Compact mini design leaves second wall outlet completely unobstructed for other plugs", "Control lights, fans, and humidifiers from anywhere using the Kasa smartphone app", "Hands-free voice control compatible with Amazon Alexa and Google Assistant", "Away Mode automatically turns lamps on and off at random intervals to deter burglars" },
            new[] { ("Electrical Rating", "15A, 120V~, 60Hz (1800W Maximum Load)"), ("Quantity", "4 Plugs included"), ("Wireless", "2.4GHz Wi-Fi (No hub required)"), ("Dimensions", "67 x 38 x 40 mm") }
        ),
    };
}
