using SPOCS.Application;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using SPOCS.Infrastructure;
using SPOCS.API.Middleware;
using SPOCS.Infrastructure.Data;
using SPOCS.Application.Contracts;

// ============================================================
// PostgreSQL DateTime Compatibility
// ============================================================

AppContext.SetSwitch(
    "Npgsql.EnableLegacyTimestampBehavior",
    true);


// ============================================================
// Builder
// ============================================================

var builder = WebApplication.CreateBuilder(args);


// ============================================================
// Serilog
// ============================================================

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "SPOCS.API")
        .Enrich.WithProperty(
            "Environment",
            context.HostingEnvironment.EnvironmentName)

        .WriteTo.Console(
            outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")

        .WriteTo.File(
            path: "logs/SPOCS-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] " +
                "[{Level:u3}] {Message:lj}{NewLine}{Exception}");
});


// ============================================================
// Controllers
// ============================================================

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles;

        options.JsonSerializerOptions.DefaultIgnoreCondition =
            JsonIgnoreCondition.WhenWritingNull;

        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });


// ============================================================
// Swagger
// ============================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SPOCS API",
        Version = "v1"
    });


    // JWT Bearer

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description =
            "Enter your Bearer token in the form 'Bearer {token}'"
    });


    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecuritySchemeReference(
                    "Bearer",
                    document)
            ] = new List<string>()
        });


    // XML comments

    var xmlFile =
        $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";

    var xmlPath =
        Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});


// ============================================================
// Application & Infrastructure
// ============================================================

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);


// ============================================================
// Authorization
// ============================================================

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "AdminOnly",
        policy =>
        {
            policy.RequireRole("Admin");
        });
});


// ============================================================
// Forwarded Headers
// Required when running behind Render / reverse proxy
// ============================================================

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownProxies.Clear();
});


// ============================================================
// CORS
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins =
            builder.Configuration
                .GetSection("AllowedOrigins")
                .Get<string[]>()
            ?? new[]
            {
                "https://spocs.netlify.app",
                "https://SPOCS-demo-dev.netlify.app",
                "http://localhost:5173",
                "http://localhost:3000",
                "https://localhost:5173",
                "https://localhost:3000",
                "https://localhost:5138",
                "http://localhost:5138"
            };

        if (builder.Environment.IsDevelopment())
        {
            policy
                .SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .WithExposedHeaders("Token-Expired");
        }
        else
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .WithMethods(
                    "GET",
                    "POST",
                    "PUT",
                    "DELETE",
                    "OPTIONS")
                .AllowCredentials()
                .WithExposedHeaders("Token-Expired");
        }
    });
});


// ============================================================
// Rate Limiting
// ============================================================

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;


    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(
            context =>
            {
                // --------------------------------------------
                // Authentication endpoints
                // 10 requests / minute
                // --------------------------------------------

                if (context.Request.Path.StartsWithSegments(
                        "/api/auth",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return RateLimitPartition
                        .GetFixedWindowLimiter(
                            context.Connection.RemoteIpAddress?
                                .ToString()
                            ?? "unknown",

                            _ => new FixedWindowRateLimiterOptions
                            {
                                AutoReplenishment = true,
                                PermitLimit = 10,
                                QueueLimit = 0,
                                Window = TimeSpan.FromMinutes(1)
                            });
                }


                // --------------------------------------------
                // Other endpoints
                // 100 requests / minute
                // --------------------------------------------

                return RateLimitPartition
                    .GetFixedWindowLimiter(
                        context.User.Identity?.Name
                            ?? context.Connection.RemoteIpAddress?
                                .ToString()
                            ?? "unknown",

                        _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        });
            });


    options.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;

        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                error =
                    "Too many requests. Please try again later."
            });
    };
});


// ============================================================
// Response Compression
// ============================================================

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});


// ============================================================
// Build Application
// ============================================================

var app = builder.Build();


// ============================================================
// Middleware Pipeline
// ============================================================

// Forwarded headers must be early in the pipeline
app.UseForwardedHeaders();

// Global exception handler
app.UseGlobalExceptionHandler(
    app.Environment);

// CORS must be early before Redirection, Security Headers, and Rate Limiting
app.UseCors();

// Security headers
app.UseMiddleware<SecurityHeadersMiddleware>();

// Serilog request logging
app.UseSerilogRequestLogging();

// Swagger
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "SPOCS API v1");

    options.DocumentTitle =
        "SPOCS API Documentation";
});

// Production Security
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Response Compression
app.UseResponseCompression();

// Rate Limiting
app.UseRateLimiter();


// ============================================================
// Database Migration & Identity Seeding
// ============================================================

using (var scope = app.Services.CreateScope())
{
    try
    {
        Log.Information(
            "Starting database initialization...");


        // ----------------------------------------------------
        // Resolve DbContext
        // ----------------------------------------------------

        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();


        Log.Information(
            "ApplicationDbContext resolved.");


        // ----------------------------------------------------
        // Check migrations
        // ----------------------------------------------------

        Log.Information(
            "Checking pending migrations...");


        var pendingMigrations =
            await context.Database
                .GetPendingMigrationsAsync();


        Log.Information(
            "Found {Count} pending migrations.",
            pendingMigrations.Count());


        // ----------------------------------------------------
        // Apply migrations
        // ----------------------------------------------------

        if (pendingMigrations.Any())
        {
            Log.Information(
                "Applying pending migrations...");


            await context.Database.MigrateAsync();


            Log.Information(
                "Database migrations completed.");
        }
        else
        {
            Log.Information(
                "Database is already up to date.");
        }

        // ----------------------------------------------------
        // Content Seeder
        // ----------------------------------------------------

        Log.Information(
            "Starting content seeding...");

        var contentSeeder =
            scope.ServiceProvider
                .GetRequiredService<IDataSeeder>();

        Log.Information(
            "Data seeder resolved.");

        await contentSeeder.SeedAsync();

        Log.Information(
            "Data seeding completed.");

        Log.Information(
            "Database initialization completed.");
    }
    catch (Exception ex)
    {
        Log.Fatal(
            ex,
            "Database initialization failed.");

        throw;
    }
}


// ============================================================
// Authentication & Authorization
// ============================================================

app.UseAuthentication();

app.UseAuthorization();


// ============================================================
// Controllers
// ============================================================

app.MapControllers();

// Health check endpoints for Render and monitoring
app.MapGet("/", () => Results.Ok(new { status = "Healthy", service = "SPOCS API", time = DateTime.UtcNow }));
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));


// ============================================================
// Run Application
// ============================================================

try
{
    Log.Information(
        "Starting SPOCS API");


    // Render provides PORT as an environment variable.
    // Local development falls back to launchSettings.json.

    var port = Environment.GetEnvironmentVariable("PORT");

    if (!string.IsNullOrEmpty(port))
    {
        Log.Information(
            "Listening on port {Port}",
            port);

        app.Run(
            $"http://0.0.0.0:{port}");
    }
    else
    {
        Log.Information(
            "Listening on ports configured in launchSettings.json");

        app.Run();
    }
}
catch (Exception ex)
{
    Log.Fatal(
        ex,
        "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}