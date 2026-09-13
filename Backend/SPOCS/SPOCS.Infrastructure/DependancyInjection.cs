using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SPOCS.Application.Contracts;
using SPOCS.Application.Contracts.AI;
using SPOCS.Application.Contracts.Identity;
using SPOCS.Application.Contracts.Persistence;
using SPOCS.Application.Services;
using SPOCS.Domain.Entities;
using Pgvector.EntityFrameworkCore;
using SPOCS.Infrastructure.AI;
using SPOCS.Infrastructure.Data;
using SPOCS.Infrastructure.Identity;
using SPOCS.Infrastructure.Repositories;
using System.Text;


namespace SPOCS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddDatabase(services, configuration);
        AddIdentity(services);
        AddAuthentication(services, configuration);
        AddAuthorization(services);
        AddRepositories(services);
        AddApplicationServices(services, configuration);

        services.AddHttpContextAccessor();
        return services;
    }

    // ============================
    // Database
    // ============================
    private static void AddDatabase(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Handle postgresql:// URIs (common on Render)
        if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = ConvertPostgresUriToConnectionString(connectionString);
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("PostgresConnection connection string is not configured.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.UseVector();
                    npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                    npgsql.CommandTimeout(30);
                })
            .ConfigureWarnings(w =>
                w.Log(RelationalEventId.PendingModelChangesWarning))
            .EnableSensitiveDataLogging(false)
            .EnableDetailedErrors(false)
        );
    }

    private static string ConvertPostgresUriToConnectionString(string uri)
    {
        var databaseUri = new Uri(uri);
        var userInfo = databaseUri.UserInfo.Split(':');

        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = databaseUri.Host,
            Port = databaseUri.Port > 0 ? databaseUri.Port : 5432,
            Username = userInfo[0],
            Password = userInfo.Length > 1 ? userInfo[1] : string.Empty,
            Database = databaseUri.LocalPath.TrimStart('/'),
            SslMode = Npgsql.SslMode.Require
        };

        return builder.ToString();
    }

    // ============================
    // Identity
    // ============================
    private static void AddIdentity(IServiceCollection services)
    {
        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(opt =>
            {
                opt.Password.RequireDigit = false;
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequiredLength = 3;
                opt.Password.RequireLowercase = false;
                opt.Password.RequireUppercase = false;
                opt.User.RequireUniqueEmail = true;
                opt.SignIn.RequireConfirmedEmail = false;

            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
    }


    // ============================
    // JWT Authentication
    // ============================
    private static void AddAuthentication(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(opt =>
        {
            opt.SaveToken = true;
            opt.RequireHttpsMetadata = true; // Enforce HTTPS
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ValidateIssuerSigningKey = true,
                ValidAudience = configuration["JwtSettings:Audience"],
                ValidIssuer = configuration["JwtSettings:Issuer"],
                ClockSkew = TimeSpan.FromSeconds(30), // FIXED: Reduced from 10 to 30 for production
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["JwtSettings:Secret"]!)),
            };

            opt.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    var result = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        error = "You are not authorized to access this resource"
                    });
                    return context.Response.WriteAsync(result);
                }
            };
        });
    }

    // ============================
    // Authorization
    // ============================
    private static void AddAuthorization(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
        });
    }

    // ============================
    // Repositories
    // ============================
    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IProductEmbeddingRepository, ProductEmbeddingRepository>();
    }

    // ============================
    // Application Services
    // ============================
    private static void AddApplicationServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IDataSeeder, DataSeeder>();

        // AI Embedding & LLM
        services.AddHttpClient("OpenAI");
        services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();
        services.AddScoped<ILLMService, OpenAiLlmService>();
    }
}

