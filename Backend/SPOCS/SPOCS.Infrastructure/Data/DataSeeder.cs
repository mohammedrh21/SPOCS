using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SPOCS.Application.Contracts;
using SPOCS.Application.Contracts.AI;
using SPOCS.Domain.Entities;

namespace SPOCS.Infrastructure.Data;

public class DataSeeder : IDataSeeder
{
    private readonly ApplicationDbContext context;
    private readonly RoleManager<IdentityRole<Guid>> roleManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<DataSeeder> logger;
    private static readonly string[] Roles =
    {
        "Admin"
    };

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

        // If categories already exist, data is already seeded; check/generate any missing embeddings
        if (await context.Categories.AnyAsync())
        {
            await TryGenerateEmbeddingsAsync();
            return;
        }

        // 3. Seed Categories
        var catLaptops = new Category
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Laptops",
            Slug = "laptops",
            Description = "High-performance laptops for programming, gaming, study, and creative workflows.",
            ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&auto=format&fit=crop&q=80"
        };

        var catPhones = new Category
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Smartphones",
            Slug = "smartphones",
            Description = "Flagship and budget smartphones featuring outstanding camera optics and battery life.",
            ImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=800&auto=format&fit=crop&q=80"
        };

        var catAudio = new Category
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "Headphones & Audio",
            Slug = "headphones-audio",
            Description = "Wireless noise-canceling headphones, gaming headsets, and studio monitors.",
            ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&auto=format&fit=crop&q=80"
        };

        var catWearables = new Category
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Name = "Wearables & Smartwatches",
            Slug = "wearables-smartwatches",
            Description = "Smartwatches, fitness trackers, and rugged outdoor smart devices.",
            ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&auto=format&fit=crop&q=80"
        };

        await context.Categories.AddRangeAsync(catLaptops, catPhones, catAudio, catWearables);

        // 4. Seed Variant Types and Options
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

        // 5. Seed Products with rich attributes for AI retrieval and few-shot grounding
        var products = new List<Product>();

        // Product 1: MacBook Pro 14 M3 Pro (Programming / Development powerhouse)
        var macbookPro = new Product
        {
            Id = Guid.Parse("aaaaaaaa-1111-0000-0000-000000000001"),
            Name = "Apple MacBook Pro 14\" (M3 Pro, 2024)",
            Slug = "macbook-pro-14-m3-pro",
            Description = "The MacBook Pro 14-inch with M3 Pro chip delivers phenomenal performance for software developers, programmers, and data scientists. Features an 11-core CPU, 14-core GPU, and up to 18 hours of battery life. The Liquid Retina XDR display with ProMotion 120Hz provides crisp text rendering for long coding sessions and compile times are lightning fast.",
            ShortDescription = "Ultimate development laptop with M3 Pro chip, 18GB unified memory, and 120Hz Liquid Retina XDR display.",
            BasePrice = 1999.00m,
            Sku = "MBP-14-M3PRO",
            StockQuantity = 35,
            CategoryId = catLaptops.Id,
            Features = new List<string>
            {
                "Apple M3 Pro chip with 11-core CPU and 14-core GPU",
                "Ideal for software development, Docker, compiling code, and machine learning models",
                "14.2-inch Liquid Retina XDR display (3024x1964) with 120Hz ProMotion",
                "Up to 18 hours battery life with fast charging",
                "MagSafe 3, three Thunderbolt 4 / USB-C ports, HDMI, SDXC card slot, headphone jack",
                "Runs macOS Sonoma / Sequoia with Unix terminal and Homebrew support"
            },
            Specifications = new Dictionary<string, string>
            {
                ["Processor"] = "Apple M3 Pro (11-Core)",
                ["Memory"] = "18GB Unified Memory",
                ["Storage"] = "512GB NVMe SSD",
                ["Display"] = "14.2-inch Liquid Retina XDR (120Hz)",
                ["Battery Life"] = "Up to 18 hours",
                ["Weight"] = "3.5 lbs (1.61 kg)",
                ["Operating System"] = "macOS"
            },
            Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&auto=format&fit=crop&q=80", IsPrimary = true, AltText = "MacBook Pro 14 Front View" }
            }
        };

        // Product 2: Acer Aspire 5 (Budget laptop for programming & university under $1000)
        var acerAspire = new Product
        {
            Id = Guid.Parse("aaaaaaaa-1111-0000-0000-000000000002"),
            Name = "Acer Aspire 5 15.6\" FHD Laptop",
            Slug = "acer-aspire-5-15",
            Description = "An affordable and capable laptop engineered for university students, budget programming, web development, and daily productivity. Powered by an Intel Core i5 13th Gen processor with 16GB DDR4 RAM and a fast 512GB PCIe SSD. Features a comfortable backlit keyboard, full numpad, and an ergonomic elevated hinge for comfortable typing angles.",
            ShortDescription = "Affordable budget laptop under $700 with Intel Core i5, 16GB RAM, perfect for programming and university students.",
            BasePrice = 649.00m,
            Sku = "ACER-ASP5-I5",
            StockQuantity = 50,
            CategoryId = catLaptops.Id,
            Features = new List<string>
            {
                "Affordable price under $700 for students and beginner coders",
                "Intel Core i5-1335U 10-core processor",
                "16GB RAM enables smooth multitasking with VS Code, browser tabs, and local development servers",
                "512GB PCIe NVMe SSD for snappy boot and load times",
                "15.6-inch Full HD (1920 x 1080) IPS anti-glare display",
                "Wi-Fi 6E and Thunderbolt 4 connectivity"
            },
            Specifications = new Dictionary<string, string>
            {
                ["Processor"] = "Intel Core i5-1335U (10 Cores, up to 4.6 GHz)",
                ["Memory"] = "16GB DDR4",
                ["Storage"] = "512GB M.2 NVMe SSD",
                ["Display"] = "15.6\" Full HD IPS (1920x1080)",
                ["Battery Life"] = "Up to 9 hours",
                ["Weight"] = "3.88 lbs (1.76 kg)",
                ["Operating System"] = "Windows 11 Home"
            },
            Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=800&auto=format&fit=crop&q=80", IsPrimary = true, AltText = "Acer Aspire 5 Budget Laptop" }
            }
        };

        // Product 3: ASUS ROG Zephyrus G16 (High-end gaming laptop)
        var rogZephyrus = new Product
        {
            Id = Guid.Parse("aaaaaaaa-1111-0000-0000-000000000003"),
            Name = "ASUS ROG Zephyrus G16 (2024) Gaming Laptop",
            Slug = "asus-rog-zephyrus-g16",
            Description = "Precision-machined CNC aluminum gaming laptop boasting an Intel Core Ultra 9 processor and NVIDIA GeForce RTX 4080 GPU. Equipped with a stunning 16-inch 2.5K 240Hz OLED ROG Nebula display with 0.2ms response time. Perfect for competitive esports gaming, AAA ray-traced gaming, VR, and video rendering.",
            ShortDescription = "Top-tier gaming laptop with Core Ultra 9, NVIDIA RTX 4080, and 240Hz OLED ROG Nebula display.",
            BasePrice = 2299.00m,
            Sku = "ASUS-ROG-G16-RTX4080",
            StockQuantity = 20,
            CategoryId = catLaptops.Id,
            Features = new List<string>
            {
                "NVIDIA GeForce RTX 4080 12GB GDDR6 GPU with DLSS 3.5 and full ray tracing",
                "Intel Core Ultra 9 185H with dedicated AI NPU acceleration",
                "16-inch 2.5K (2560x1600) 240Hz OLED Display with G-Sync and 0.2ms response time",
                "Advanced ROG Intelligent Cooling with liquid metal and vapor chamber",
                "Sleek thin-and-light CNC unibody chassis weighing only 4.3 lbs"
            },
            Specifications = new Dictionary<string, string>
            {
                ["Processor"] = "Intel Core Ultra 9 185H (16 Cores)",
                ["Graphics"] = "NVIDIA GeForce RTX 4080 (12GB GDDR6)",
                ["Memory"] = "32GB LPDDR5X",
                ["Storage"] = "1TB PCIe Gen4 SSD",
                ["Display"] = "16\" 2.5K 240Hz OLED ROG Nebula",
                ["Weight"] = "4.3 lbs (1.95 kg)",
                ["Operating System"] = "Windows 11 Pro"
            },
            Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=800&auto=format&fit=crop&q=80", IsPrimary = true, AltText = "ASUS ROG Gaming Laptop" }
            }
        };

        // Product 4: Lenovo ThinkPad X1 Carbon Gen 12 (Business ultraportable)
        var thinkPad = new Product
        {
            Id = Guid.Parse("aaaaaaaa-1111-0000-0000-000000000004"),
            Name = "Lenovo ThinkPad X1 Carbon Gen 12",
            Slug = "lenovo-thinkpad-x1-carbon-gen12",
            Description = "The quintessential business ultrabook crafted with carbon fiber and magnesium alloy. Features an Intel Core Ultra 7 processor, legendary spill-resistant ThinkPad keyboard, TrackPoint, and enterprise security with dTPM 2.0 and fingerprint reader. Extremely durable and tested against military-grade MIL-STD 810H standards.",
            ShortDescription = "Legendary business ultrabook weighing just 2.4 lbs with Intel Core Ultra 7 and MIL-STD durability.",
            BasePrice = 1499.00m,
            Sku = "LEN-TP-X1C-G12",
            StockQuantity = 25,
            CategoryId = catLaptops.Id,
            Features = new List<string>
            {
                "Ultra-lightweight aerospace carbon fiber design weighing just 2.4 lbs (1.09 kg)",
                "Legendary ThinkPad ergonomic keyboard with TrackPoint and glass touchpad",
                "Intel Core Ultra 7 155H with Intel Evo certification",
                "14-inch 2.8K OLED display (400 nits, 100% DCI-P3)",
                "Enterprise security: Match-on-chip fingerprint reader, IR webcam with privacy shutter"
            },
            Specifications = new Dictionary<string, string>
            {
                ["Processor"] = "Intel Core Ultra 7 155H",
                ["Memory"] = "16GB LPDDR5X",
                ["Storage"] = "512GB PCIe 4.0 SSD",
                ["Display"] = "14\" 2.8K (2880x1800) OLED",
                ["Weight"] = "2.42 lbs (1.09 kg)",
                ["Operating System"] = "Windows 11 Pro"
            },
            Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://images.unsplash.com/photo-1541807084-5c52b6b3adef?w=800&auto=format&fit=crop&q=80", IsPrimary = true, AltText = "Lenovo ThinkPad X1 Carbon" }
            }
        };

        // Product 5: Samsung Galaxy S24 Ultra (Flagship smartphone with outstanding camera)
        var s24Ultra = new Product
        {
            Id = Guid.Parse("bbbbbbbb-2222-0000-0000-000000000001"),
            Name = "Samsung Galaxy S24 Ultra 5G",
            Slug = "samsung-galaxy-s24-ultra",
            Description = "The ultimate Android camera phone and AI powerhouse. Equipped with a revolutionary quad camera system headlined by a 200MP wide sensor, 50MP 5x periscope optical zoom, and 100x Space Zoom. Features an anti-reflective flat 6.8\" Dynamic AMOLED 2X 120Hz display, titanium frame, integrated S Pen stylus, and Galaxy AI photo editing tools.",
            ShortDescription = "Industry-leading smartphone camera with 200MP sensor, 100x Space Zoom, titanium frame, and built-in S Pen.",
            BasePrice = 1299.00m,
            Sku = "SAMSUNG-S24U-512",
            StockQuantity = 40,
            CategoryId = catPhones.Id,
            Features = new List<string>
            {
                "Pro-grade 200MP main camera captures extraordinary detail even in low light",
                "50MP periscope telephoto lens with 5x optical zoom and 100x digital Space Zoom",
                "Galaxy AI photo assistance: Generative edit, object eraser, instant slow-mo",
                "6.8-inch QHD+ Dynamic AMOLED 2X 1-120Hz display with Corning Gorilla Armor anti-reflective glass",
                "Built-in S Pen for drawing, note-taking, and camera remote control",
                "Snapdragon 8 Gen 3 for Galaxy with massive 5,000 mAh all-day battery"
            },
            Specifications = new Dictionary<string, string>
            {
                ["Camera System"] = "200MP Wide + 50MP 5x Telephoto + 10MP 3x Telephoto + 12MP Ultra-wide",
                ["Front Camera"] = "12MP Dual Pixel AF",
                ["Display"] = "6.8-inch Dynamic AMOLED 2X (3120x1440, 120Hz, 2600 nits)",
                ["Processor"] = "Snapdragon 8 Gen 3 for Galaxy",
                ["Battery"] = "5000 mAh (45W fast charging)",
                ["Water Resistance"] = "IP68 dust/water resistant"
            },
            Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?w=800&auto=format&fit=crop&q=80", IsPrimary = true, AltText = "Samsung Galaxy S24 Ultra" }
            }
        };

        // Product 6: Google Pixel 9 Pro (AI Computational Photography)
        var pixel9Pro = new Product
        {
            Id = Guid.Parse("bbbbbbbb-2222-0000-0000-000000000002"),
            Name = "Google Pixel 9 Pro",
            Slug = "google-pixel-9-pro",
            Description = "Google's premier smartphone engineered for computational photography, AI features, and clean pure Android experience. The 50MP triple camera system with Super Res Zoom, Night Sight Video, and Add Me AI photography captures true-to-life colors and stunning portrait shots in every lighting condition.",
            ShortDescription = "Top smartphone camera powered by Google Tensor G4 AI, Night Sight Video, and 50MP triple camera.",
            BasePrice = 999.00m,
            Sku = "GOOGLE-PIXEL-9PRO",
            StockQuantity = 30,
            CategoryId = catPhones.Id,
            Features = new List<string>
            {
                "Award-winning computational photography with natural skin tones via Real Tone",
                "50MP Octa PD main camera, 48MP ultra-wide with Macro Focus, 48MP 5x telephoto",
                "Night Sight for photos and 8K Video Boost",
                "Powered by Google Tensor G4 with 16GB RAM for on-device Gemini AI",
                "7 years of OS, security, and Pixel Drop updates"
            },
            Specifications = new Dictionary<string, string>
            {
                ["Camera System"] = "50MP Wide + 48MP Ultra-Wide + 48MP 5x Telephoto",
                ["Processor"] = "Google Tensor G4 with Titan M2 security",
                ["Display"] = "6.3-inch Super Actua LTPO OLED (1-120Hz, 3000 nits)",
                ["Memory"] = "16GB RAM",
                ["Battery"] = "4700 mAh",
                ["Operating System"] = "Android 15"
            },
            Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=800&auto=format&fit=crop&q=80", IsPrimary = true, AltText = "Google Pixel 9 Pro" }
            }
        };

        // Product 7: Samsung Galaxy A54 (Cheap phone with good camera under $450)
        var galaxyA54 = new Product
        {
            Id = Guid.Parse("bbbbbbbb-2222-0000-0000-000000000003"),
            Name = "Samsung Galaxy A54 5G",
            Slug = "samsung-galaxy-a54-5g",
            Description = "An affordable mid-range smartphone that punches well above its price tag, especially in camera quality. Features a 50MP main sensor with Optical Image Stabilization (OIS) and Nightography, a vibrant 6.4\" Super AMOLED 120Hz screen, and a durable glass back with IP67 water resistance.",
            ShortDescription = "Affordable budget smartphone under $450 with 50MP OIS camera, 120Hz AMOLED screen, and IP67 rating.",
            BasePrice = 399.00m,
            Sku = "SAMSUNG-A54-128",
            StockQuantity = 60,
            CategoryId = catPhones.Id,
            Features = new List<string>
            {
                "Budget-friendly price under $450 with flagship-inspired design",
                "50MP main camera with Optical Image Stabilization (OIS) for sharp blur-free shots",
                "Nightography low-light camera capabilities in an affordable phone",
                "6.4-inch FHD+ Super AMOLED 120Hz display",
                "5,000 mAh battery offering up to 2 days of typical usage",
                "IP67 dust and water resistance rating"
            },
            Specifications = new Dictionary<string, string>
            {
                ["Camera System"] = "50MP Main (OIS) + 12MP Ultra-wide + 5MP Macro",
                ["Front Camera"] = "32MP Selfie Camera",
                ["Display"] = "6.4-inch Super AMOLED 120Hz (1080x2340)",
                ["Battery"] = "5000 mAh",
                ["Storage"] = "128GB (MicroSD expandable up to 1TB)",
                ["Water Resistance"] = "IP67"
            },
            Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://images.unsplash.com/photo-1574944985070-8f3ebc6b79d2?w=800&auto=format&fit=crop&q=80", IsPrimary = true, AltText = "Samsung Galaxy A54 Budget Phone" }
            }
        };

        // Product 8: SteelSeries Arctis Nova Pro Wireless (Wireless headphones for gaming)
        var arctisNovaPro = new Product
        {
            Id = Guid.Parse("cccccccc-3333-0000-0000-000000000001"),
            Name = "SteelSeries Arctis Nova Pro Wireless Gaming Headset",
            Slug = "steelseries-arctis-nova-pro-wireless",
            Description = "The benchmark wireless gaming headset built specifically for PC and console gamers. Features ultra-low latency 2.4GHz wireless audio, simultaneous Bluetooth audio mixing, Active Noise Cancellation (ANC), and an Infinity Power System with hot-swappable dual batteries so you never need to pause gaming to recharge.",
            ShortDescription = "Elite wireless gaming headset with 2.4GHz lag-free wireless, ANC, dual hot-swap batteries, and Sonar spatial audio.",
            BasePrice = 349.99m,
            Sku = "SS-NOVA-PRO-WL",
            StockQuantity = 45,
            CategoryId = catAudio.Id,
            Features = new List<string>
            {
                "Engineered specifically for gaming with ultra-low latency 2.4GHz wireless connection",
                "Simultaneous 2.4GHz and Bluetooth to game while chatting on Discord or mobile",
                "Active Noise Cancellation (ANC) with 4-mic hybrid system isolates in-game sound",
                "Infinity Power System: Two hot-swappable batteries for unlimited continuous gaming",
                "ClearCast Gen 2 AI-powered bidirectional retractable noise-canceling microphone",
                "Sonar Audio Software Suite with 360-degree spatial audio and parametric EQ for footsteps"
            },
            Specifications = new Dictionary<string, string>
            {
                ["Audio Connectivity"] = "Lag-free 2.4GHz Wireless + Bluetooth 5.0 + 3.5mm wired",
                ["Battery Life"] = "Up to 44 hours (22 hours per battery, hot-swappable)",
                ["Drivers"] = "40mm Neodymium Drivers (Hi-Res certified)",
                ["Frequency Response"] = "10–40,000 Hz",
                ["Microphone"] = "ClearCast Gen 2 fully retractable with AI noise cancel",
                ["Compatibility"] = "PC, PS5, PS4, Nintendo Switch, Mobile, Mac"
            },
            Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://images.unsplash.com/photo-1546435770-a3e426bf472b?w=800&auto=format&fit=crop&q=80", IsPrimary = true, AltText = "SteelSeries Gaming Headset" }
            }
        };

        // Product 9: Sony WH-1000XM5 (Wireless headphones for music, office, travel)
        var sonyXm5 = new Product
        {
            Id = Guid.Parse("cccccccc-3333-0000-0000-000000000002"),
            Name = "Sony WH-1000XM5 Wireless Noise-Canceling Headphones",
            Slug = "sony-wh-1000xm5-wireless",
            Description = "Industry-leading noise-canceling headphones powered by two processors and 8 microphones. Delivers superlative Hi-Res wireless audio quality with LDAC, crystal clear hands-free calling with precise voice pickup, and exceptional 30-hour battery life with 3-minute quick charging for 3 hours of playback.",
            ShortDescription = "Industry-standard noise-canceling wireless headphones with 30-hour battery and Hi-Res LDAC sound.",
            BasePrice = 399.99m,
            Sku = "SONY-WH1000XM5-BLK",
            StockQuantity = 50,
            CategoryId = catAudio.Id,
            Features = new List<string>
            {
                "Two processors and 8 microphones control industry-leading active noise cancellation",
                "Ultra-comfortable lightweight design with soft fit leather",
                "Hi-Res Audio and Hi-Res Audio Wireless supported with Sony LDAC codec",
                "Up to 30 hours of continuous battery life on a single charge",
                "Multipoint Bluetooth pairing: Seamlessly switch between laptop and phone",
                "Speak-to-Chat automatically pauses playback when you start speaking"
            },
            Specifications = new Dictionary<string, string>
            {
                ["Battery Life"] = "Up to 30 hours (ANC On), up to 40 hours (ANC Off)",
                ["Fast Charging"] = "3 min charge = 3 hours playback",
                ["Bluetooth Version"] = "Bluetooth 5.2 (LDAC, AAC, SBC)",
                ["Weight"] = "250 grams (8.8 oz)",
                ["Noise Cancellation"] = "Integrated Processor V1 + HD QN1 processor"
            },
            Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&auto=format&fit=crop&q=80", IsPrimary = true, AltText = "Sony WH-1000XM5 Headphones" }
            }
        };

        // Product 10: Apple AirPods Pro 2 (Wireless in-ear earbuds)
        var airPodsPro = new Product
        {
            Id = Guid.Parse("cccccccc-3333-0000-0000-000000000003"),
            Name = "Apple AirPods Pro (2nd Generation, USB-C)",
            Slug = "apple-airpods-pro-2-usb-c",
            Description = "AirPods Pro 2 feature up to 2x more Active Noise Cancellation, Adaptive Audio that tailors noise control to your environment, and Transparency mode to hear the world around you. Includes personalized Spatial Audio with dynamic head tracking and a MagSafe Charging Case (USB-C) with speaker and lanyard loop.",
            ShortDescription = "Pro wireless in-ear earbuds with up to 2x more ANC, Adaptive Audio, and USB-C MagSafe case.",
            BasePrice = 249.00m,
            Sku = "APPLE-APP2-USBC",
            StockQuantity = 75,
            CategoryId = catAudio.Id,
            Features = new List<string>
            {
                "Apple H2 headphone chip powers computational audio and advanced ANC",
                "Up to 2x more active noise cancellation than previous generation",
                "Adaptive Audio automatically blends ANC and Transparency mode",
                "Personalized Spatial Audio with dynamic head tracking",
                "MagSafe Case (USB-C) with Precision Finding via U1 chip and built-in speaker",
                "Dust, sweat, and water resistant (IP54)"
            },
            Specifications = new Dictionary<string, string>
            {
                ["Chip"] = "Apple H2 headphone chip, Apple U1 in case",
                ["Battery Life"] = "Up to 6 hours listening (up to 30 hours with case)",
                ["Charging"] = "USB-C, MagSafe, Apple Watch charger, Qi-certified",
                ["Water Resistance"] = "IP54 for earbuds and case"
            },
            Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://images.unsplash.com/photo-1600294037681-c80b4cb5b434?w=800&auto=format&fit=crop&q=80", IsPrimary = true, AltText = "Apple AirPods Pro 2" }
            }
        };

        // Product 11: Apple Watch Ultra 2 (Rugged outdoor smartwatch)
        var appleWatchUltra = new Product
        {
            Id = Guid.Parse("dddddddd-4444-0000-0000-000000000001"),
            Name = "Apple Watch Ultra 2 (GPS + Cellular)",
            Slug = "apple-watch-ultra-2",
            Description = "The most capable and rugged Apple Watch ever. Crafted from lightweight aerospace-grade titanium with sapphire crystal front glass. Features a 3000-nit Always-On Retina display, dual-frequency precision GPS (L1 and L5), 36 hours of normal battery life (up to 72 hours in Low Power Mode), and 100m water resistance for diving.",
            ShortDescription = "Rugged titanium smartwatch with precision dual-frequency GPS, 3000-nit display, and 36-hour battery life.",
            BasePrice = 799.00m,
            Sku = "APPLE-AW-ULTRA2",
            StockQuantity = 20,
            CategoryId = catWearables.Id,
            Features = new List<string>
            {
                "49mm corrosion-resistant aerospace titanium case",
                "Brightest Apple display ever at 3000 nits for direct sunlight legibility",
                "Precision dual-frequency GPS (L1 and L5) for exact distance and route maps",
                "Up to 36 hours normal battery life, up to 72 hours in Low Power Mode",
                "Water resistant to 100m, certified for recreational scuba diving down to 40m",
                "Customizable Action button for workout starts, compass waypoints, and siren"
            },
            Specifications = new Dictionary<string, string>
            {
                ["Case Size"] = "49mm Titanium",
                ["Display"] = "3000-nit Always-On Retina OLED",
                ["Battery Life"] = "Up to 36 hours (72 hours Low Power)",
                ["Connectivity"] = "LTE Cellular + Wi-Fi + Bluetooth 5.3 + Dual GPS",
                ["Water Resistance"] = "100m / EN13319 dive certified"
            },
            Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&auto=format&fit=crop&q=80", IsPrimary = true, AltText = "Apple Watch Ultra 2" }
            }
        };

        products.AddRange(new[]
        {
            macbookPro, acerAspire, rogZephyrus, thinkPad,
            s24Ultra, pixel9Pro, galaxyA54,
            arctisNovaPro, sonyXm5, airPodsPro,
            appleWatchUltra
        });

        await context.Products.AddRangeAsync(products);

        // 6. Seed Product Variants for MacBook Pro and S24 Ultra
        var macbookVariant1 = new ProductVariant
        {
            Id = Guid.NewGuid(),
            Product = macbookPro,
            Sku = "MBP-14-M3P-18-512-SG",
            Price = 1999.00m,
            StockQuantity = 20
        };
        macbookVariant1.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = macbookVariant1, VariantOption = opt512Gb });
        macbookVariant1.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = macbookVariant1, VariantOption = optSpaceGray });

        var macbookVariant2 = new ProductVariant
        {
            Id = Guid.NewGuid(),
            Product = macbookPro,
            Sku = "MBP-14-M3P-36-1TB-SLV",
            Price = 2499.00m,
            StockQuantity = 15
        };
        macbookVariant2.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = macbookVariant2, VariantOption = opt1Tb });
        macbookVariant2.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = macbookVariant2, VariantOption = opt32GbRam });
        macbookVariant2.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = macbookVariant2, VariantOption = optSilver });

        var s24Variant1 = new ProductVariant
        {
            Id = Guid.NewGuid(),
            Product = s24Ultra,
            Sku = "S24U-256-BLK",
            Price = 1299.00m,
            StockQuantity = 25
        };
        s24Variant1.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = s24Variant1, VariantOption = opt256Gb });
        s24Variant1.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = s24Variant1, VariantOption = optBlack });

        var s24Variant2 = new ProductVariant
        {
            Id = Guid.NewGuid(),
            Product = s24Ultra,
            Sku = "S24U-512-TI",
            Price = 1419.00m,
            StockQuantity = 15
        };
        s24Variant2.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = s24Variant2, VariantOption = opt512Gb });
        s24Variant2.ProductVariantOptions.Add(new ProductVariantOption { ProductVariant = s24Variant2, VariantOption = optSilver });

        await context.ProductVariants.AddRangeAsync(macbookVariant1, macbookVariant2, s24Variant1, s24Variant2);

        await context.SaveChangesAsync();

        // 7. Seed product embeddings
        await TryGenerateEmbeddingsAsync();
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
}
