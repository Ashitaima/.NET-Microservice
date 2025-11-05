using AuctionService.Dal;
using AuctionService.Dal.Interfaces;
using AuctionService.Bll.Interfaces;
using AuctionService.Bll.Services;
using AuctionService.Bll.Mapping;
using Microsoft.EntityFrameworkCore;
using Serilog;
using FluentValidation.AspNetCore;
using FluentValidation;
using Hellang.Middleware.ProblemDetails;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/auction-service-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting Auction Service API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Отримуємо connection string
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    // Configure DbContext with PostgreSQL
    builder.Services.AddDbContext<AuctionDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Реєструємо сервіси
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { 
            Title = "Art Auction API", 
            Version = "v1",
            Description = "RESTful API for Art Auction Platform with EF Core"
        });
    });

    // Реєструємо AutoMapper
    builder.Services.AddAutoMapper(typeof(MappingProfile));

    // Реєструємо FluentValidation
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // Configure ProblemDetails
    builder.Services.AddProblemDetails(options =>
    {
        options.IncludeExceptionDetails = (ctx, ex) => builder.Environment.IsDevelopment();
        
        options.Map<ValidationException>(ex => new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Validation Error",
            Status = StatusCodes.Status400BadRequest,
            Detail = ex.Message
        });
        
        options.Map<KeyNotFoundException>(ex => new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = ex.Message
        });
        
        options.Map<InvalidOperationException>(ex => new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Business Rule Violation",
            Status = StatusCodes.Status409Conflict,
            Detail = ex.Message
        });
    });

    // Реєструємо DAL та BLL
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IAuctionService, AuctionBllService>();
    builder.Services.AddScoped<IBidService, BidBllService>();

    var app = builder.Build();

    // Use ProblemDetails middleware
    app.UseProblemDetails();

    // Use Serilog request logging
    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        
        // Вимикаємо HTTPS redirect в Development
    }
    else
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
