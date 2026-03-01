# Patterns Index

> ✅ USE these proven patterns

## PAT-001: ABP Entity Creation
```csharp
public class ChargingStation : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; private set; }
    public StationStatus Status { get; private set; }

    protected ChargingStation() { } // EF Core

    public ChargingStation(Guid id, string name) : base(id)
    {
        SetName(name);
        Status = StationStatus.Offline;
    }

    public void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 200);
    }
}
```

## PAT-002: CQRS Query Handler
```csharp
public class GetStationByIdQuery : IQuery<StationDto>
{
    public Guid StationId { get; set; }
}

public class GetStationByIdHandler : IRequestHandler<GetStationByIdQuery, StationDto>
{
    // Implementation with Redis cache-first pattern
}
```

## PAT-003: Redis Cache-First Pattern
```csharp
var cached = await _cache.GetAsync<StationDto>($"station:{id}");
if (cached != null) return cached;

var station = await _repository.GetAsync(id);
var dto = ObjectMapper.Map<StationDto>(station);
await _cache.SetAsync($"station:{id}", dto, TimeSpan.FromMinutes(5));
return dto;
```

<!-- Add more patterns as developed -->
