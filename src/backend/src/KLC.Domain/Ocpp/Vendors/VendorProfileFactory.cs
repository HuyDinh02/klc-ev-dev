using System.Collections.Generic;
using System.Linq;
using KLC.Enums;
using Microsoft.Extensions.Logging;

namespace KLC.Ocpp.Vendors;

/// <summary>
/// Resolves the correct <see cref="IVendorProfile"/> for a charger
/// based on vendor/model strings from BootNotification or pre-registered mapping.
/// </summary>
public class VendorProfileFactory
{
    private readonly IEnumerable<IVendorProfile> _profiles;
    private readonly IVendorProfile _genericProfile;
    private readonly ILogger<VendorProfileFactory> _logger;

    public VendorProfileFactory(
        IEnumerable<IVendorProfile> profiles,
        ILogger<VendorProfileFactory> logger)
    {
        _profiles = profiles;
        _logger = logger;
        _genericProfile = profiles.First(p => p.ProfileType == VendorProfileType.Generic);
    }

    /// <summary>
    /// Auto-detect vendor profile from BootNotification vendor/model strings.
    /// Returns the matching profile or GenericProfile as default.
    /// </summary>
    public IVendorProfile Detect(string? chargePointVendor, string? chargePointModel)
    {
        foreach (var profile in _profiles)
        {
            if (profile.ProfileType == VendorProfileType.Generic)
                continue;

            if (profile.MatchesVendor(chargePointVendor, chargePointModel))
            {
                _logger.LogInformation(
                    "Detected vendor profile {ProfileType} for vendor='{Vendor}', model='{Model}'",
                    profile.ProfileType, chargePointVendor, chargePointModel);
                return profile;
            }
        }

        _logger.LogDebug(
            "No vendor profile matched for vendor='{Vendor}', model='{Model}' — using Generic",
            chargePointVendor, chargePointModel);
        return _genericProfile;
    }

    /// <summary>
    /// Resolve vendor profile by pre-registered type (stored on ChargingStation entity).
    /// </summary>
    public IVendorProfile Resolve(VendorProfileType profileType)
    {
        var profile = _profiles.FirstOrDefault(p => p.ProfileType == profileType);
        if (profile != null)
            return profile;

        _logger.LogWarning("Unknown VendorProfileType {ProfileType}, falling back to Generic", profileType);
        return _genericProfile;
    }
}
