using ArtAuction.Aggregator.Services;

var builder = WebApplication.CreateBuilder(args);

// Aspire ServiceDefaults
builder.AddServiceDefaults();

// Lab #6: gRPC Clients with service discovery
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
