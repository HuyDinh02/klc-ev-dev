using Riok.Mapperly.Abstractions;

namespace KCharge.Stations;

[Mapper]
public partial class StationMapper
{
    public partial StationDto ToDto(ChargingStation source);
    public partial StationListDto ToListDto(ChargingStation source);
    public partial ConnectorDto ToDto(Connector source);
    public partial ConnectorListDto ToListDto(Connector source);
}
