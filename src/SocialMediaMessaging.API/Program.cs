using Serilog;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SocialMediaMessaging.Infrastructure.Services;
using SocialMediaMessaging.Infrastructure.Adapters;
using System.Threading.RateLimiting;

// Serilog konfigürasyonu
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/socialmedia-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog'u host builder'a ekle
    builder.Host.UseSerilog();

    // Configuration
    builder.Services.Configure<SocialMediaConfiguration>(
        builder.Configuration.GetSection("SocialMediaMessaging"));

    // Controllers
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.WriteIndented = true;
        });

    // Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Social Media Messaging API",
            Version = "v1",
            Description = "Çoklu sosyal medya platformlarına mesaj gönderme API'si",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "API Desteği",
                Email = "support@example.com"
            }
        });

        // XML documentation
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // Memory Cache
    builder.Services.AddMemoryCache();

    // HttpClient
    builder.Services.AddHttpClient();

    // Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1)
                }));

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            await context.HttpContext.Response.WriteAsync("Rate limit aşıldı. Lütfen daha sonra tekrar deneyin.", token);
        };
    });

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultPolicy", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Platform Adapters
    builder.Services.AddScoped<TelegramAdapter>();
    builder.Services.AddScoped<TwitterAdapter>();
    builder.Services.AddScoped<DiscordAdapter>();

    // Services
    builder.Services.AddScoped<IPlatformAdapterFactory, PlatformAdapterFactory>();
    builder.Services.AddScoped<IMessageService, MessageService>();

    // Mock implementations (production'da gerçek implementasyonlar kullanılmalı)
    builder.Services.AddScoped<IMessageValidator, MockMessageValidator>();
    builder.Services.AddScoped<IRateLimitingService, MockRateLimitingService>();
    builder.Services.AddScoped<IRetryService, MockRetryService>();

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddCheck("api", () => HealthCheckResult.Healthy("API is running"))
        .AddCheck<PlatformHealthCheck>("platforms");

    var app = builder.Build();

    // Middleware pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Social Media Messaging API v1");
            c.RoutePrefix = string.Empty; // Swagger UI'yi root'ta göster
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("DefaultPolicy");
    app.UseRateLimiter();

    // Global Exception Handler
    app.UseExceptionHandler("/error");
    app.Map("/error", (HttpContext context) =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (feature?.Error != null)
        {
            Log.Error(feature.Error, "Unhandled exception occurred");
        }

        return Results.Problem(
            title: "Bir hata oluştu",
            detail: "Sunucu isteği işlerken bir hata ile karşılaştı.",
            statusCode: 500);
    });

    app.UseRouting();

    app.MapControllers();

    // Health Check endpoint
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(x => new
                {
                    name = x.Key,
                    status = x.Value.Status.ToString(),
                    description = x.Value.Description,
                    duration = x.Value.Duration.TotalMilliseconds
                }),
                totalDuration = report.TotalDuration.TotalMilliseconds
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    });

    // Minimal API endpoints
    app.MapGet("/", () => Results.Redirect("/swagger"));

    app.MapGet("/info", () => new
    {
        name = "Social Media Messaging API",
        version = "1.0.0",
        description = "Çoklu sosyal medya platformlarına mesaj gönderme API'si",
        author = "SocialMediaMessaging Team",
        uptime = DateTime.UtcNow.Subtract(System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()),
        environment = app.Environment.EnvironmentName
    });

    // Platform capabilities endpoint
    app.MapGet("/platforms", async (IPlatformAdapterFactory factory) =>
    {
        var capabilities = ((PlatformAdapterFactory)factory).GetAllPlatformCapabilities();
        return Results.Ok(capabilities);
    });

    Log.Information("Social Media Messaging API başlatılıyor...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}

// Mock Services (Production'da gerçek implementasyonlar olmalı)

public class MockMessageValidator : IMessageValidator
{
    public Task<SocialMediaMessaging.Core.Interfaces.ValidationResult> ValidateAsync(MessageRequest request, CancellationToken cancellationToken = default)
    {
        var result = new SocialMediaMessaging.Core.Interfaces.ValidationResult { IsValid = true };
        
        if (string.IsNullOrWhiteSpace(request.Content))
            result.AddError("Content is required");
            
        if (request.Platforms == null || !request.Platforms.Any())
            result.AddError("At least one platform is required");
            
        return Task.FromResult(result);
    }

    public Task<SocialMediaMessaging.Core.Interfaces.ValidationResult> ValidateForPlatformAsync(MessageRequest request, string platform, CancellationToken cancellationToken = default)
    {
        return ValidateAsync(request, cancellationToken);
    }

    public Task<SocialMediaMessaging.Core.Interfaces.ValidationResult> ValidateAttachmentsAsync(List<Attachment> attachments, string platform, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SocialMediaMessaging.Core.Interfaces.ValidationResult.Success());
    }

    public Task<SocialMediaMessaging.Core.Interfaces.ValidationResult> ValidateContentAsync(string content, MessageType type, string platform, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SocialMediaMessaging.Core.Interfaces.ValidationResult.Success());
    }

    public Task<List<SocialMediaMessaging.Core.Interfaces.ValidationResult>> ValidateBulkAsync(List<MessageRequest> requests, CancellationToken cancellationToken = default)
    {
        var results = requests.Select(r => ValidateAsync(r, cancellationToken).Result).ToList();
        return Task.FromResult(results);
    }

    public Task<SocialMediaMessaging.Core.Interfaces.ValidationResult> ValidateScheduledMessageAsync(MessageRequest request, CancellationToken cancellationToken = default)
    {
        return ValidateAsync(request, cancellationToken);
    }

    public Task<SocialMediaMessaging.Core.Interfaces.ValidationResult> ValidateAgainstConstraintsAsync(MessageRequest request, PlatformConstraints constraints, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SocialMediaMessaging.Core.Interfaces.ValidationResult.Success());
    }
}

public class MockRateLimitingService : IRateLimitingService
{
    public Task<bool> IsAllowedAsync(string platform, string identifier) => Task.FromResult(true);
    public Task RecordRequestAsync(string platform, string identifier) => Task.CompletedTask;
    public Task<RateLimitStatus> GetRateLimitStatusAsync(string platform, string identifier) =>
        Task.FromResult(new RateLimitStatus { IsAllowed = true, RemainingRequests = 100, TotalLimit = 100, Platform = platform, Identifier = identifier });
    public Task ResetRateLimitAsync(string platform, string identifier) => Task.CompletedTask;
    public Task ClearAllRateLimitsAsync() => Task.CompletedTask;
    public Task UpdateRateLimitConfigurationAsync(string platform, int limit, TimeSpan window) => Task.CompletedTask;
    public Task<bool> CanProcessAsync(string identifier, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task IncrementAsync(string identifier, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public class MockRetryService : IRetryService
{
    public Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxAttempts, CancellationToken cancellationToken = default)
    {
        return operation();
    }

    public Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxAttempts, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        return operation();
    }

    public Task<T> ExecuteWithExponentialBackoffAsync<T>(Func<Task<T>> operation, int maxAttempts, TimeSpan baseDelay, TimeSpan maxDelay, CancellationToken cancellationToken = default)
    {
        return operation();
    }

    public Task<T> ExecuteWithConditionalRetryAsync<T>(Func<Task<T>> operation, int maxAttempts, Func<Exception, bool> shouldRetry, CancellationToken cancellationToken = default)
    {
        return operation();
    }

    public void ConfigureRetryPolicy(RetryPolicy policy)
    {
        // Mock implementation - do nothing
    }

    public RetryStatistics GetRetryStatistics()
    {
        return new RetryStatistics
        {
            TotalAttempts = 0,
            SuccessfulOperations = 0,
            FailedOperations = 0,
            AverageAttempts = 0,
            LastUpdated = DateTime.UtcNow
        };
    }
}

public class PlatformHealthCheck : IHealthCheck
{
    private readonly IPlatformAdapterFactory _platformAdapterFactory;

    public PlatformHealthCheck(IPlatformAdapterFactory platformAdapterFactory)
    {
        _platformAdapterFactory = platformAdapterFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = (PlatformAdapterFactory)_platformAdapterFactory;
            var healthResults = await factory.PerformHealthCheckAsync();
            
            var healthyCount = healthResults.Count(r => r.Value);
            var totalCount = healthResults.Count;
            
            if (healthyCount == totalCount)
            {
                return HealthCheckResult.Healthy($"Tüm platformlar sağlıklı ({healthyCount}/{totalCount})");
            }
            else if (healthyCount > 0)
            {
                return HealthCheckResult.Degraded($"Bazı platformlar sağlıklı değil ({healthyCount}/{totalCount})");
            }
            else
            {
                return HealthCheckResult.Unhealthy($"Hiçbir platform sağlıklı değil (0/{totalCount})");
            }
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Platform sağlık kontrolü başarısız: {ex.Message}");
        }
    }
} 

// Integration testleri için public partial class
public partial class Program { } 