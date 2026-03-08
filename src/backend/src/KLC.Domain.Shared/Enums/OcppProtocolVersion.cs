namespace KLC.Enums;

/// <summary>
/// OCPP protocol version negotiated during WebSocket handshake.
/// </summary>
public enum OcppProtocolVersion
{
    /// <summary>OCPP 1.6 JSON (subprotocol: ocpp1.6)</summary>
    Ocpp16J = 0,

    /// <summary>OCPP 2.0.1 JSON (subprotocol: ocpp2.0.1)</summary>
    Ocpp201 = 1
}
