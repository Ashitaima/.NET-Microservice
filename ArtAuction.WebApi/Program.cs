using MediatR;

var builder = WebApplication.CreateBuilder(args);

// Aspire ServiceDefaults - OpenTelemetry, Serilog, Health Checks
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Art Auction API - Clean Architecture", Version = "v1" });
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

app.Run();
