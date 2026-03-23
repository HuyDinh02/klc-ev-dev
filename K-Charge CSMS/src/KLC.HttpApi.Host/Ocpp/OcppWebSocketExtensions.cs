using Microsoft.AspNetCore.Builder;

namespace KLC.Ocpp;

/// <summary>
/// Extension methods for configuring OCPP WebSocket middleware.
/// </summary>
public static class OcppWebSocketExtensions
{
    /// <summary>
    /// Register OCPP WebSocket middleware to handle charge point connections.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="path">The path to listen on (default: /ocpp)</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseOcppWebSocket(this IApplicationBuilder app, string path = "/ocpp")
    {
        app.UseWebSockets();
        app.UseMiddleware<OcppWebSocketMiddleware>(path);
        return app;
    }
}
