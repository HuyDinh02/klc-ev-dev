using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace KLC.Controllers;

/// <summary>
/// Proxies OCPP management requests from Admin Portal to the OCPP Gateway.
/// Admin Portal authenticates with OpenIddict → Admin API validates → proxies to Gateway with internal key.
/// This avoids exposing the internal API key to the browser.
/// </summary>
[ApiController]
[Route("api/v1/ocpp-proxy")]
[Authorize(KLCPermissions.Monitoring.Default)]
public class OcppGatewayProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public OcppGatewayProxyController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    /// <summary>
    /// Get connected chargers from OCPP Gateway.
    /// </summary>
    [HttpGet("connections")]
    public async Task<ActionResult> GetConnections()
    {
        return await ProxyGet("/api/v1/ocpp/connections");
    }

    /// <summary>
    /// Get connection detail for a specific charger.
    /// </summary>
    [HttpGet("connections/{chargePointId}")]
    public async Task<ActionResult> GetConnection(string chargePointId)
    {
        return await ProxyGet($"/api/v1/ocpp/connections/{chargePointId}");
    }

    private async Task<ActionResult> ProxyGet(string path)
    {
        var gatewayUrl = _configuration["Ocpp:GatewayUrl"];
        if (string.IsNullOrEmpty(gatewayUrl))
        {
            // Fallback: query local OcppConnectionManager (when Gateway not separated)
            return NotFound("OCPP Gateway URL not configured");
        }

        var client = _httpClientFactory.CreateClient();
        var internalKey = _configuration["Internal:ApiKey"];
        if (!string.IsNullOrEmpty(internalKey))
        {
            client.DefaultRequestHeaders.Add("X-Internal-Key", internalKey);
        }

        // Route to internal API (AllowAnonymous + X-Internal-Key)
        var internalPath = path.Replace("/api/v1/ocpp/", "/api/internal/ocpp/");
        var response = await client.GetAsync($"{gatewayUrl}{internalPath}");
        var content = await response.Content.ReadAsStringAsync();

        return Content(content, "application/json");
    }
}
