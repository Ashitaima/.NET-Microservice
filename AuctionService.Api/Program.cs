using AuctionService.Dal;
using AuctionService.Dal.Interfaces;
using AuctionService.Bll.Interfaces;
using AuctionService.Bll.Services;
using AuctionService.Bll.Mapping;

var builder = WebApplication.CreateBuilder(args);

// Отримуємо connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Реєструємо сервіси
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Art Auction API", Version = "v1" });
});

// Реєструємо AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Реєструємо DAL та BLL
builder.Services.AddScoped<IUnitOfWork>(sp => new UnitOfWork(connectionString));
builder.Services.AddScoped<IAuctionService, AuctionBllService>();
builder.Services.AddScoped<IBidService, BidBllService>();

var app = builder.Build();

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
