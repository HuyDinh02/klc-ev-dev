using System;
using Volo.Abp.Application.Services;

namespace KLC.ChargingStations;

public interface IChargingStationAppService : ICrudAppService<ChargingStationDto, Guid, PagedAndSortedResultRequestDto, CreateChargingStationDto>
{
}
