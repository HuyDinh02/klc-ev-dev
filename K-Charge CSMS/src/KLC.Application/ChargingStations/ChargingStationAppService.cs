using System;
using AutoMapper;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using KLC.ChargingStations;

namespace KLC.Application.ChargingStations;

public class ChargingStationAppService : CrudAppService<ChargingStation, ChargingStationDto, Guid, PagedAndSortedResultRequestDto, CreateChargingStationDto>, IChargingStationAppService
{
    private readonly IChargingStationRepository _chargingStationRepository;

    public ChargingStationAppService(
        IRepository<ChargingStation, Guid> repository,
        IChargingStationRepository chargingStationRepository,
        IObjectMapper objectMapper)
        : base(repository, objectMapper)
    {
        _chargingStationRepository = chargingStationRepository;
    }

    public override async System.Threading.Tasks.Task<ChargingStationDto> CreateAsync(CreateChargingStationDto input)
    {
        var entity = ObjectMapper.Map<CreateChargingStationDto, ChargingStation>(input);

        // Add connectors to the station
        if (input.Connectors != null && input.Connectors.Count > 0)
        {
            foreach (var connectorInput in input.Connectors)
            {
                var connector = new Connector
                {
                    ConnectorId = connectorInput.ConnectorId,
                    Type = connectorInput.Type,
                    MaxPowerKw = connectorInput.MaxPowerKw,
                    Status = "Available"
                };
                entity.Connectors.Add(connector);
            }
        }

        var result = await Repository.InsertAsync(entity, autoSave: true);
        return ObjectMapper.Map<ChargingStation, ChargingStationDto>(result);
    }

    public async System.Threading.Tasks.Task<ChargingStationDto> GetByChargePointIdAsync(string chargePointId)
    {
        var entity = await _chargingStationRepository.FindByChargePointIdAsync(chargePointId);
        if (entity == null)
        {
            throw new Volo.Abp.UserFriendlyException($"Charging station with ChargePointId '{chargePointId}' not found.");
        }
        return ObjectMapper.Map<ChargingStation, ChargingStationDto>(entity);
    }
}
