using System.Threading.Tasks;
using KLC.Enums;
using KLC.Notifications;
using Shouldly;
using Volo.Abp.Modularity;
using Xunit;

namespace KLC.Notifications;

public abstract class BroadcastAppServiceTests<TStartupModule> : KLCApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IBroadcastAppService _broadcastAppService;

    protected BroadcastAppServiceTests()
    {
        _broadcastAppService = GetRequiredService<IBroadcastAppService>();
    }

    [Fact]
    public async Task Should_Get_Empty_Broadcast_History()
    {
        var history = await _broadcastAppService.GetBroadcastHistoryAsync(
            new GetBroadcastHistoryDto { PageSize = 10 });

        history.ShouldNotBeNull();
        history.Count.ShouldBe(0);
    }

}
