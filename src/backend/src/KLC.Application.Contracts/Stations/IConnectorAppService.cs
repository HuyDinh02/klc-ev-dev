using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace KLC.Stations;

/// <summary>
/// Application service interface for connector management.
/// </summary>
public interface IConnectorAppService : IApplicationService
{
    /// <summary>
    /// Creates a new connector for a station.
    /// </summary>
    Task<ConnectorDto> CreateAsync(Guid stationId, CreateConnectorDto input);

    /// <summary>
    /// Updates an existing connector.
    /// </summary>
    Task<ConnectorDto> UpdateAsync(Guid id, UpdateConnectorDto input);

    /// <summary>
    /// Gets a connector by ID.
    /// </summary>
    Task<ConnectorDto> GetAsync(Guid id);

    /// <summary>
    /// Gets all connectors for a station.
    /// </summary>
    Task<List<ConnectorDto>> GetListByStationAsync(Guid stationId);

    /// <summary>
    /// Enables a connector.
    /// </summary>
    Task EnableAsync(Guid id);

    /// <summary>
    /// Disables a connector.
    /// </summary>
    Task DisableAsync(Guid id);

    /// <summary>
    /// Deletes a connector.
    /// </summary>
    Task DeleteAsync(Guid id);
}
