using MediatR;
using ArtAuction.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Aspire ServiceDefaults - OpenTelemetry, Serilog, Health Checks
builder.AddServiceDefaults();

// Lab #6: Add Memory Cache
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Limit cache size to prevent memory leaks
    options.CompactionPercentage = 0.25; // Remove 25% when size limit reached
});

// Lab #6: Add Redis Distributed Cache via Aspire
builder.AddRedisClient("redis"); // Aspire Redis client
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("redis");
    options.InstanceName = "ArtAuction:";
});

// Lab #6: Add gRPC Services
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaxReceiveMessageSize = 16 * 1024 * 1024; // 16 MB
    options.MaxSendMessageSize = 16 * 1024 * 1024; // 16 MB
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Art Auction API - Clean Architecture with gRPC", Version = "v1" });
});

// MediatR
builder.Services.AddMediatR(cfg => 
{
    cfg.RegisterServicesFromAssembly(typeof(ArtAuction.Application.Common.Interfaces.ICommand).Assembly);
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(ArtAuction.Application.Common.Interfaces.ICommand).Assembly);

// CORS (for development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Aspire ServiceDefaults middleware (logging, tracing)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Art Auction API v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Lab #6: Map gRPC Services
app.MapGrpcService<AuctionGrpcService>();

app.Run();
