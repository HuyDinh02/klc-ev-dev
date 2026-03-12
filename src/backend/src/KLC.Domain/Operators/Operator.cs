using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Operators;

/// <summary>
/// Represents a B2B external operator that can manage stations and sessions via API.
/// Aggregate root for the Operators bounded context.
/// </summary>
public class Operator : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Display name of the operator.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// SHA256 hash of the API key used for authentication.
    /// </summary>
    public string ApiKeyHash { get; private set; } = string.Empty;

    /// <summary>
    /// Contact email for the operator.
    /// </summary>
    public string ContactEmail { get; private set; } = string.Empty;

    /// <summary>
    /// URL to send webhook notifications to.
    /// </summary>
    public string? WebhookUrl { get; private set; }

    /// <summary>
    /// Whether this operator account is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Maximum API requests per minute for this operator.
    /// </summary>
    public int RateLimitPerMinute { get; private set; }

    /// <summary>
    /// Optional description of the operator.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Stations this operator has access to.
    /// </summary>
    public ICollection<OperatorStation> AllowedStations { get; private set; } = new List<OperatorStation>();

    protected Operator()
    {
        // Required by EF Core
    }

    public Operator(
        Guid id,
        string name,
        string apiKeyHash,
        string contactEmail,
        string? description = null,
        int rateLimitPerMinute = 1000)
        : base(id)
    {
        SetName(name);
        SetApiKeyHash(apiKeyHash);
        ContactEmail = Check.NotNullOrWhiteSpace(contactEmail, nameof(contactEmail), maxLength: 200);
        Description = description;
        RateLimitPerMinute = rateLimitPerMinute > 0 ? rateLimitPerMinute : 1000;
        IsActive = true;
    }

    public void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 200);
    }

    public void SetApiKeyHash(string hash)
    {
        ApiKeyHash = Check.NotNullOrWhiteSpace(hash, nameof(hash), maxLength: 128);
    }

    public void SetContactEmail(string email)
    {
        ContactEmail = Check.NotNullOrWhiteSpace(email, nameof(email), maxLength: 200);
    }

    public void SetWebhookUrl(string? webhookUrl)
    {
        WebhookUrl = webhookUrl;
    }

    public void SetRateLimit(int rateLimitPerMinute)
    {
        RateLimitPerMinute = rateLimitPerMinute > 0 ? rateLimitPerMinute : 1000;
    }

    public void SetDescription(string? description)
    {
        Description = description;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public OperatorStation AddStation(Guid operatorStationId, Guid stationId)
    {
        if (AllowedStations.Any(s => s.StationId == stationId && !s.IsDeleted))
            throw new BusinessException(KLCDomainErrorCodes.Operators.StationAlreadyAssigned);

        var operatorStation = new OperatorStation(operatorStationId, Id, stationId);
        AllowedStations.Add(operatorStation);
        return operatorStation;
    }

    public void RemoveStation(Guid stationId)
    {
        var operatorStation = AllowedStations.FirstOrDefault(s => s.StationId == stationId && !s.IsDeleted);
        if (operatorStation == null)
            throw new BusinessException(KLCDomainErrorCodes.Operators.StationNotAssigned);

        operatorStation.MarkAsDeleted();
    }

    public bool HasStationAccess(Guid stationId)
    {
        return AllowedStations.Any(s => s.StationId == stationId && !s.IsDeleted);
    }
}
