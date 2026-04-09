using System.Text.Json;
using System.Threading.Tasks;
using KLC.Ocpp.Messages;
using Microsoft.Extensions.Logging;

namespace KLC.Ocpp.Handlers;

public class AuthorizeHandler : IOcppActionHandler
{
    private readonly ILogger<AuthorizeHandler> _logger;
    private readonly IOcppService _ocppService;

    public string Action => "Authorize";

    public AuthorizeHandler(
        ILogger<AuthorizeHandler> logger,
        IOcppService ocppService)
    {
        _logger = logger;
        _ocppService = ocppService;
    }

    public async Task<string> HandleAsync(OcppHandlerContext context)
    {
        var idTag = string.Empty;
        if (context.Payload.ValueKind == JsonValueKind.Object && context.Payload.TryGetProperty("idTag", out var idTagElement))
        {
            idTag = idTagElement.GetString() ?? string.Empty;
        }

        var isValid = await _ocppService.ValidateIdTagAsync(idTag);

        _logger.LogInformation("Authorize request for idTag {IdTag}: {Status}", idTag, isValid ? "Accepted" : "Invalid");

        var response = new
        {
            idTagInfo = new IdTagInfo
            {
                Status = isValid ? AuthorizationStatus.Accepted : AuthorizationStatus.Invalid
            }
        };

        return context.Parser.SerializeCallResult(context.UniqueId, response);
    }
}
