using System;
using KLC.Enums;

namespace KLC.Ocpp;

/// <summary>
/// Resolves the correct OCPP message parser based on the connection's negotiated protocol version.
/// </summary>
public class OcppMessageParserFactory
{
    private readonly OcppV16MessageParser _v16Parser = new();

    /// <summary>
    /// Get the message parser for the given OCPP protocol version.
    /// </summary>
    public IOcppMessageParser GetParser(OcppProtocolVersion version)
    {
        return version switch
        {
            OcppProtocolVersion.Ocpp16J => _v16Parser,
            OcppProtocolVersion.Ocpp201 => throw new NotSupportedException(
                "OCPP 2.0.1 message parser is not yet implemented. " +
                "Phase 2 will add OcppV201MessageParser."),
            _ => _v16Parser
        };
    }
}
