using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Sessions;
using Microsoft.EntityFrameworkCore;

namespace KLC.Driver.Services;

public interface ISessionBffService
{
    Task<SessionResponseDto> StartSessionAsync(Guid userId, StartSessionRequest request);
    Task<SessionResponseDto> StopSessionAsync(Guid userId, Guid sessionId);
    Task<ActiveSessionDto?> GetActiveSessionAsync(Guid userId);
    Task<SessionDetailDto?> GetSessionDetailAsync(Guid userId, Guid sessionId);
    Task<PagedResult<SessionHistoryDto>> GetSessionHistoryAsync(Guid userId, Guid? cursor, int pageSize, DateTime? fromDate = null);
}

public class SessionBffService : ISessionBffService
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ISessionBffAppService _sessionAppService;
    private readonly ILogger<SessionBffService> _logger;

    public SessionBffService(
        KLCDbContext dbContext,
        ICacheService cache,
        ISessionBffAppService sessionAppService,
        ILogger<SessionBffService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _sessionAppService = sessionAppService;
        _logger = logger;
    }

    public async Task<SessionResponseDto> StartSessionAsync(Guid userId, StartSessionRequest request)
    {
        // Delegate business logic to Application layer
        var result = await _sessionAppService.StartSessionAsync(new StartSessionInput
        {
            UserId = userId,
            StationId = request.StationId,
            StationCode = request.StationCode,
            ConnectorId = request.ConnectorId,
            ConnectorNumber = request.ConnectorNumber,
            VehicleId = request.VehicleId
        });

        // BFF handles cache invalidation after successful start
        if (result.StationId.HasValue)
        {
            await _cache.RemoveAsync(CacheKeys.StationConnectors(result.StationId.Value));
            await _cache.RemoveAsync(CacheKeys.StationDetail(result.StationId.Value));
        }

        if (result.Success)
        {
            await _cache.RemoveAsync(CacheKeys.UserActiveSession(userId));
        }

        // Map Application DTO to BFF DTO (preserves mobile API contract)
        return new SessionResponseDto
        {
            Success = result.Success,
            SessionId = result.SessionId,
            Status = result.Status,
            Error = result.Error
        };
    }

    public async Task<SessionResponseDto> StopSessionAsync(Guid userId, Guid sessionId)
    {
        // Delegate business logic to Application layer
        var result = await _sessionAppService.StopSessionAsync(userId, sessionId);

        // BFF handles cache invalidation after successful stop
        if (result.Success)
        {
            await _cache.RemoveAsync(CacheKeys.UserActiveSession(userId));
            await _cache.RemoveAsync(CacheKeys.SessionDetail(sessionId));
        }

        // Map Application DTO to BFF DTO (preserves mobile API contract)
        return new SessionResponseDto
        {
            Success = result.Success,
            SessionId = result.SessionId,
            Status = result.Status,
            Error = result.Error
        };
    }

    public async Task<ActiveSessionDto?> GetActiveSessionAsync(Guid userId)
    {
        var cacheKey = CacheKeys.UserActiveSession(userId);

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var session = await _dbContext.ChargingSessions
                .AsNoTracking()
                .Where(s => s.UserId == userId &&
                            (s.Status == SessionStatus.Pending ||
                             s.Status == SessionStatus.Starting ||
                             s.Status == SessionStatus.InProgress ||
                             s.Status == SessionStatus.Stopping))
                .Select(s => new ActiveSessionDto
                {
                    SessionId = s.Id,
                    StationId = s.StationId,
                    ConnectorNumber = s.ConnectorNumber,
                    Status = s.Status,
                    StartTime = s.StartTime,
                    EnergyKwh = s.TotalEnergyKwh,
                    CurrentCost = s.TotalCost,
                    RatePerKwh = s.RatePerKwh
                })
                .FirstOrDefaultAsync();

            if (session != null)
            {
                // Get station info
                var station = await _dbContext.ChargingStations
                    .AsNoTracking()
                    .Where(s => s.Id == session.StationId)
                    .Select(s => new { s.Name, s.Address })
                    .FirstOrDefaultAsync();

                session.StationName = station?.Name ?? "";
                session.StationAddress = station?.Address ?? "";

                // Get latest meter value for real-time SoC + Power
                var latestMeter = await _dbContext.MeterValues
                    .AsNoTracking()
                    .Where(m => m.SessionId == session.SessionId)
                    .OrderByDescending(m => m.Timestamp)
                    .Select(m => new { m.SocPercent, m.PowerKw, m.VoltageVolts, m.CurrentAmps })
                    .FirstOrDefaultAsync();

                if (latestMeter != null)
                {
                    session.SocPercent = latestMeter.SocPercent;
                    session.PowerKw = latestMeter.PowerKw;
                    session.VoltageVolts = latestMeter.VoltageVolts;
                    session.CurrentAmps = latestMeter.CurrentAmps;
                }

                // Get vehicle battery capacity for ETA calculation
                var sessionEntity = await _dbContext.ChargingSessions
                    .AsNoTracking()
                    .Where(s => s.Id == session.SessionId)
                    .Select(s => new { s.VehicleId })
                    .FirstOrDefaultAsync();

                if (sessionEntity?.VehicleId.HasValue == true)
                {
                    var vehicle = await _dbContext.Vehicles
                        .AsNoTracking()
                        .Where(v => v.Id == sessionEntity.VehicleId.Value)
                        .Select(v => new { v.BatteryCapacityKwh, v.Make, v.Model, v.Nickname })
                        .FirstOrDefaultAsync();

                    if (vehicle != null)
                    {
                        session.BatteryCapacityKwh = vehicle.BatteryCapacityKwh;
                        session.VehicleName = vehicle.Nickname ?? $"{vehicle.Make} {vehicle.Model}";

                        // Calculate ETA (estimated time to full)
                        if (session.SocPercent.HasValue && session.PowerKw.HasValue
                            && session.PowerKw.Value > 0 && vehicle.BatteryCapacityKwh > 0)
                        {
                            var remainingKwh = vehicle.BatteryCapacityKwh * (100 - session.SocPercent.Value) / 100;
                            var etaMinutes = (int)(remainingKwh / session.PowerKw.Value * 60);
                            session.EstimatedMinutesToFull = etaMinutes;
                        }
                    }
                }

                // Calculate duration and convert to UTC+7
                if (session.StartTime.HasValue)
                {
                    session.DurationMinutes = (int)(DateTime.UtcNow - session.StartTime.Value).TotalMinutes;
                    session.DurationSeconds = (int)(DateTime.UtcNow - session.StartTime.Value).TotalSeconds;
                    session.StartTime = session.StartTime.Value.AddHours(7);
                }
            }

            return session;
        }, TimeSpan.FromSeconds(10));
    }

    public async Task<SessionDetailDto?> GetSessionDetailAsync(Guid userId, Guid sessionId)
    {
        var session = await _dbContext.ChargingSessions
            .AsNoTracking()
            .Include(s => s.MeterValues.OrderByDescending(m => m.Timestamp).Take(1))
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session == null) return null;

        var station = await _dbContext.ChargingStations
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == session.StationId);

        var latestMeter = session.MeterValues.FirstOrDefault();

        return new SessionDetailDto
        {
            SessionId = session.Id,
            StationId = session.StationId,
            StationName = station?.Name ?? "",
            StationAddress = station?.Address ?? "",
            ConnectorNumber = session.ConnectorNumber,
            Status = session.Status,
            StartTime = session.StartTime?.AddHours(7),
            EndTime = session.EndTime?.AddHours(7),
            DurationSeconds = session.StartTime.HasValue
                ? (int)((session.EndTime ?? DateTime.UtcNow) - session.StartTime.Value).TotalSeconds : null,
            EnergyKwh = session.TotalEnergyKwh,
            TotalCost = session.TotalCost,
            RatePerKwh = session.RatePerKwh,
            CurrentPowerKw = latestMeter?.PowerKw,
            SocPercent = latestMeter?.SocPercent,
            StopReason = session.StopReason
        };
    }

    public async Task<PagedResult<SessionHistoryDto>> GetSessionHistoryAsync(Guid userId, Guid? cursor, int pageSize, DateTime? fromDate = null)
    {
        // Include both Completed and Failed sessions in history
        var query = _dbContext.ChargingSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId
                && (s.Status == SessionStatus.Completed || s.Status == SessionStatus.Failed))
            .OrderByDescending(s => s.EndTime);

        // Period filter
        if (fromDate.HasValue)
        {
            query = (IOrderedQueryable<ChargingSession>)query
                .Where(s => s.StartTime >= fromDate.Value);
        }

        if (cursor.HasValue)
        {
            var cursorSession = await _dbContext.ChargingSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == cursor.Value);

            if (cursorSession?.EndTime != null)
            {
                query = (IOrderedQueryable<ChargingSession>)query
                    .Where(s => s.EndTime < cursorSession.EndTime);
            }
        }

        var sessions = await query
            .Take(pageSize + 1)
            .GroupJoin(_dbContext.Connectors.AsNoTracking(),
                s => new { s.StationId, s.ConnectorNumber },
                c => new { c.StationId, c.ConnectorNumber },
                (s, connectors) => new { Session = s, Connectors = connectors })
            .SelectMany(
                x => x.Connectors.DefaultIfEmpty(),
                (x, c) => new SessionHistoryDto
                {
                    SessionId = x.Session.Id,
                    StationId = x.Session.StationId,
                    ConnectorNumber = x.Session.ConnectorNumber,
                    ConnectorType = c != null ? c.ConnectorType.ToString() : string.Empty,
                    Status = x.Session.Status,
                    StartTime = x.Session.StartTime,
                    EndTime = x.Session.EndTime,
                    EnergyKwh = x.Session.TotalEnergyKwh,
                    TotalCost = x.Session.TotalCost,
                    RatePerKwh = x.Session.RatePerKwh
                })
            .ToListAsync();

        // Get station names
        var stationIds = sessions.Select(s => s.StationId).Distinct().ToList();
        var stations = await _dbContext.ChargingStations
            .AsNoTracking()
            .Where(s => stationIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        foreach (var session in sessions)
        {
            session.StationName = stations.GetValueOrDefault(session.StationId, "");
        }

        var hasMore = sessions.Count > pageSize;
        var items = hasMore ? sessions.Take(pageSize).ToList() : sessions;
        var nextCursor = hasMore && items.Any() ? items.Last().SessionId : (Guid?)null;

        return new PagedResult<SessionHistoryDto>
        {
            Data = items,
            NextCursor = nextCursor,
            HasMore = hasMore,
            PageSize = pageSize
        };
    }
}

// DTOs
public record StartSessionRequest
{
    public Guid? StationId { get; init; }
    public string? StationCode { get; init; }
    public Guid? ConnectorId { get; init; }
    public int? ConnectorNumber { get; init; }
    public Guid? VehicleId { get; init; }
}

public record SessionResponseDto
{
    public bool Success { get; init; }
    public Guid? SessionId { get; init; }
    public SessionStatus? Status { get; init; }
    public string? Error { get; init; }
}

public record ActiveSessionDto
{
    public Guid SessionId { get; init; }
    public Guid StationId { get; init; }
    public string StationName { get; set; } = string.Empty;
    public string StationAddress { get; set; } = string.Empty;
    public int ConnectorNumber { get; init; }
    public SessionStatus Status { get; init; }
    public DateTime? StartTime { get; set; }
    public decimal EnergyKwh { get; init; }
    public decimal CurrentCost { get; init; }
    public decimal RatePerKwh { get; init; }

    // Real-time charging data (from latest MeterValues)
    public decimal? SocPercent { get; set; }
    public decimal? PowerKw { get; set; }
    public decimal? VoltageVolts { get; set; }
    public decimal? CurrentAmps { get; set; }

    // Vehicle info
    public string? VehicleName { get; set; }
    public decimal? BatteryCapacityKwh { get; set; }

    // Calculated fields
    public int? EstimatedMinutesToFull { get; set; }
    public int? DurationMinutes { get; set; }
    public int? DurationSeconds { get; set; }
}

public record SessionDetailDto
{
    public Guid SessionId { get; init; }
    public Guid StationId { get; init; }
    public string StationName { get; init; } = string.Empty;
    public string StationAddress { get; init; } = string.Empty;
    public int ConnectorNumber { get; init; }
    public SessionStatus Status { get; init; }
    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public int? DurationSeconds { get; init; }
    public decimal EnergyKwh { get; init; }
    public decimal TotalCost { get; init; }
    public decimal RatePerKwh { get; init; }
    public decimal? CurrentPowerKw { get; init; }
    public decimal? SocPercent { get; init; }
    public string? StopReason { get; init; }
}

public record SessionHistoryDto
{
    public Guid SessionId { get; init; }
    public Guid StationId { get; init; }
    public string StationName { get; set; } = string.Empty;
    public int ConnectorNumber { get; init; }
    public string ConnectorType { get; init; } = string.Empty;
    public SessionStatus Status { get; init; }
    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public decimal EnergyKwh { get; init; }
    public decimal TotalCost { get; init; }
    public decimal RatePerKwh { get; init; }

    public int DurationMinutes => StartTime.HasValue && EndTime.HasValue
        ? (int)(EndTime.Value - StartTime.Value).TotalMinutes
        : 0;
}

public record PagedResult<T>
{
    public List<T> Data { get; init; } = new();
    public Guid? NextCursor { get; init; }
    public bool HasMore { get; init; }
    public int PageSize { get; init; }
}
