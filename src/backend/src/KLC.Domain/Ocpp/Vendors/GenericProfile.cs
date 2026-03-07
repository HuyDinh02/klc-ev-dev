using KLC.Enums;
using Microsoft.Extensions.Logging;

namespace KLC.Ocpp.Vendors;

/// <summary>
/// Default OCPP 1.6J profile. Used when no vendor-specific profile matches.
/// Follows strict OCPP 1.6J specification behavior.
/// </summary>
public class GenericProfile : VendorProfileBase
{
    public GenericProfile(ILogger<GenericProfile> logger) : base(logger) { }

    public override VendorProfileType ProfileType => VendorProfileType.Generic;

    public override bool MatchesVendor(string? chargePointVendor, string? chargePointModel) => false;
}
