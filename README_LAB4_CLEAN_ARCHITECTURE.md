# Лабораторна робота #4 - Clean Architecture з CQRS, MediatR та MongoDB

## 🏗️ Архітектура проекту

Проект побудований за принципами **Clean Architecture** з чотирма основними шарами:

```
ArtAuction.Domain/          (Core Layer - немає зовнішніх залежностей)
├── Common/
│   ├── BaseEntity.cs       - Базовий клас для всіх сутностей з ObjectId
│   └── ValueObject.cs      - Базовий клас для Value Objects
├── Entities/
│   ├── User.cs            - Користувач з балансом (Money), email (Email), адресою (Address)
│   ├── Auction.cs         - Аукціон з вкладеними ставками (BidInfo), статусами
│   └── Payment.cs         - Платіж з транзакціями
├── ValueObjects/
│   ├── Money.cs           - Immutable Value Object для грошей з валютою
│   ├── Email.cs           - Immutable Value Object для email з валідацією
│   └── Address.cs         - Immutable Value Object для адреси
├── Enums/
│   └── AuctionStatus.cs   - AuctionStatus, PaymentStatus
├── Exceptions/
│   └── DomainException.cs - NotFoundException, ConflictException, ValidationException
└── Interfaces/
    ├── IRepository.cs     - Generic repository з MongoDB-специфічними методами
    ├── IRepositories.cs   - IAuctionRepository, IUserRepository, IPaymentRepository
    └── IUnitOfWork.cs     - Unit of Work з MongoDB transactions

ArtAuction.Application/     (Use Cases Layer - залежить тільки від Domain)
├── Common/
│   ├── Interfaces/
│   │   └── ICommand.cs    - ICommand, ICommand<T>, IQuery<T>
│   └── Models/
│       └── PagedResult.cs - Generic paged result model
├── Features/
│   ├── Auctions/
│   │   ├── Commands/
│   │   │   ├── CreateAuction/
│   │   │   │   ├── CreateAuctionCommand.cs
│   │   │   │   ├── CreateAuctionCommandHandler.cs
│   │   │   │   └── CreateAuctionCommandValidator.cs
│   │   │   └── PlaceBid/
│   │   │       ├── PlaceBidCommand.cs
│   │   │       ├── PlaceBidCommandHandler.cs
│   │   │       └── PlaceBidCommandValidator.cs
│   │   ├── Queries/
│   │   │   ├── GetAuctions/
│   │   │   │   ├── GetAuctionsQuery.cs
│   │   │   │   └── GetAuctionsQueryHandler.cs
│   │   │   └── GetAuctionById/
│   │   │       ├── GetAuctionByIdQuery.cs
│   │   │       └── GetAuctionByIdQueryHandler.cs
│   │   └── DTOs/
│   │       ├── AuctionDto.cs
│   │       └── BidDto.cs
│   ├── Users/
│   │   ├── Commands/CreateUser/...
│   │   ├── Queries/GetUser/...
│   │   └── DTOs/UserDto.cs
│   └── Payments/
│       ├── Commands/CreatePayment/...
│       ├── Queries/GetPayment/...
│       └── DTOs/PaymentDto.cs
├── Behaviors/
│   ├── ValidationBehavior.cs         - FluentValidation pipeline behavior
│   ├── LoggingBehavior.cs           - Request/Response logging
│   ├── PerformanceBehavior.cs       - Performance monitoring
│   └── ExceptionHandlingBehavior.cs - Centralized exception handling
└── Mappings/
    └── MappingProfile.cs            - AutoMapper profile

ArtAuction.Infrastructure/  (Persistence Layer - реалізує інтерфейси з Domain/Application)
├── Persistence/
│   ├── MongoDbContext.cs           - IMongoDatabase wrapper
│   ├── UnitOfWork.cs               - Unit of Work з MongoDB sessions
│   ├── Repositories/
│   │   ├── MongoRepository.cs      - Generic repository implementation
│   │   ├── AuctionRepository.cs    - Auction-specific queries
│   │   ├── UserRepository.cs       - User-specific queries
│   │   └── PaymentRepository.cs    - Payment-specific queries
│   ├── Configurations/
│   │   ├── MongoDbSettings.cs      - Configuration class
│   │   └── BsonClassMapConfiguration.cs - BSON class maps
│   ├── Serializers/
│   │   ├── MoneySerializer.cs      - Custom BSON serializer for Money
│   │   ├── EmailSerializer.cs      - Custom BSON serializer for Email
│   │   └── AddressSerializer.cs    - Custom BSON serializer for Address
│   └── Seeding/
│       ├── IDataSeeder.cs
│       ├── UserSeeder.cs
│       ├── AuctionSeeder.cs
│       └── DataSeederRunner.cs
└── Services/
    └── IndexCreationService.cs     - Automatic index creation

ArtAuction.WebApi/          (Presentation Layer - Controllers, DI, Middleware)
├── Controllers/
│   ├── AuctionsController.cs      - Auction endpoints з MediatR
│   ├── UsersController.cs         - User endpoints
│   └── PaymentsController.cs      - Payment endpoints
├── Middleware/
│   └── GlobalExceptionMiddleware.cs - Centralized error handling
├── Program.cs                      - DI configuration, MongoDB, MediatR, Swagger
└── appsettings.json               - MongoDB connection string
```

## 📦 Технології та пакети

### Domain Layer
- **MongoDB.Bson 2.30.0** - BSON типи та атрибути

### Application Layer
- **MediatR 12.4.1** - CQRS pattern implementation
- **FluentValidation 11.11.0** - Request validation
- **AutoMapper 12.0.1** - Object-to-object mapping

### Infrastructure Layer
- **MongoDB.Driver 2.30.0** - MongoDB C# driver
- **Microsoft.Extensions.Options.ConfigurationExtensions 8.0.0** - Configuration binding

### WebApi Layer
- **MediatR 12.4.1** - MediatR в контролерах
- **AutoMapper.Extensions.Microsoft.DependencyInjection 12.0.1** - AutoMapper DI
- **Swashbuckle.AspNetCore** - Swagger/OpenAPI

## 🎯 Ключові концепції

### 1. Clean Architecture Principles

**Dependency Rule**: Залежності спрямовані всередину (до Domain)

```
WebApi → Application → Domain
Infrastructure → Application → Domain
```

- **Domain** не знає про інші шари
- **Application** знає тільки про Domain
- **Infrastructure** реалізує інтерфейси з Domain/Application
- **WebApi** оркеструє все через MediatR

### 2. CQRS з MediatR

**Commands** (зміна стану):
```csharp
public record CreateAuctionCommand(
    string ArtworkName,
    string SellerId,
    decimal StartPrice,
    DateTime StartTime,
    DateTime EndTime,
    string? Description
) : ICommand<string>; // Повертає ID створеного аукціону

public class CreateAuctionCommandHandler : IRequestHandler<CreateAuctionCommand, string>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<string> Handle(CreateAuctionCommand request, CancellationToken cancellationToken)
    {
        var auction = Auction.Create(
            request.ArtworkName,
            request.SellerId,
            request.StartPrice,
            request.StartTime,
            request.EndTime,
            request.Description
        );
        
        await _unitOfWork.Auctions.AddAsync(auction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return auction.Id;
    }
}
```

**Queries** (читання даних):
```csharp
public record GetAuctionsQuery(
    int Page = 1,
    int PageSize = 10,
    AuctionStatus? Status = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null
) : IQuery<PagedResult<AuctionDto>>;

public class GetAuctionsQueryHandler : IRequestHandler<GetAuctionsQuery, PagedResult<AuctionDto>>
{
    private readonly IAuctionRepository _repository;
    private readonly IMapper _mapper;
    
    public async Task<PagedResult<AuctionDto>> Handle(GetAuctionsQuery request, CancellationToken cancellationToken)
    {
        Expression<Func<Auction, bool>> predicate = a => true;
        
        if (request.Status.HasValue)
            predicate = predicate.And(a => a.Status == request.Status);
            
        if (request.MinPrice.HasValue)
            predicate = predicate.And(a => a.CurrentPrice.Amount >= request.MinPrice);
            
        var (items, totalCount) = await _repository.FindPagedAsync(
            predicate,
            request.Page,
            request.PageSize,
            orderBy: a => a.EndTime,
            cancellationToken: cancellationToken
        );
        
        var dtos = _mapper.Map<IReadOnlyList<AuctionDto>>(items);
        
        return new PagedResult<AuctionDto>(dtos, request.Page, request.PageSize, totalCount);
    }
}
```

### 3. Value Objects з BSON серіалізацією

**Money Value Object**:
```csharp
[BsonNoId]
public sealed class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    private Money() { } // For MongoDB deserialization

    public static Money Create(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative");
        return new Money(amount, currency.ToUpperInvariant());
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        return new Money(Amount + other.Amount, Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

**BSON серіалізація** автоматично підтримується через атрибути `[BsonElement]`, `[BsonNoId]`.

### 4. Pipeline Behaviors

**ValidationBehavior** - автоматична валідація перед виконанням:
```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
                throw new FluentValidation.ValidationException(failures);
        }

        return await next();
    }
}
```

**LoggingBehavior** - логування всіх запитів:
```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {RequestName} with data: {@Request}", 
            typeof(TRequest).Name, request);

        var response = await next();

        _logger.LogInformation("Handled {RequestName} with result: {@Response}", 
            typeof(TRequest).Name, response);

        return response;
    }
}
```

### 5. MongoDB Integration

**MongoDbContext**:
```csharp
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>() where T : BaseEntity
    {
        var collectionName = typeof(T)
            .GetCustomAttribute<BsonCollectionAttribute>()
            ?.CollectionName ?? typeof(T).Name.ToLowerInvariant() + "s";
            
        return _database.GetCollection<T>(collectionName);
    }
}
```

**Generic Repository з MongoDB**:
```csharp
public class MongoRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> _collection;

    public MongoRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<T>();
    }

    public async Task<(IReadOnlyList<T> Items, long TotalCount)> FindPagedAsync(
        Expression<Func<T, bool>>? predicate,
        int page,
        int pageSize,
        Expression<Func<T, object>>? orderBy = null,
        bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var filter = predicate != null 
            ? Builders<T>.Filter.Where(predicate) 
            : Builders<T>.Filter.Empty;

        var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var findFluent = _collection.Find(filter);

        if (orderBy != null)
        {
            var sortDefinition = descending
                ? Builders<T>.Sort.Descending(orderBy)
                : Builders<T>.Sort.Ascending(orderBy);
            findFluent = findFluent.Sort(sortDefinition);
        }

        var items = await findFluent
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
```

**Aggregation Pipeline приклад**:
```csharp
public async Task<IReadOnlyList<Auction>> GetActiveAuctionsAsync(CancellationToken cancellationToken = default)
{
    var pipeline = _collection.Aggregate()
        .Match(a => a.Status == AuctionStatus.Active)
        .SortBy(a => a.EndTime)
        .Limit(100);

    return await pipeline.ToListAsync(cancellationToken);
}
```

### 6. Centralized Error Handling

**GlobalExceptionMiddleware**:
```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, exception.Message),
            ValidationException validationEx => (StatusCodes.Status400BadRequest, 
                JsonSerializer.Serialize(validationEx.Errors)),
            MongoWriteException mongoEx when mongoEx.WriteError.Code == 11000 => 
                (StatusCodes.Status409Conflict, "Duplicate key error"),
            MongoConnectionException => (StatusCodes.Status503ServiceUnavailable, 
                "Database connection failed"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = exception.GetType().Name,
            Detail = message,
            Instance = context.Request.Path
        };

        return context.Response.WriteAsJsonAsync(problemDetails);
    }
}
```

## 🚀 Налаштування та запуск

### 1. Встановити MongoDB

Завантажити з https://www.mongodb.com/try/download/community або через Docker:

```powershell
docker run -d -p 27017:27017 --name mongodb -e MONGO_INITDB_ROOT_USERNAME=admin -e MONGO_INITDB_ROOT_PASSWORD=password mongo:latest
```

### 2. Налаштувати appsettings.json

```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "artauction_db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 3. Запустити проект

```powershell
cd "C:\Work\Study\.NET Microservice\ProjectAuction"
dotnet run --project ArtAuction.WebApi
```

Відкрити **http://localhost:5000/swagger**

## 📊 MongoDB Schema

### Users Collection
```json
{
  "_id": ObjectId("..."),
  "user_name": "John Doe",
  "email": { "Value": "john@example.com" },
  "balance": { "Amount": 1000.00, "Currency": "USD" },
  "address": {
    "Street": "123 Main St",
    "City": "New York",
    "PostalCode": "10001",
    "Country": "USA"
  },
  "is_active": true,
  "created_at": ISODate("2025-11-19T..."),
  "updated_at": ISODate("2025-11-19T..."),
  "version": 1
}
```

### Auctions Collection
```json
{
  "_id": ObjectId("..."),
  "artwork_name": "Mona Lisa Replica",
  "description": "Beautiful replica",
  "seller_id": ObjectId("..."),
  "start_price": { "Amount": 100.00, "Currency": "USD" },
  "current_price": { "Amount": 250.00, "Currency": "USD" },
  "start_time": ISODate("2025-11-20T..."),
  "end_time": ISODate("2025-11-27T..."),
  "status": 1,
  "winner_id": null,
  "bids": [
    {
      "user_id": ObjectId("..."),
      "amount": { "Amount": 150.00, "Currency": "USD" },
      "timestamp": ISODate("2025-11-21T...")
    },
    {
      "user_id": ObjectId("..."),
      "amount": { "Amount": 250.00, "Currency": "USD" },
      "timestamp": ISODate("2025-11-22T...")
    }
  ],
  "created_at": ISODate("2025-11-19T..."),
  "updated_at": ISODate("2025-11-22T..."),
  "version": 3
}
```

### Payments Collection
```json
{
  "_id": ObjectId("..."),
  "auction_id": ObjectId("..."),
  "user_id": ObjectId("..."),
  "amount": { "Amount": 250.00, "Currency": "USD" },
  "payment_time": ISODate("2025-11-23T..."),
  "status": 1,
  "transaction_id": "TXN_123456789",
  "created_at": ISODate("2025-11-23T..."),
  "updated_at": ISODate("2025-11-23T..."),
  "version": 1
}
```

## 📝 Приклади API запитів

### POST /api/auctions - Створити аукціон
```json
{
  "artworkName": "The Scream",
  "sellerId": "673c1234567890abcdef1234",
  "startPrice": 1000.00,
  "startTime": "2025-11-25T10:00:00Z",
  "endTime": "2025-12-01T10:00:00Z",
  "description": "Famous artwork replica"
}
```

Response: `"673c9876543210fedcba4321"` (auction ID)

### GET /api/auctions?page=1&pageSize=10&status=1
```json
{
  "items": [
    {
      "id": "673c...",
      "artworkName": "The Scream",
      "currentPrice": { "amount": 1500.00, "currency": "USD" },
      "status": "Active",
      "endTime": "2025-12-01T10:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 25,
  "totalPages": 3,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

### POST /api/auctions/{id}/bids - Розмістити ставку
```json
{
  "userId": "673c1111222233334444",
  "bidAmount": 1600.00
}
```

## ✅ Критерії виконання

- ✅ Clean Architecture з 4 шарами та правильними залежностями
- ✅ Domain сутності з Value Objects (Money, Email, Address)
- ✅ CQRS з MediatR (Commands та Queries)
- ✅ FluentValidation з ValidationBehavior
- ✅ MongoDB з BSON серіалізацією
- ✅ Generic Repository та Unit of Work
- ✅ Pipeline Behaviors (Validation, Logging, Performance)
- ✅ AutoMapper для DTO mapping
- ✅ Centralized error handling з MongoDB exceptions
- ✅ Swagger documentation
- ✅ Paged results з cursor-based pagination

## 🎓 Контрольні питання

1. **Що таке Clean Architecture та чому це важливо?**
   - Незалежність від frameworks, UI, databases
   - Тестованість
   - Незалежність від зовнішніх агентів

2. **Чим CQRS відрізняється від традиційного CRUD?**
   - Розділення команд (write) та запитів (read)
   - Оптимізація кожної сторони окремо
   - Краща масштабованість

3. **Навіщо потрібні Value Objects?**
   - Інкапсуляція бізнес-правил
   - Immutability
   - Type safety

4. **Як Pipeline Behaviors покращують код?**
   - Cross-cutting concerns (logging, validation)
   - Separation of concerns
   - Reusability

5. **Особливості MongoDB порівняно з SQL?**
   - Document-oriented (flexible schema)
   - Horizontal scalability
   - No joins (embedded documents)

## 📚 Корисні посилання

- [Clean Architecture by Jason Taylor](https://github.com/jasontaylordev/CleanArchitecture)
- [MediatR GitHub](https://github.com/jbogard/MediatR)
- [MongoDB C# Driver Docs](https://www.mongodb.com/docs/drivers/csharp/current/)
- [CQRS Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [DDD and Clean Architecture](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice)
