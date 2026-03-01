using System;
using KCharge.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KCharge.Vehicles;

/// <summary>
/// Represents an electric vehicle registered by a user.
/// </summary>
public class Vehicle : FullAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the owner user.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Vehicle make (e.g., "VinFast", "Tesla").
    /// </summary>
    public string Make { get; private set; } = string.Empty;

    /// <summary>
    /// Vehicle model (e.g., "VF e34", "Model 3").
    /// </summary>
    public string Model { get; private set; } = string.Empty;

    /// <summary>
    /// License plate number.
    /// </summary>
    public string? LicensePlate { get; private set; }

    /// <summary>
    /// Vehicle color.
    /// </summary>
    public string? Color { get; private set; }

    /// <summary>
    /// Year of manufacture.
    /// </summary>
    public int? Year { get; private set; }

    /// <summary>
    /// Battery capacity in kWh.
    /// </summary>
    public decimal? BatteryCapacityKwh { get; private set; }

    /// <summary>
    /// Preferred connector type for this vehicle.
    /// </summary>
    public ConnectorType? PreferredConnectorType { get; private set; }

    /// <summary>
    /// Whether this vehicle is active (not deleted).
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Whether this is the user's default vehicle.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// Nickname for the vehicle (e.g., "My VinFast").
    /// </summary>
    public string? Nickname { get; private set; }

    protected Vehicle()
    {
        // Required by EF Core
    }

    public Vehicle(
        Guid id,
        Guid userId,
        string make,
        string model,
        string? licensePlate = null,
        decimal? batteryCapacityKwh = null,
        ConnectorType? preferredConnectorType = null)
        : base(id)
    {
        UserId = userId;
        SetMakeAndModel(make, model);
        LicensePlate = licensePlate;
        BatteryCapacityKwh = batteryCapacityKwh;
        PreferredConnectorType = preferredConnectorType;
        IsActive = true;
        IsDefault = false;
    }

    public void SetMakeAndModel(string make, string model)
    {
        Make = Check.NotNullOrWhiteSpace(make, nameof(make), maxLength: 100);
        Model = Check.NotNullOrWhiteSpace(model, nameof(model), maxLength: 100);
    }

    public void SetLicensePlate(string? licensePlate)
    {
        LicensePlate = licensePlate;
    }

    public void SetDetails(string? color, int? year, string? nickname)
    {
        Color = color;
        Year = year;
        Nickname = nickname;
    }

    public void SetBatteryCapacity(decimal? batteryCapacityKwh)
    {
        BatteryCapacityKwh = batteryCapacityKwh;
    }

    public void SetPreferredConnectorType(ConnectorType? connectorType)
    {
        PreferredConnectorType = connectorType;
    }

    public void SetAsDefault()
    {
        IsDefault = true;
    }

    public void RemoveDefault()
    {
        IsDefault = false;
    }

    public void Deactivate()
    {
        IsActive = false;
        IsDefault = false;
    }

    public void Reactivate()
    {
        IsActive = true;
    }
}
