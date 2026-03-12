using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Vehicles;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using VehicleDto = KLC.Driver.Services.VehicleDto;

namespace KLC.BffServices;

/// <summary>
/// Tests for VehicleBffService cache layer behavior.
/// Uses NSubstitute mocks for ICacheService to verify cache interactions.
/// </summary>
[Collection(KLCTestConsts.CollectionDefinitionName)]
public class VehicleBffServiceCacheTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly VehicleBffService _service;

    public VehicleBffServiceCacheTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _cache = Substitute.For<ICacheService>();
        var logger = Substitute.For<ILogger<VehicleBffService>>();
        _service = new VehicleBffService(_dbContext, _cache, logger);
    }

    [Fact]
    public async Task GetVehicles_Should_Return_Cached_Result_On_Cache_Hit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cachedVehicles = new List<VehicleDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Make = "VinFast",
                Model = "VF e34",
                Year = 2024,
                LicensePlate = "30A-12345",
                BatteryCapacityKwh = 42,
                PreferredConnectorType = ConnectorType.CCS2,
                IsDefault = true
            }
        };

        var cacheKey = $"user:{userId}:vehicles";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<VehicleDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(cachedVehicles);

        // Act
        var result = await _service.GetVehiclesAsync(userId);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].Make.ShouldBe("VinFast");
        result[0].Model.ShouldBe("VF e34");
        result[0].IsDefault.ShouldBeTrue();

        await _cache.Received(1).GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<VehicleDto>>>>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetVehicles_Should_Query_DB_On_Cache_Miss()
    {
        // Arrange
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var vehicle = new Vehicle(Guid.NewGuid(), userId, "Tesla", "Model 3", "30B-99999", 60, ConnectorType.CCS2);
            vehicle.SetAsDefault();
            await _dbContext.Vehicles.AddAsync(vehicle);
            await _dbContext.SaveChangesAsync();
        });

        var cacheKey = $"user:{userId}:vehicles";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<VehicleDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                var factory = callInfo.ArgAt<Func<Task<List<VehicleDto>>>>(1);
                return factory();
            });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetVehiclesAsync(userId);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result[0].Make.ShouldBe("Tesla");
            result[0].Model.ShouldBe("Model 3");
            result[0].IsDefault.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task GetDefaultVehicle_Should_Return_Cached_Result_On_Cache_Hit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cachedVehicle = new VehicleDto
        {
            Id = Guid.NewGuid(),
            Make = "VinFast",
            Model = "VF 8",
            IsDefault = true
        };

        var cacheKey = $"user:{userId}:default-vehicle";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<VehicleDto?>>>(), Arg.Any<TimeSpan?>())
            .Returns(cachedVehicle);

        // Act
        var result = await _service.GetDefaultVehicleAsync(userId);

        // Assert
        result.ShouldNotBeNull();
        result!.Make.ShouldBe("VinFast");
        result.IsDefault.ShouldBeTrue();

        await _cache.Received(1).GetOrSetAsync(cacheKey, Arg.Any<Func<Task<VehicleDto?>>>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task AddVehicle_Should_Invalidate_Cache()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.AddVehicleAsync(userId, new AddVehicleRequest
            {
                Make = "VinFast",
                Model = "VF e34",
                LicensePlate = "30A-11111",
                BatteryCapacityKwh = 42,
                PreferredConnectorType = ConnectorType.CCS2
            });

            result.ShouldNotBeNull();
            result.Make.ShouldBe("VinFast");
        });

        // Assert - both vehicle list and default vehicle cache invalidated
        await _cache.Received(1).RemoveAsync($"user:{userId}:vehicles");
        await _cache.Received(1).RemoveAsync($"user:{userId}:default-vehicle");
    }

    [Fact]
    public async Task UpdateVehicle_Should_Invalidate_Cache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var vehicle = new Vehicle(vehicleId, userId, "VinFast", "VF e34", "30A-22222", 42);
            await _dbContext.Vehicles.AddAsync(vehicle);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.UpdateVehicleAsync(userId, vehicleId, new UpdateVehicleRequest
            {
                Nickname = "My VinFast"
            });

            result.ShouldNotBeNull();
            result.Nickname.ShouldBe("My VinFast");
        });

        // Assert
        await _cache.Received(1).RemoveAsync($"user:{userId}:vehicles");
        await _cache.Received(1).RemoveAsync($"user:{userId}:default-vehicle");
    }

    [Fact]
    public async Task DeleteVehicle_Should_Invalidate_Cache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var vehicle = new Vehicle(vehicleId, userId, "VinFast", "VF e34", "30A-33333", 42);
            await _dbContext.Vehicles.AddAsync(vehicle);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            await _service.DeleteVehicleAsync(userId, vehicleId);
        });

        // Assert
        await _cache.Received(1).RemoveAsync($"user:{userId}:vehicles");
        await _cache.Received(1).RemoveAsync($"user:{userId}:default-vehicle");
    }

    [Fact]
    public async Task SetDefaultVehicle_Should_Invalidate_Cache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var vehicle = new Vehicle(vehicleId, userId, "VinFast", "VF e34", "30A-44444", 42);
            await _dbContext.Vehicles.AddAsync(vehicle);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            await _service.SetDefaultVehicleAsync(userId, vehicleId);
        });

        // Assert
        await _cache.Received(1).RemoveAsync($"user:{userId}:vehicles");
        await _cache.Received(1).RemoveAsync($"user:{userId}:default-vehicle");
    }

    [Fact]
    public async Task GetVehicles_Should_Use_Correct_Cache_Key_Format()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedCacheKey = $"user:{userId}:vehicles";

        _cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<Task<List<VehicleDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(new List<VehicleDto>());

        // Act
        await _service.GetVehiclesAsync(userId);

        // Assert
        await _cache.Received(1).GetOrSetAsync(
            expectedCacheKey,
            Arg.Any<Func<Task<List<VehicleDto>>>>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetVehicle_Should_Bypass_Cache()
    {
        // Arrange - GetVehicle (single) does NOT use cache, queries DB directly
        var userId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var vehicle = new Vehicle(vehicleId, userId, "Tesla", "Model Y", "30C-55555", 75);
            await _dbContext.Vehicles.AddAsync(vehicle);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetVehicleAsync(userId, vehicleId);

            // Assert
            result.ShouldNotBeNull();
            result!.Make.ShouldBe("Tesla");
        });

        // Verify cache was NOT called for single vehicle lookup
        await _cache.DidNotReceive().GetOrSetAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<VehicleDto?>>>(),
            Arg.Any<TimeSpan?>());
    }
}
