using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KCharge.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace KCharge.Stations;

[Authorize(KChargePermissions.Connectors.Default)]
public class ConnectorAppService : KChargeAppService, IConnectorAppService
{
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<Connector, Guid> _connectorRepository;
    private readonly StationMapper _mapper;

    public ConnectorAppService(
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<Connector, Guid> connectorRepository)
    {
        _stationRepository = stationRepository;
        _connectorRepository = connectorRepository;
        _mapper = new StationMapper();
    }

    [Authorize(KChargePermissions.Connectors.Create)]
    public async Task<ConnectorDto> CreateAsync(Guid stationId, CreateConnectorDto input)
    {
        var query = await _stationRepository.WithDetailsAsync(s => s.Connectors);
        var station = await AsyncExecuter.FirstOrDefaultAsync(query.Where(s => s.Id == stationId));

        if (station == null)
        {
            throw new BusinessException("MOD_002_001")
                .WithData("stationId", stationId);
        }

        // Check for duplicate connector number
        if (station.Connectors.Any(c => c.ConnectorNumber == input.ConnectorNumber))
        {
            throw new BusinessException("MOD_002_002")
                .WithData("connectorNumber", input.ConnectorNumber);
        }

        var connector = station.AddConnector(
            GuidGenerator.Create(),
            input.ConnectorNumber,
            input.ConnectorType,
            input.MaxPowerKw
        );

        await _stationRepository.UpdateAsync(station);

        return _mapper.ToDto(connector);
    }

    [Authorize(KChargePermissions.Connectors.Update)]
    public async Task<ConnectorDto> UpdateAsync(Guid id, UpdateConnectorDto input)
    {
        var connector = await _connectorRepository.GetAsync(id);

        connector.SetMaxPower(input.MaxPowerKw);
        // ConnectorType update would require domain method if we want to allow it

        await _connectorRepository.UpdateAsync(connector);

        return _mapper.ToDto(connector);
    }

    public async Task<ConnectorDto> GetAsync(Guid id)
    {
        var connector = await _connectorRepository.GetAsync(id);
        return _mapper.ToDto(connector);
    }

    public async Task<List<ConnectorDto>> GetListByStationAsync(Guid stationId)
    {
        var query = await _stationRepository.WithDetailsAsync(s => s.Connectors);
        var station = await AsyncExecuter.FirstOrDefaultAsync(query.Where(s => s.Id == stationId));

        if (station == null)
        {
            throw new BusinessException("MOD_002_001")
                .WithData("stationId", stationId);
        }

        return station.Connectors
            .Select(c => _mapper.ToDto(c))
            .OrderBy(c => c.ConnectorNumber)
            .ToList();
    }

    [Authorize(KChargePermissions.Connectors.Enable)]
    public async Task EnableAsync(Guid id)
    {
        var connector = await _connectorRepository.GetAsync(id);
        connector.Enable();
        await _connectorRepository.UpdateAsync(connector);
    }

    [Authorize(KChargePermissions.Connectors.Disable)]
    public async Task DisableAsync(Guid id)
    {
        var connector = await _connectorRepository.GetAsync(id);

        // Check for active session would go here (MOD_002_003)

        connector.Disable();
        await _connectorRepository.UpdateAsync(connector);
    }

    [Authorize(KChargePermissions.Connectors.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await _connectorRepository.DeleteAsync(id);
    }
}
