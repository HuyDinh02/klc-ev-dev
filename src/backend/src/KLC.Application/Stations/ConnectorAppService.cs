using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KLC.Ocpp;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace KLC.Stations;

[Authorize(KLCPermissions.Connectors.Default)]
public class ConnectorAppService : KLCAppService, IConnectorAppService
{
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<Connector, Guid> _connectorRepository;
    private readonly IOcppRemoteCommandService _remoteCommandService;
    private readonly StationMapper _mapper;

    public ConnectorAppService(
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<Connector, Guid> connectorRepository,
        IOcppRemoteCommandService remoteCommandService)
    {
        _stationRepository = stationRepository;
        _connectorRepository = connectorRepository;
        _remoteCommandService = remoteCommandService;
        _mapper = new StationMapper();
    }

    [Authorize(KLCPermissions.Connectors.Create)]
    public async Task<ConnectorDto> CreateAsync(Guid stationId, CreateConnectorDto input)
    {
        var query = await _stationRepository.WithDetailsAsync(s => s.Connectors);
        var station = await AsyncExecuter.FirstOrDefaultAsync(query.Where(s => s.Id == stationId));

        if (station == null)
        {
            throw new BusinessException(KLCDomainErrorCodes.Connector.StationNotFound);
        }

        // Check for duplicate connector number
        if (station.Connectors.Any(c => c.ConnectorNumber == input.ConnectorNumber))
        {
            throw new BusinessException(KLCDomainErrorCodes.Connector.DuplicateNumber)
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

    [Authorize(KLCPermissions.Connectors.Update)]
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
            throw new BusinessException(KLCDomainErrorCodes.Connector.StationNotFound);
        }

        return station.Connectors
            .Select(c => _mapper.ToDto(c))
            .OrderBy(c => c.ConnectorNumber)
            .ToList();
    }

    [Authorize(KLCPermissions.Connectors.Enable)]
    public async Task EnableAsync(Guid id)
    {
        var connector = await _connectorRepository.GetAsync(id);
        connector.Enable();
        await _connectorRepository.UpdateAsync(connector);
    }

    [Authorize(KLCPermissions.Connectors.Disable)]
    public async Task DisableAsync(Guid id)
    {
        var connector = await _connectorRepository.GetAsync(id);

        // Check for active session would go here (MOD_002_003)

        connector.Disable();
        await _connectorRepository.UpdateAsync(connector);
    }

    [Authorize(KLCPermissions.Connectors.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await _connectorRepository.DeleteAsync(id);
    }

    [Authorize(KLCPermissions.Connectors.Update)]
    public async Task<ConnectorDto> SetQrCodeAsync(Guid stationId, int connectorNumber, SetConnectorQrCodeDto input)
    {
        var query = await _stationRepository.WithDetailsAsync(s => s.Connectors);
        var station = await AsyncExecuter.FirstOrDefaultAsync(query.Where(s => s.Id == stationId));

        if (station == null)
        {
            throw new BusinessException(KLCDomainErrorCodes.Connector.StationNotFound);
        }

        var connector = station.Connectors.FirstOrDefault(c => c.ConnectorNumber == connectorNumber);
        if (connector == null)
        {
            throw new BusinessException(KLCDomainErrorCodes.Connector.NotFound);
        }

        connector.SetQrCodeData(input.QrCodeData);
        await _connectorRepository.UpdateAsync(connector);

        if (input.SendToCharger && !string.IsNullOrWhiteSpace(input.QrCodeData))
        {
            var dataPayload = JsonSerializer.Serialize(new
            {
                connectorId = connectorNumber,
                qrCodeData = input.QrCodeData
            });

            var result = await _remoteCommandService.SendDataTransferAsync(
                station.StationCode,
                "com.cnchargepoint",
                "SetQRCode",
                dataPayload);

            if (!result.Accepted)
            {
                Logger.LogWarning(
                    "DataTransfer SetQRCode failed for station {StationCode} connector {ConnectorNumber}: {Error}",
                    station.StationCode, connectorNumber, result.ErrorMessage);
            }
        }

        return _mapper.ToDto(connector);
    }
}
