using ArtAuction.Aggregator.Services;
using ArtAuction.Aggregator.HealthChecks;
using ArtAuction.Aggregator.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Aspire ServiceDefaults
builder.AddServiceDefaults();

// Lab #7: Custom health checks (ServiceDefaults already adds "self" check)
builder.Services.AddHealthChecks()
    .AddCheck<AuctionServiceHealthCheck>(
        "auction-service-grpc",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "grpc", "downstream", "ready" });

// Lab #6: gRPC Clients with service discovery
// Lab #7: Added Standard Resilience Handler (retry + circuit breaker + timeout)
builder.Services.AddGrpcClient<ArtAuction.Aggregator.Protos.AuctionService.AuctionServiceClient>(options =>
{
    // Service discovery - Aspire automatically resolves "https+http://artauction-webapi"
    options.Address = new Uri("https+http://artauction-webapi");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    // Configure for HTTP/2
    return new SocketsHttpHandler
    {
        PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
        KeepAlivePingDelay = TimeSpan.FromSeconds(60),
        KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
        EnableMultipleHttp2Connections = true
    };
})
.AddStandardResilienceHandler(resilience =>
{
    // Custom retry policy with exponential backoff and jitter
    resilience.Retry = new Microsoft.Extensions.Http.Resilience.HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = Polly.DelayBackoffType.Exponential,
        UseJitter = true // Prevents thundering herd
    };
    
    // Custom circuit breaker for auction service
    resilience.CircuitBreaker = new Microsoft.Extensions.Http.Resilience.HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5, // Open circuit at 50% failure rate
        MinimumThroughput = 50, // Minimum 50 requests before calculating ratio
        BreakDuration = TimeSpan.FromSeconds(30),
        SamplingDuration = TimeSpan.FromSeconds(60)
    };
    
    // Total request timeout (including retries)
    resilience.TotalRequestTimeout = new Microsoft.Extensions.Http.Resilience.HttpTimeoutStrategyOptions
    {
        Timeout = TimeSpan.FromSeconds(10)
    };
});

// Lab #6: Distributed cache for aggregated results
builder.AddRedisClient("redis");
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("redis");
    options.InstanceName = "Aggregator:";
});

// Register Aggregator Service
builder.Services.AddScoped<AuctionAggregatorService>();

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Auction Aggregator API - gRPC Client", Version = "v1" });
});

var app = builder.Build();

// Lab #7: Health check endpoints - liveness and readiness
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    AllowCachingResponses = false
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    AllowCachingResponses = false,
    ResponseWriter = HealthCheckExtensions.WriteHealthCheckResponse
});

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    AllowCachingResponses = false,
    ResponseWriter = HealthCheckExtensions.WriteHealthCheckResponse
});

// Aspire middleware
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Aggregator API v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
