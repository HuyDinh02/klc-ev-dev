using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KLC.Ocpp.Handlers;

public class DataTransferHandler : IOcppActionHandler
{
    private readonly ILogger<DataTransferHandler> _logger;

    public string Action => "DataTransfer";

    public DataTransferHandler(ILogger<DataTransferHandler> logger)
    {
        _logger = logger;
    }

    public Task<string> HandleAsync(OcppHandlerContext context)
    {
        _logger.LogDebug("DataTransfer received: {Payload}", context.Payload.GetRawText());

        var response = new
        {
            status = "Accepted",
            data = (string?)null
        };

        return Task.FromResult(context.Parser.SerializeCallResult(context.UniqueId, response));
    }
}
