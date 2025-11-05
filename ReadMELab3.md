# Лабораторна робота №3: ASP.NET Core Web API з Entity Framework Core

## Тема
**ASP.NET. Тришарова архітектура на базі Entity Framework Core**

## Мета роботи
Ознайомлення із сучасним підходом до розробки RESTful Web API з використанням:
- Entity Framework Core (Code-First)
- Реалізація шаблонів Repository/Unit of Work
- Використання DTO та AutoMapper
- Побудова механізмів валідації та централізованої обробки помилок
- Впровадження пагінації, фільтрації та логування
- Робота з міграціями та початковим наповненням бази даних

## Технології та інструменти

### Основні технології
- **.NET 8.0** - платформа розробки
- **ASP.NET Core Web API** - фреймворк для RESTful API
- **Entity Framework Core 8.0** - ORM для роботи з базою даних
- **PostgreSQL** - реляційна база даних
- **AutoMapper** - маппінг між Entity та DTO
- **Serilog** - структуроване логування
- **Swagger/Swashbuckle** - документація API

### Додаткові бібліотеки
- **FluentValidation** - валідація моделей
- **Hellang.Middleware.ProblemDetails** - обробка помилок
- **Ardalis.Specification** - pattern Specification
- **Npgsql.EntityFrameworkCore.PostgreSQL** - провайдер PostgreSQL

## Структура проєкту

```
ProjectAuction/
├── AuctionService.Domain/          # Domain layer (Entities)
│   └── Entities/
│       ├── Auction.cs              # Сутність аукціону
│       ├── Bid.cs                  # Сутність ставки
│       ├── User.cs                 # Сутність користувача
│       ├── Payment.cs              # Сутність платежу
│       ├── AuctionStatus.cs        # Enum статусів аукціону
│       └── TransactionStatus.cs    # Enum статусів транзакцій
│
├── AuctionService.Dal/             # Data Access Layer
│   ├── AuctionDbContext.cs         # DbContext з конфігурацією та Seeding
│   ├── UnitOfWork.cs               # Реалізація Unit of Work
│   ├── Configurations/             # Fluent API конфігурації
│   │   ├── AuctionConfiguration.cs
│   │   ├── BidConfiguration.cs
│   │   ├── UserConfiguration.cs
│   │   └── PaymentConfiguration.cs
│   ├── Interfaces/
│   │   ├── IRepository.cs          # Generic Repository інтерфейс
│   │   ├── IAuctionRepository.cs
│   │   ├── IBidRepository.cs
│   │   ├── IUserRepository.cs
│   │   ├── IPaymentRepository.cs
│   │   └── IUnitOfWork.cs
│   ├── Repositories/
│   │   ├── Repository.cs           # Generic Repository реалізація
│   │   ├── AuctionRepository.cs    # Специфічні методи для аукціонів
│   │   ├── BidRepository.cs        # Специфічні методи для ставок
│   │   ├── UserRepository.cs       # Специфічні методи для користувачів
│   │   └── PaymentRepository.cs    # Специфічні методи для платежів
│   └── Migrations/                 # EF Core міграції
│
├── AuctionService.Bll/             # Business Logic Layer
│   ├── DTOs/
│   │   ├── AuctionDto.cs
│   │   ├── BidDto.cs
│   │   ├── UserDto.cs
│   │   └── PaymentDto.cs
│   ├── Interfaces/
│   │   ├── IAuctionService.cs
│   │   └── IBidService.cs
│   ├── Services/
│   │   ├── AuctionBllService.cs    # Бізнес-логіка аукціонів
│   │   └── BidBllService.cs        # Бізнес-логіка ставок
│   └── Mapping/
│       └── MappingProfile.cs       # AutoMapper профіль
│
└── AuctionService.Api/             # Presentation Layer (Web API)
    ├── Controllers/
    │   ├── AuctionsController.cs
    │   └── BidsController.cs
    ├── Program.cs                  # Конфігурація додатку
    ├── appsettings.json            # Налаштування
    └── logs/                       # Файли логів Serilog
```

## Частина 1: Entity Framework Core (Code-First)

### 1.1 Доменні моделі (Entities)

Всі entity класи знаходяться в проєкті `AuctionService.Domain` та включають:

#### Auction (Аукціон)
```csharp
public class Auction
{
    public long AuctionId { get; set; }
    public long ArtworkId { get; set; }
    public string ArtworkName { get; set; }
    public long SellerUserId { get; set; }
    public decimal StartPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AuctionStatus Status { get; set; }
    public long? WinnerUserId { get; set; }
    
    // Navigation properties
    public User? Seller { get; set; }
    public User? Winner { get; set; }
    public ICollection<Bid> Bids { get; set; }
    public Payment? Payment { get; set; }
}
```

#### Bid (Ставка)
```csharp
public class Bid
{
    public long BidId { get; set; }
    public long AuctionId { get; set; }
    public long UserId { get; set; }
    public decimal BidAmount { get; set; }
    public DateTime Timestamp { get; set; }
    
    // Navigation properties
    public Auction Auction { get; set; }
    public User User { get; set; }
}
```

### 1.2 Fluent API конфігурація

Кожна сутність має окрему конфігурацію через `IEntityTypeConfiguration<T>`:

**Приклад AuctionConfiguration:**
```csharp
public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.ToTable("Auctions");
        builder.HasKey(a => a.AuctionId);
        
        builder.Property(a => a.ArtworkName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(a => a.StartPrice).HasPrecision(18, 2);
        builder.Property(a => a.CurrentPrice).HasPrecision(18, 2);
        
        // Relationships
        builder.HasOne(a => a.Seller)
            .WithMany(u => u.SellerAuctions)
            .HasForeignKey(a => a.SellerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.EndTime);
    }
}
```

### 1.3 DbContext та Data Seeding

**AuctionDbContext** включає:
- Всі DbSet для сутностей
- Застосування конфігурацій через `ApplyConfiguration`
- Початкове наповнення даних через `HasData`

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Apply configurations
    modelBuilder.ApplyConfiguration(new AuctionConfiguration());
    modelBuilder.ApplyConfiguration(new BidConfiguration());
    modelBuilder.ApplyConfiguration(new UserConfiguration());
    modelBuilder.ApplyConfiguration(new PaymentConfiguration());
    
    // Data seeding
    SeedData(modelBuilder);
}
```

**Seeding включає:**
- 5 користувачів з різними балансами
- 5 аукціонів (активні, завершені, очікують)
- 7 ставок на різні аукціони
- 2 платежі для завершених аукціонів

### 1.4 Міграції

Створення та застосування міграцій:

```bash
# Створення міграції
dotnet ef migrations add InitialCreate --project AuctionService.Dal --startup-project AuctionService.Api

# Застосування міграції
dotnet ef database update --project AuctionService.Dal --startup-project AuctionService.Api

# Перегляд SQL скрипту
dotnet ef migrations script --project AuctionService.Dal
```

## Частина 2: Repository Pattern та Unit of Work

### 2.1 Generic Repository

**IRepository<T>** надає базові CRUD операції:
```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(long id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
}
```

### 2.2 Специфічні репозиторії

#### AuctionRepository - демонструє Eager Loading
```csharp
public async Task<Auction?> GetAuctionWithDetailsAsync(long auctionId)
{
    return await _dbSet
        .Include(a => a.Seller)              // Eager Loading
        .Include(a => a.Winner)
        .Include(a => a.Bids)
            .ThenInclude(b => b.User)        // ThenInclude для вкладених даних
        .Include(a => a.Payment)
        .FirstOrDefaultAsync(a => a.AuctionId == auctionId);
}
```

#### UserRepository - демонструє Explicit Loading
```csharp
public async Task<User?> GetUserWithBidsAsync(long userId)
{
    var user = await _dbSet.FindAsync(userId);
    
    if (user != null)
    {
        // Explicit Loading
        await _context.Entry(user)
            .Collection(u => u.Bids)
            .Query()
            .Include(b => b.Auction)
            .LoadAsync();
    }
    
    return user;
}
```

#### BidRepository - демонструє складні LINQ запити
```csharp
public async Task<IEnumerable<Bid>> GetBidsWithUsersAsync(long auctionId)
{
    return await _dbSet
        .Where(b => b.AuctionId == auctionId)
        .Include(b => b.User)
        .Include(b => b.Auction)
        .OrderByDescending(b => b.BidAmount)
        .ToListAsync();
}
```

### 2.3 Unit of Work

**UnitOfWork** координує роботу репозиторіїв та транзакцій:

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly AuctionDbContext _context;
    
    public IAuctionRepository Auctions { get; }
    public IBidRepository Bids { get; }
    public IUserRepository Users { get; }
    public IPaymentRepository Payments { get; }
    
    public async Task<int> SaveChangesAsync() 
        => await _context.SaveChangesAsync();
    
    public async Task BeginTransactionAsync() 
        => await _context.Database.BeginTransactionAsync();
    
    public async Task CommitAsync() 
        => await _context.Database.CommitTransactionAsync();
    
    public async Task RollbackAsync() 
        => await _context.Database.RollbackTransactionAsync();
}
```

**Приклад використання транзакцій у BLL:**
```csharp
public async Task<bool> PlaceBidAsync(PlaceBidDto dto)
{
    await _unitOfWork.BeginTransactionAsync();
    try
    {
        var bid = new Bid { /* ... */ };
        await _unitOfWork.Bids.AddAsync(bid);
        
        var auction = await _unitOfWork.Auctions.GetByIdAsync(dto.AuctionId);
        auction.CurrentPrice = dto.BidAmount;
        _unitOfWork.Auctions.Update(auction);
        
        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.CommitAsync();
        return true;
    }
    catch
    {
        await _unitOfWork.RollbackAsync();
        throw;
    }
}
```

## Частина 3: DTO, AutoMapper, Web API

### 3.1 Data Transfer Objects (DTO)

DTO класи використовуються для передачі даних між шарами:

```csharp
public class AuctionDto
{
    public long AuctionId { get; set; }
    public string ArtworkName { get; set; }
    public decimal StartPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Status { get; set; }
}

public class CreateAuctionDto
{
    public long ArtworkId { get; set; }
    public string ArtworkName { get; set; }
    public decimal StartPrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
```

### 3.2 AutoMapper Configuration

```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Auction, AuctionDto>()
            .ForMember(dest => dest.Status, 
                opt => opt.MapFrom(src => (int)src.Status));
        
        CreateMap<CreateAuctionDto, Auction>()
            .ForMember(dest => dest.Status, 
                opt => opt.MapFrom(_ => AuctionStatus.Pending))
            .ForMember(dest => dest.CurrentPrice, 
                opt => opt.MapFrom(src => src.StartPrice));
    }
}
```

### 3.3 Business Logic Services

**AuctionBllService** інкапсулює бізнес-логіку:

```csharp
public class AuctionBllService : IAuctionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    
    public async Task<long> CreateAsync(CreateAuctionDto dto)
    {
        // Валідація
        if (dto.StartPrice <= 0)
            throw new ArgumentException("Start price must be greater than 0");
        
        if (dto.EndTime <= dto.StartTime)
            throw new ArgumentException("End time must be after start time");
        
        // Створення та збереження
        var auction = _mapper.Map<Auction>(dto);
        await _unitOfWork.Auctions.AddAsync(auction);
        await _unitOfWork.SaveChangesAsync();
        
        return auction.AuctionId;
    }
}
```

### 3.4 API Controllers

**Асинхронні контролери з коректними HTTP кодами:**

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionService _auctionService;
    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AuctionDto>>> GetAll()
    {
        var auctions = await _auctionService.GetAllAsync();
        return Ok(auctions);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuctionDto>> GetById(long id)
    {
        var auction = await _auctionService.GetByIdAsync(id);
        if (auction == null)
            return NotFound();
        return Ok(auction);
    }
    
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<long>> Create(CreateAuctionDto dto)
    {
        var id = await _auctionService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }
}
```

## Частина 4: Додаткові можливості

### 4.1 Serilog - Структуроване логування

**Конфігурація в Program.cs:**
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/auction-service-.txt", 
        rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();
app.UseSerilogRequestLogging();
```

### 4.2 Централізована обробка помилок

**ProblemDetails Middleware:**
```csharp
builder.Services.AddProblemDetails(options =>
{
    options.IncludeExceptionDetails = (ctx, ex) => 
        builder.Environment.IsDevelopment();
    
    options.Map<ValidationException>(ex => new ProblemDetails
    {
        Title = "Validation Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = ex.Message
    });
    
    options.Map<KeyNotFoundException>(ex => new ProblemDetails
    {
        Title = "Not Found",
        Status = StatusCodes.Status404NotFound,
        Detail = ex.Message
    });
});

app.UseProblemDetails();
```

### 4.3 FluentValidation (готово до використання)

Пакет встановлено, можна додати валідатори:

```csharp
public class CreateAuctionDtoValidator : AbstractValidator<CreateAuctionDto>
{
    public CreateAuctionDtoValidator()
    {
        RuleFor(x => x.ArtworkName)
            .NotEmpty()
            .MaximumLength(200);
        
        RuleFor(x => x.StartPrice)
            .GreaterThan(0);
        
        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime);
    }
}
```

### 4.4 Swagger/Swashbuckle

API документація доступна за адресою: `http://localhost:5001/swagger`

Конфігурація включає:
- Опис API
- Версіонування
- Приклади запитів/відповідей

## Налаштування та запуск

### Передумови
- .NET 8.0 SDK
- PostgreSQL Server
- Visual Studio 2022 або VS Code

### Connection String

Файл `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=artauctiondb;Username=postgres;Password=your_password;Pooling=true;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Кроки запуску

1. **Відновлення пакетів:**
```bash
dotnet restore
```

2. **Створення міграції (якщо потрібно):**
```bash
cd AuctionService.Api
dotnet ef migrations add YourMigrationName --project ..\AuctionService.Dal\AuctionService.Dal.csproj
```

3. **Застосування міграції:**
```bash
dotnet ef database update --project ..\AuctionService.Dal\AuctionService.Dal.csproj
```

4. **Запуск додатку:**
```bash
dotnet run
```

5. **Відкрити Swagger:**
```
http://localhost:5001/swagger
```

## Тестування API

### Приклади запитів через Swagger або curl:

**1. Отримати всі аукціони:**
```bash
GET http://localhost:5001/api/auctions
```

**2. Отримати активні аукціони:**
```bash
GET http://localhost:5001/api/auctions/active
```

**3. Отримати аукціон за ID:**
```bash
GET http://localhost:5001/api/auctions/1
```

**4. Створити новий аукціон:**
```bash
POST http://localhost:5001/api/auctions
Content-Type: application/json

{
  "artworkId": 201,
  "artworkName": "New Artwork",
  "sellerUserId": 1,
  "startPrice": 1000,
  "startTime": "2025-11-06T00:00:00Z",
  "endTime": "2025-11-13T00:00:00Z"
}
```

**5. Зробити ставку:**
```bash
POST http://localhost:5001/api/bids
Content-Type: application/json

{
  "auctionId": 1,
  "userId": 2,
  "bidAmount": 2000
}
```

## Досягнуті результати

### ✅ Реалізовано всі основні вимоги:

1. **Code-First підхід з EF Core**
   - Доменні моделі з навігаційними властивостями
   - Fluent API конфігурація
   - Міграції та Seeding

2. **Repository Pattern**
   - Generic Repository
   - Специфічні репозиторії з Eager/Explicit Loading
   - LINQ to Entities запити

3. **Unit of Work**
   - Координація репозиторіїв
   - Підтримка транзакцій

4. **Тришарова архітектура**
   - Domain Layer (Entities)
   - Data Access Layer (Repositories, DbContext)
   - Business Logic Layer (Services, DTO)
   - Presentation Layer (API Controllers)

5. **Додаткові можливості**
   - AutoMapper для маппінгу
   - Serilog для логування
   - ProblemDetails для обробки помилок
   - Swagger для документації

## Висновки

У ході виконання лабораторної роботи:

1. **Освоєно Code-First підхід** - створення бази даних з C# класів через міграції EF Core
2. **Реалізовано Repository/UoW паттерни** - абстракція доступу до даних та координація змін
3. **Впроваджено LINQ to Entities** - різні види завантаження даних (Eager, Explicit)
4. **Використано DTO та AutoMapper** - розділення доменних моделей та моделей передачі даних
5. **Налаштовано інфраструктуру** - логування, обробка помилок, документація API

Результатом є повноцінний RESTful API з тришаровою архітектурою, який відповідає сучасним практикам розробки на .NET.

## Корисні посилання

- [EF Core Documentation](https://learn.microsoft.com/ef/core/)
- [Repository Pattern](https://learn.microsoft.com/aspnet/mvc/overview/older-versions/getting-started-with-ef-5-using-mvc-4/implementing-the-repository-and-unit-of-work-patterns-in-an-asp-net-mvc-application)
- [AutoMapper](https://docs.automapper.org/)
- [Serilog](https://serilog.net/)
- [ASP.NET Core Best Practices](https://learn.microsoft.com/aspnet/core/fundamentals/best-practices)

---

**Виконав:** [Ваше ім'я]  
**Група:** [Ваша група]  
**Дата:** 05.11.2025
