using Microsoft.AspNetCore.SignalR;
using Volo.Abp.AspNetCore.SignalR;

namespace KLC.Hubs;

public class ChargingHub : AbpHub
{
    public async Task JoinStationGroup(string chargePointId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"station:{chargePointId}");
    }

    public async Task LeaveStationGroup(string chargePointId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"station:{chargePointId}");
    }

    public async Task JoinDashboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");
    }

    public async Task LeaveDashboard()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "dashboard");
    }
}
