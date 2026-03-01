# Coding Conventions

> Status: PUBLISHED | Last Updated: 2026-03-01

Standards and best practices for EV Charging CSMS codebase consistency, maintainability, and performance.

## Language & Localization

### Code Language
- **All code** in English: variable names, method names, class names, comments
- **Documentation** in English
- **User-facing strings** ONLY in Vietnamese (via `IStringLocalizer`)

Example:
```csharp
// Good
public class ChargingSessionService
{
    private readonly IStringLocalizer<ChargingSessionResource> _localizer;

    public async Task<StartSessionResponse> StartCharging(StartSessionRequest request)
    {
        if (request.StationId == Guid.Empty)
            throw new UserFriendlyException(
                _localizer["StationNotFound"]  // "Không tìm thấy trạm sạc"
            );
        // ...
    }
}

// Bad
public class DichVuSach  // Vietnamese class name
{
    private string message = "Lỗi";  // Hardcoded Vietnamese
}
```

## C# Naming Conventions

### Classes & Interfaces
```csharp
// PascalCase
public class ChargingStation { }
public class ChargingSessionQuery { }
public interface IChargingStationRepository { }
public record StartChargeRequest(Guid StationId, int ConnectorId);
```

### Methods & Properties
```csharp
// PascalCase
public async Task<ChargingSessionDto> GetSession(Guid sessionId) { }
public bool IsActive { get; set; }
private string _backingField;  // camelCase with underscore for private fields
```

### Parameters & Local Variables
```csharp
// camelCase
public void ProcessPayment(string transactionId, decimal amount)
{
    var currentTime = DateTime.UtcNow;
    const int maxRetries = 3;
}
```

### Constants
```csharp
// PascalCase or UPPER_CASE for domain constants
public const int MaxConnectorsPerStation = 16;
public const string CacheKeyPrefix = "station:{id}:status";
private const decimal DefaultChargeRate = 250_000m;  // VNĐ
```

### Acronyms
```csharp
// PascalCase (not all-caps)
public class OcppConnector { }  // Not OCPP
public interface IJsonRpcHandler { }
public enum EvConnectorType { Type1, Type2, Ccs }  // Not CCS
```

## ABP Framework Conventions

### Entity Definition
All domain entities must inherit from ABP aggregate root:

```csharp
namespace EVCharging.Stations.Domain
{
    public class ChargingStation : FullAuditedAggregateRoot<Guid>
    {
        // Required properties
        public string Name { get; set; }
        public string Address { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int TotalConnectors { get; set; }

        // Navigation
        public virtual ICollection<Connector> Connectors { get; set; }

        // Domain logic
        public Connector CreateConnector(EvConnectorType type)
        {
            var connector = new Connector
            {
                StationId = Id,
                ConnectorType = type,
                Status = ConnectorStatus.Available
            };
            Connectors.Add(connector);
            return connector;
        }
    }

    public class Connector : Entity<Guid>
    {
        public Guid StationId { get; set; }
        public EvConnectorType ConnectorType { get; set; }
        public ConnectorStatus Status { get; set; }
    }
}
```

### Repository Naming
```csharp
// Interface — singular noun
public interface IChargingStationRepository : IRepository<ChargingStation, Guid>
{
    Task<ChargingStation> GetByNameAsync(string name);
    Task<List<ChargingStation>> GetByProvinceAsync(string provinceCode);
}

// Implementation
public class ChargingStationRepository : EfCoreRepository<EVChargingDbContext, ChargingStation, Guid>,
    IChargingStationRepository
{
    public ChargingStationRepository(IDbContextProvider<EVChargingDbContext> dbContextProvider)
        : base(dbContextProvider) { }

    public async Task<ChargingStation> GetByNameAsync(string name)
        => await DbSet.FirstOrDefaultAsync(x => x.Name == name);

    public async Task<List<ChargingStation>> GetByProvinceAsync(string provinceCode)
        => await DbSet.Where(x => x.ProvinceCode == provinceCode).ToListAsync();
}
```

### Application Service Naming
```csharp
// Interface — {Entity}AppService
public interface IChargingStationAppService : IApplicationService
{
    Task<ChargingStationDto> CreateAsync(CreateChargingStationDto input);
    Task<ChargingStationDto> UpdateAsync(Guid id, UpdateChargingStationDto input);
    Task<ChargingStationDto> GetAsync(Guid id);
    Task DeleteAsync(Guid id);
    Task<PagedResultDto<ChargingStationDto>> GetListAsync(GetChargingStationListInput input);
}

// Implementation
[Authorize]
public class ChargingStationAppService : ApplicationService, IChargingStationAppService
{
    private readonly IChargingStationRepository _repository;
    private readonly IMapper _mapper;

    public ChargingStationAppService(
        IChargingStationRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ChargingStationDto> CreateAsync(CreateChargingStationDto input)
    {
        var entity = new ChargingStation { Name = input.Name, Address = input.Address };
        entity = await _repository.InsertAsync(entity);
        return _mapper.Map<ChargingStationDto>(entity);
    }
}
```

### DTO Naming
```csharp
namespace EVCharging.Stations.Dtos
{
    // Create
    public class CreateChargingStationDto
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; }
    }

    // Update
    public class UpdateChargingStationDto : CreateChargingStationDto
    {
        [Required]
        public Guid Id { get; set; }
    }

    // Read (output)
    public class ChargingStationDto : IEntityDto<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public DateTime CreationTime { get; set; }
    }

    // List input
    public class GetChargingStationListInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }
        public string ProvinceCode { get; set; }
    }
}
```

## CQRS Patterns (MediatR)

### Query Definition
```csharp
namespace EVCharging.Stations.Queries
{
    // Query request (IRequest<TResponse>)
    public class GetChargingStationQuery : IRequest<ChargingStationDto>
    {
        public Guid StationId { get; set; }
    }

    // Query handler
    public class GetChargingStationQueryHandler : IRequestHandler<GetChargingStationQuery, ChargingStationDto>
    {
        private readonly IChargingStationRepository _repository;
        private readonly IMapper _mapper;

        public GetChargingStationQueryHandler(
            IChargingStationRepository repository,
            IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ChargingStationDto> Handle(GetChargingStationQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetAsync(request.StationId);
            return _mapper.Map<ChargingStationDto>(entity);
        }
    }
}
```

### Command Definition
```csharp
namespace EVCharging.Stations.Commands
{
    // Command request (IRequest<TResponse>)
    public class CreateChargingStationCommand : IRequest<ChargingStationDto>
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }

    // Command handler
    public class CreateChargingStationCommandHandler : IRequestHandler<CreateChargingStationCommand, ChargingStationDto>
    {
        private readonly IChargingStationRepository _repository;
        private readonly IMapper _mapper;

        public CreateChargingStationCommandHandler(
            IChargingStationRepository repository,
            IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ChargingStationDto> Handle(
            CreateChargingStationCommand request,
            CancellationToken cancellationToken)
        {
            var entity = new ChargingStation { Name = request.Name, Address = request.Address };
            entity = await _repository.InsertAsync(entity);
            return _mapper.Map<ChargingStationDto>(entity);
        }
    }
}
```

## API Controller Conventions

### Minimal API (Driver BFF)
```csharp
// Program.cs
var app = builder.Build();

var api = app.MapGroup("/api/v1/stations")
    .WithTags("Stations");

api.MapGet("/", GetStations)
    .WithName("GetStations")
    .WithOpenApi()
    .Produces<List<StationDto>>();

api.MapGet("/{id}", GetStation)
    .WithName("GetStation")
    .WithOpenApi()
    .Produces<StationDto>()
    .Produces(StatusCodes.Status404NotFound);

api.MapPost("/", CreateStation)
    .WithName("CreateStation")
    .WithOpenApi()
    .Produces<StationDto>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .RequireAuthorization();

async Task<List<StationDto>> GetStations(
    IMediator mediator,
    [FromQuery] string filter = "")
{
    var query = new ListStationsQuery { Filter = filter };
    return await mediator.Send(query);
}

async Task<IResult> GetStation(
    Guid id,
    IMediator mediator)
{
    var query = new GetStationQuery { StationId = id };
    var result = await mediator.Send(query);
    return result == null ? Results.NotFound() : Results.Ok(result);
}

async Task<IResult> CreateStation(
    CreateStationDto dto,
    IMediator mediator)
{
    var command = new CreateStationCommand { Name = dto.Name, Address = dto.Address };
    var result = await mediator.Send(command);
    return Results.Created($"/api/v1/stations/{result.Id}", result);
}
```

### Standard Controller (Admin API)
```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/stations")]
[Authorize]
public class StationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ChargingStationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChargingStationDto>> GetAsync(Guid id)
    {
        var query = new GetChargingStationQuery { StationId = id };
        var result = await _mediator.Send(query);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ChargingStationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChargingStationDto>> CreateAsync(CreateChargingStationDto input)
    {
        var command = new CreateChargingStationCommand { Name = input.Name };
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAsync), new { id = result.Id }, result);
    }
}
```

## Error Handling

### Standard Error Response Format
All API errors follow this format:
```json
{
  "code": "MOD_001",
  "message": "Detailed error message",
  "details": {
    "field1": ["Error detail 1"],
    "field2": ["Error detail 2"]
  }
}
```

### Error Codes
- **MOD_NNN**: Module-specific error (e.g., `MOD_001` for Stations module)
- **AUTH_NNN**: Authentication errors
- **VAL_NNN**: Validation errors
- **SYS_NNN**: System errors

### Exception Handling Pattern
```csharp
public class ChargingStationAppService : ApplicationService
{
    public async Task<ChargingStationDto> GetAsync(Guid id)
    {
        try
        {
            var entity = await _repository.GetAsync(id);
            if (entity == null)
                throw new EntityNotFoundException(typeof(ChargingStation), id);
            return _mapper.Map<ChargingStationDto>(entity);
        }
        catch (EntityNotFoundException ex)
        {
            throw new UserFriendlyException(
                _localizer["StationNotFound"],
                errorCode: "MOD_001"
            );
        }
    }
}
```

### Validation Pattern
```csharp
public class CreateChargingStationCommandValidator : AbstractValidator<CreateChargingStationCommand>
{
    public CreateChargingStationCommandValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["NameRequired"])
            .MaximumLength(255).WithMessage(localizer["NameTooLong"]);

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage(localizer["AddressRequired"]);
    }
}
```

## Caching Conventions

### Cache Key Format
```csharp
// Standard format: "entity:{id}:field"
public const string StationCacheKey = "station:{id}:info";
public const string ConnectorStatusCacheKey = "connector:{id}:status";
public const string SessionActiveCacheKey = "session:{id}:active";

// List cache
public const string StationsListCacheKey = "stations:list:{filter}:{page}";

// Usage
var cacheKey = $"station:{stationId}:info";
var cached = await _cache.GetAsync<ChargingStationDto>(cacheKey);
```

### Cache Expiration
```csharp
// TTL (Time To Live) in seconds
public const int StationCacheTtl = 3600;  // 1 hour
public const int ConnectorStatusTtl = 300;  // 5 minutes (frequently changes)
public const int SessionListTtl = 60;  // 1 minute (very dynamic)

// Usage
await _cache.SetAsync(cacheKey, data, TimeSpan.FromSeconds(StationCacheTtl));
```

## Pagination

### Cursor-Based Pagination (Required)
Never use offset-based pagination (skip/take):

```csharp
// Request DTO
public class GetStationsListInput : PagedAndSortedResultRequestDto
{
    public string Cursor { get; set; }  // Base64 encoded last ID
    public int MaxResultCount { get; set; } = 20;
    public string Sorting { get; set; } = "Id desc";
}

// Query handler
public class ListStationsQueryHandler : IRequestHandler<ListStationsQuery, PagedResultDto<StationDto>>
{
    public async Task<PagedResultDto<StationDto>> Handle(ListStationsQuery request, CancellationToken ct)
    {
        var query = _repository.GetQueryableAsync().Result;

        // Apply cursor
        if (!string.IsNullOrEmpty(request.Cursor))
        {
            var cursorId = Guid.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(request.Cursor)));
            var cursorEntity = await _repository.GetAsync(cursorId);
            query = query.Where(x => x.CreationTime < cursorEntity.CreationTime);
        }

        // Fetch one extra to determine if there are more results
        var items = await query.Take(request.MaxResultCount + 1).ToListAsync();
        var hasMore = items.Count > request.MaxResultCount;
        items = items.Take(request.MaxResultCount).ToList();

        var nextCursor = hasMore ? Convert.ToBase64String(Encoding.UTF8.GetBytes(items.Last().Id.ToString())) : null;

        return new PagedResultDto<StationDto>
        {
            Items = _mapper.Map<List<StationDto>>(items),
            TotalCount = hasMore ? request.MaxResultCount + 1 : items.Count
        };
    }
}
```

## Date & Time Formatting

### Server-Side
- Always store and process in **UTC** in database
- Internal logic uses `DateTime.UtcNow`
- Convert to **UTC+7** only for display/API responses

```csharp
// Correct
public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;  // Store UTC

// For display
public string CreatedAtDisplay => CreatedAtUtc
    .AddHours(7)  // Convert to UTC+7
    .ToString("dd/MM/yyyy HH:mm:ss");  // Vietnamese format
```

### API Response Format
All date fields in ISO 8601 UTC format with timezone offset:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "creationTime": "2026-03-01T10:30:00+07:00",
  "lastModificationTime": "2026-03-01T14:45:30+07:00"
}
```

## Currency Formatting

### Database
Store amounts as `decimal(18,2)` in VNĐ:
```csharp
public decimal ChargeRate { get; set; }  // e.g., 250000.00 VNĐ
```

### Display
Format with thousand separator (dấu chấm):
```csharp
public string ChargeRateDisplay => ChargeRate.ToString("N0") + "đ";  // 250.000đ

// Localizer
_localizer.SetCulture("vi-VN");  // Uses VNĐ formatting
```

## Localization Conventions

### Resource File Structure
```
Resources/
├── Dtos/
│   └── ChargingSessionDtoResource.{culture}.resx
├── Exceptions/
│   └── ExceptionResource.{culture}.resx
└── ValidationResource.{culture}.resx
```

### Usage Pattern
```csharp
public class ChargingSessionAppService : ApplicationService
{
    private readonly IStringLocalizer<ChargingSessionResource> _localizer;

    public ChargingSessionAppService(
        IStringLocalizer<ChargingSessionResource> localizer)
    {
        _localizer = localizer;
    }

    public async Task<ChargingSessionDto> StartAsync(StartSessionCommand request)
    {
        if (request.Duration <= 0)
            throw new UserFriendlyException(
                _localizer["InvalidDuration"],  // "Thời lượng không hợp lệ"
                errorCode: "VAL_001"
            );
        // ...
    }
}
```

## Git Commit Conventions

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Type
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `style`: Code style (formatting, missing semicolons, etc.)
- `refactor`: Code refactoring
- `perf`: Performance improvement
- `test`: Adding/updating tests
- `chore`: Build, CI/CD, dependencies
- `ci`: CI/CD changes

### Scope
Module name: `stations`, `charging`, `payment`, `auth`, etc.

### Examples
```
feat(stations): add station location filtering by proximity

fix(charging): correct connector status update timing in OCPP protocol

docs(api-guide): add pagination examples for list endpoints

refactor(payment): simplify transaction retry logic

perf(driver-api): implement query result caching with Redis
```

### Body & Footer
```
fix(charging): prevent duplicate session start

When a connector receives rapid OCPP Start messages, multiple sessions
are created. This fix adds atomic transaction check in handler.

Closes #123
```

## Code Style Tools

### EditorConfig (.editorconfig)
```ini
root = true

[*.cs]
indent_style = space
indent_size = 4
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

csharp_indent_case_contents = true
csharp_indent_switch_labels = true
csharp_space_after_cast = true

# Naming conventions
dotnet_naming_rule.interfaces_should_be_begins_with_i.severity = suggestion
dotnet_naming_style.begins_with_i.required_prefix = I
dotnet_naming_style.begins_with_i.capitalization = pascal_case
```

### Roslyn Analyzers
Enable in `.csproj`:
```xml
<PropertyGroup>
  <EnableNETAnalyzers>true</EnableNETAnalyzers>
  <AnalysisLevel>latest</AnalysisLevel>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

## Code Review Checklist

Before submitting PR, ensure:
- [ ] Code follows naming conventions
- [ ] All strings use `IStringLocalizer`
- [ ] Error handling with proper error codes
- [ ] CQRS pattern for complex operations
- [ ] Cache keys follow format
- [ ] Pagination uses cursor-based (not offset)
- [ ] Database migrations included
- [ ] Unit tests added (80%+ coverage)
- [ ] API documented with Swagger/OpenAPI
- [ ] Commit messages follow conventional commits
- [ ] No hardcoded Vietnamese strings
- [ ] No hardcoded configuration values
