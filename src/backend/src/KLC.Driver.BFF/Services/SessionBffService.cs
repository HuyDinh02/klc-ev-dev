using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Fleets;
using KLC.Sessions;
using Microsoft.EntityFrameworkCore;

namespace KLC.Driver.Services;

public interface ISessionBffService
{
    Task<SessionResponseDto> StartSessionAsync(Guid userId, StartSessionRequest request);
    Task<SessionResponseDto> StopSessionAsync(Guid userId, Guid sessionId);
    Task<ActiveSessionDto?> GetActiveSessionAsync(Guid userId);
    Task<SessionDetailDto?> GetSessionDetailAsync(Guid userId, Guid sessionId);
    Task<PagedResult<SessionHistoryDto>> GetSessionHistoryAsync(Guid userId, Guid? cursor, int pageSize);
}

public class SessionBffService : ISessionBffService
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly IFleetChargingPolicyService _fleetChargingPolicyService;
    private readonly ILogger<SessionBffService> _logger;

    public SessionBffService(
        KLCDbContext dbContext,
        ICacheService cache,
        IFleetChargingPolicyService fleetChargingPolicyService,
        ILogger<SessionBffService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _fleetChargingPolicyService = fleetChargingPolicyService;
        _logger = logger;
    }

    public async Task<SessionResponseDto> StartSessionAsync(Guid userId, StartSessionRequest request)
    {
        // Validate connector availability
        var connector = await _dbContext.Connectors
            .Include(c => c.Station)
            .FirstOrDefaultAsync(c => c.StationId == request.StationId &&
                                      c.ConnectorNumber == request.ConnectorNumber &&
                                      !c.IsDeleted);

        if (connector == null)
        {
            return new SessionResponseDto { Success = false, Error = "Connector not found" };
        }

        if (!connector.IsEnabled || connector.Status != ConnectorStatus.Available)
        {
            return new SessionResponseDto { Success = false, Error = "Connector is not available" };
        }

        // Check for existing active session
        var existingSession = await _dbContext.ChargingSessions
            .FirstOrDefaultAsync(s => s.UserId == userId &&
                                      (s.Status == SessionStatus.Pending ||
                                       s.Status == SessionStatus.Starting ||
                                       s.Status == SessionStatus.InProgress));

        if (existingSession != null)
        {
            return new SessionResponseDto { Success = false, Error = "You already have an active session" };
        }

        // Validate fleet charging policy if vehicle is specified
        if (request.VehicleId.HasValue)
        {
            var policyResult = await _fleetChargingPolicyService.ValidateChargingAsync(
                request.VehicleId.Value, request.StationId);
            if (!policyResult.Allowed)
            {
                _logger.LogWarning(
                    "Session start denied by fleet policy: userId={UserId}, vehicleId={VehicleId}, reason={Reason}",
                    userId, request.VehicleId, policyResult.DenialReason);
                return new SessionResponseDto
                {
                    Success = false,
                    Error = policyResult.DenialReason ?? "Fleet charging policy denied"
                };
            }
        }

        try
        {
            // Get tariff
            var tariff = connector.Station?.TariffPlanId.HasValue == true
                ? await _dbContext.TariffPlans.FirstOrDefaultAsync(t => t.Id == connector.Station.TariffPlanId)
                : await _dbContext.TariffPlans.FirstOrDefaultAsync(t => t.IsDefault && t.IsActive);

            // Create session
            var session = new ChargingSession(
                Guid.NewGuid(),
                userId,
                request.StationId,
                request.ConnectorNumber,
                request.VehicleId,
                tariff?.Id,
                tariff?.BaseRatePerKwh ?? 0);

            await _dbContext.ChargingSessions.AddAsync(session);

            // Update connector status
            connector.UpdateStatus(ConnectorStatus.Preparing);
            await _dbContext.SaveChangesAsync();

            // Invalidate cache
            await _cache.RemoveAsync($"station:{request.StationId}:connectors");
            await _cache.RemoveAsync($"user:{userId}:active-session");

            return new SessionResponseDto
            {
                Success = true,
                SessionId = session.Id,
                Status = session.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start session for user {UserId} at station {StationId}", userId, request.StationId);
            return new SessionResponseDto { Success = false, Error = "Failed to start charging session" };
        }
    }

    public async Task<SessionResponseDto> StopSessionAsync(Guid userId, Guid sessionId)
    {
        var session = await _dbContext.ChargingSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session == null)
        {
            return new SessionResponseDto { Success = false, Error = "Session not found" };
        }

        if (session.Status != SessionStatus.InProgress && session.Status != SessionStatus.Suspended)
        {
            return new SessionResponseDto { Success = false, Error = "Session is not in progress" };
        }

        try
        {
            session.MarkStopping();
            await _dbContext.SaveChangesAsync();

            // Invalidate cache
            await _cache.RemoveAsync($"user:{userId}:active-session");
            await _cache.RemoveAsync($"session:{sessionId}:detail");

            return new SessionResponseDto
            {
                Success = true,
                SessionId = session.Id,
                Status = session.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop session {SessionId} for user {UserId}", sessionId, userId);
            return new SessionResponseDto { Success = false, Error = "Failed to stop charging session" };
        }
    }

    public async Task<ActiveSessionDto?> GetActiveSessionAsync(Guid userId)
    {
        var cacheKey = $"user:{userId}:active-session";

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
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            EnergyKwh = session.TotalEnergyKwh,
            TotalCost = session.TotalCost,
            RatePerKwh = session.RatePerKwh,
            CurrentPowerKw = latestMeter?.PowerKw,
            SocPercent = latestMeter?.SocPercent,
            StopReason = session.StopReason
        };
    }

    public async Task<PagedResult<SessionHistoryDto>> GetSessionHistoryAsync(Guid userId, Guid? cursor, int pageSize)
    {
        var query = _dbContext.ChargingSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Completed)
            .OrderByDescending(s => s.EndTime);

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
            .Select(s => new SessionHistoryDto
            {
                SessionId = s.Id,
                StationId = s.StationId,
                ConnectorNumber = s.ConnectorNumber,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                EnergyKwh = s.TotalEnergyKwh,
                TotalCost = s.TotalCost
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
    public Guid StationId { get; init; }
    public int ConnectorNumber { get; init; }
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
    public DateTime? StartTime { get; init; }
    public decimal EnergyKwh { get; init; }
    public decimal CurrentCost { get; init; }
    public decimal RatePerKwh { get; init; }
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
    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public decimal EnergyKwh { get; init; }
    public decimal TotalCost { get; init; }
}

public record PagedResult<T>
{
    public List<T> Data { get; init; } = new();
    public Guid? NextCursor { get; init; }
    public bool HasMore { get; init; }
    public int PageSize { get; init; }
}
