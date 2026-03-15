using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Permissions;
using KLC.Stations;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace KLC.Operators;

[Authorize(KLCPermissions.Operators.Default)]
public class OperatorAppService : KLCAppService, IOperatorAppService
{
    private readonly IRepository<Operator, Guid> _operatorRepository;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<OperatorWebhookLog, Guid> _webhookLogRepository;

    public OperatorAppService(
        IRepository<Operator, Guid> operatorRepository,
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<OperatorWebhookLog, Guid> webhookLogRepository)
    {
        _operatorRepository = operatorRepository;
        _stationRepository = stationRepository;
        _webhookLogRepository = webhookLogRepository;
    }

    public async Task<List<OperatorDto>> GetListAsync(GetOperatorListDto input)
    {
        var query = await _operatorRepository.GetQueryableAsync();

        if (input.IsActive.HasValue)
            query = query.Where(o => o.IsActive == input.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(input.Search))
            query = query.Where(o => o.Name.ToLower().Contains(input.Search.ToLower()));

        if (!string.IsNullOrWhiteSpace(input.Cursor) && Guid.TryParse(input.Cursor, out var cursorId))
            query = query.Where(o => o.Id.CompareTo(cursorId) > 0);

        query = query.OrderBy(o => o.Id);

        var operators = await AsyncExecuter.ToListAsync(query.Take(input.PageSize));

        return operators.Select(o => new OperatorDto
        {
            Id = o.Id,
            Name = o.Name,
            ContactEmail = o.ContactEmail,
            WebhookUrl = o.WebhookUrl,
            IsActive = o.IsActive,
            RateLimitPerMinute = o.RateLimitPerMinute,
            Description = o.Description,
            StationCount = o.AllowedStations.Count(s => !s.IsDeleted),
            CreationTime = o.CreationTime
        }).ToList();
    }

    public async Task<OperatorDetailDto> GetAsync(Guid id)
    {
        var op = await _operatorRepository.GetAsync(id);
        return await MapToDetailDtoAsync(op);
    }

    [Authorize(KLCPermissions.Operators.Create)]
    public async Task<CreateOperatorResultDto> CreateAsync(CreateOperatorDto input)
    {
        // Check duplicate name
        var existingQuery = await _operatorRepository.GetQueryableAsync();
        var nameExists = await AsyncExecuter.AnyAsync(
            existingQuery.Where(o => o.Name.ToLower() == input.Name.ToLower()));
        if (nameExists)
            throw new BusinessException(KLCDomainErrorCodes.Operators.DuplicateName)
                .WithData("name", input.Name);

        // Generate API key
        var rawApiKey = GenerateApiKey();
        var apiKeyHash = HashApiKey(rawApiKey);

        var op = new Operator(
            GuidGenerator.Create(),
            input.Name,
            apiKeyHash,
            input.ContactEmail,
            input.Description,
            input.RateLimitPerMinute);

        await _operatorRepository.InsertAsync(op);

        var detailDto = await MapToDetailDtoAsync(op);
        return new CreateOperatorResultDto
        {
            Operator = detailDto,
            ApiKey = rawApiKey
        };
    }

    [Authorize(KLCPermissions.Operators.Update)]
    public async Task<OperatorDetailDto> UpdateAsync(Guid id, UpdateOperatorDto input)
    {
        var op = await _operatorRepository.GetAsync(id);

        // Check duplicate name (excluding self)
        var existingQuery = await _operatorRepository.GetQueryableAsync();
        var nameExists = await AsyncExecuter.AnyAsync(
            existingQuery.Where(o => o.Name.ToLower() == input.Name.ToLower() && o.Id != id));
        if (nameExists)
            throw new BusinessException(KLCDomainErrorCodes.Operators.DuplicateName)
                .WithData("name", input.Name);

        op.SetName(input.Name);
        op.SetContactEmail(input.ContactEmail);
        op.SetWebhookUrl(input.WebhookUrl);
        op.SetDescription(input.Description);
        op.SetRateLimit(input.RateLimitPerMinute);

        await _operatorRepository.UpdateAsync(op);
        return await MapToDetailDtoAsync(op);
    }

    [Authorize(KLCPermissions.Operators.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await _operatorRepository.DeleteAsync(id);
    }

    [Authorize(KLCPermissions.Operators.Update)]
    public async Task<OperatorApiKeyDto> RegenerateApiKeyAsync(Guid id)
    {
        var op = await _operatorRepository.GetAsync(id);

        var rawApiKey = GenerateApiKey();
        var apiKeyHash = HashApiKey(rawApiKey);

        op.SetApiKeyHash(apiKeyHash);
        await _operatorRepository.UpdateAsync(op);

        return new OperatorApiKeyDto { ApiKey = rawApiKey };
    }

    [Authorize(KLCPermissions.Operators.ManageStations)]
    public async Task AddStationAsync(Guid operatorId, Guid stationId)
    {
        var op = await _operatorRepository.GetAsync(operatorId);

        // Verify station exists
        var station = await _stationRepository.FindAsync(stationId);
        if (station == null)
            throw new BusinessException(KLCDomainErrorCodes.Station.NotFound);

        op.AddStation(GuidGenerator.Create(), stationId);
        await _operatorRepository.UpdateAsync(op);
    }

    [Authorize(KLCPermissions.Operators.ManageStations)]
    public async Task RemoveStationAsync(Guid operatorId, Guid stationId)
    {
        var op = await _operatorRepository.GetAsync(operatorId);
        op.RemoveStation(stationId);
        await _operatorRepository.UpdateAsync(op);
    }

    private async Task<OperatorDetailDto> MapToDetailDtoAsync(Operator op)
    {
        var activeStations = op.AllowedStations.Where(s => !s.IsDeleted).ToList();
        var stationDtos = new List<OperatorStationDto>();

        foreach (var os in activeStations)
        {
            var station = await _stationRepository.FindAsync(os.StationId);
            stationDtos.Add(new OperatorStationDto
            {
                StationId = os.StationId,
                StationCode = station?.StationCode ?? string.Empty,
                StationName = station?.Name ?? string.Empty
            });
        }

        return new OperatorDetailDto
        {
            Id = op.Id,
            Name = op.Name,
            ContactEmail = op.ContactEmail,
            WebhookUrl = op.WebhookUrl,
            IsActive = op.IsActive,
            RateLimitPerMinute = op.RateLimitPerMinute,
            Description = op.Description,
            StationCount = activeStations.Count,
            CreationTime = op.CreationTime,
            AllowedStations = stationDtos
        };
    }

    private static string GenerateApiKey()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public async Task<List<OperatorWebhookLogDto>> GetWebhookLogsAsync(Guid operatorId, GetWebhookLogsDto input)
    {
        // Verify operator exists
        await _operatorRepository.GetAsync(operatorId);

        var query = await _webhookLogRepository.GetQueryableAsync();
        query = query.Where(l => l.OperatorId == operatorId);

        if (!string.IsNullOrWhiteSpace(input.EventType) && Enum.TryParse<WebhookEventType>(input.EventType, out var eventType))
            query = query.Where(l => l.EventType == eventType);
        if (input.Success.HasValue)
            query = query.Where(l => l.Success == input.Success.Value);

        if (!string.IsNullOrWhiteSpace(input.Cursor) && Guid.TryParse(input.Cursor, out var cursorId))
            query = query.Where(l => l.Id.CompareTo(cursorId) > 0);

        query = query.OrderByDescending(l => l.CreationTime);

        var logs = await AsyncExecuter.ToListAsync(query.Take(input.PageSize));

        return logs.Select(l => new OperatorWebhookLogDto
        {
            Id = l.Id,
            OperatorId = l.OperatorId,
            EventType = l.EventType.ToString(),
            HttpStatusCode = l.HttpStatusCode,
            Success = l.Success,
            ErrorMessage = l.ErrorMessage,
            AttemptCount = l.AttemptCount,
            CreationTime = l.CreationTime
        }).ToList();
    }

    internal static string HashApiKey(string apiKey)
    {
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
