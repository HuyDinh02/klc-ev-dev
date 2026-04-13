using System;
using System.Threading.Tasks;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.TestDoubles;
using KLC.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.BffServices;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class VehicleBffServiceTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly VehicleBffService _service;

    public VehicleBffServiceTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        var cache = new PassthroughCacheService();
        var logger = Substitute.For<ILogger<VehicleBffService>>();
        _service = new VehicleBffService(_dbContext, cache, logger);
    }

    [Fact]
    public async Task AddVehicle_Should_Create_And_Set_Default_If_First()
    {
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.AddVehicleAsync(userId, new AddVehicleRequest
            {
                Make = "VinFast",
                Model = "VF 8",
                Year = 2025,
                LicensePlate = "30A-12345",
                BatteryCapacityKwh = 87.7m,
                PreferredConnectorType = ConnectorType.CCS2
            });

            result.ShouldNotBeNull();
            result.Make.ShouldBe("VinFast");
            result.Model.ShouldBe("VF 8");
            result.IsDefault.ShouldBeTrue(); // First vehicle = default
        });
    }

    [Fact]
    public async Task AddVehicle_Should_Not_Set_Default_If_Not_First()
    {
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var existing = new Vehicle(Guid.NewGuid(), userId, "Tesla", "Model 3", "30B-99999");
            existing.SetAsDefault();
            await _dbContext.Vehicles.AddAsync(existing);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.AddVehicleAsync(userId, new AddVehicleRequest
            {
                Make = "VinFast",
                Model = "VF 9"
            });

            result.IsDefault.ShouldBeFalse(); // Not first
        });
    }

    [Fact]
    public async Task GetVehicles_Should_Return_Active_Vehicles()
    {
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var v1 = new Vehicle(Guid.NewGuid(), userId, "VinFast", "VF 8");
            v1.SetAsDefault();
            var v2 = new Vehicle(Guid.NewGuid(), userId, "Tesla", "Model Y");
            var v3 = new Vehicle(Guid.NewGuid(), userId, "Hyundai", "Ioniq 5");
            v3.Deactivate(); // Soft deleted
            await _dbContext.Vehicles.AddRangeAsync(v1, v2, v3);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetVehiclesAsync(userId);

            result.Count.ShouldBe(2); // Excludes deactivated
            result[0].IsDefault.ShouldBeTrue(); // Default first
        });
    }

    [Fact]
    public async Task GetVehicle_Should_Return_Specific_Vehicle()
    {
        var userId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _dbContext.Vehicles.AddAsync(
                new Vehicle(vehicleId, userId, "BYD", "Atto 3", "51H-77777"));
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetVehicleAsync(userId, vehicleId);

            result.ShouldNotBeNull();
            result!.Make.ShouldBe("BYD");
            result.LicensePlate.ShouldBe("51H-77777");
        });
    }

    [Fact]
    public async Task GetVehicle_Should_Return_Null_For_Other_User()
    {
        var vehicleId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _dbContext.Vehicles.AddAsync(
                new Vehicle(vehicleId, Guid.NewGuid(), "BMW", "iX3"));
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetVehicleAsync(Guid.NewGuid(), vehicleId);
            result.ShouldBeNull(); // Different user
        });
    }

    [Fact]
    public async Task UpdateVehicle_Should_Modify_Fields()
    {
        var userId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _dbContext.Vehicles.AddAsync(
                new Vehicle(vehicleId, userId, "VinFast", "VF 8", "30A-12345"));
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.UpdateVehicleAsync(userId, vehicleId, new UpdateVehicleRequest
            {
                LicensePlate = "30A-99999",
                Color = "Blue",
                Nickname = "My EV"
            });

            result.LicensePlate.ShouldBe("30A-99999");
            result.Color.ShouldBe("Blue");
            result.Nickname.ShouldBe("My EV");
        });
    }

    [Fact]
    public async Task UpdateVehicle_Should_Throw_When_Not_Found()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var ex = await Should.ThrowAsync<BusinessException>(
                () => _service.UpdateVehicleAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateVehicleRequest()));
            ex.Code.ShouldBe(KLCDomainErrorCodes.EntityNotFound);
        });
    }

    [Fact]
    public async Task DeleteVehicle_Should_Deactivate()
    {
        var userId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _dbContext.Vehicles.AddAsync(
                new Vehicle(vehicleId, userId, "VinFast", "VF 8"));
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            await _service.DeleteVehicleAsync(userId, vehicleId);
        });

        _dbContext.ChangeTracker.Clear();

        await WithUnitOfWorkAsync(async () =>
        {
            var vehicle = await _dbContext.Vehicles.FirstAsync(v => v.Id == vehicleId);
            vehicle.IsActive.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task SetDefaultVehicle_Should_Update_Default()
    {
        var userId = Guid.NewGuid();
        var v1Id = Guid.NewGuid();
        var v2Id = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var v1 = new Vehicle(v1Id, userId, "VinFast", "VF 8");
            v1.SetAsDefault();
            var v2 = new Vehicle(v2Id, userId, "Tesla", "Model 3");
            await _dbContext.Vehicles.AddRangeAsync(v1, v2);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            await _service.SetDefaultVehicleAsync(userId, v2Id);
        });

        _dbContext.ChangeTracker.Clear();

        await WithUnitOfWorkAsync(async () =>
        {
            var v1 = await _dbContext.Vehicles.FirstAsync(v => v.Id == v1Id);
            var v2 = await _dbContext.Vehicles.FirstAsync(v => v.Id == v2Id);
            v1.IsDefault.ShouldBeFalse();
            v2.IsDefault.ShouldBeTrue();
        });
    }

}
