using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Permissions;
using NSubstitute;
using Shouldly;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Modularity;
using Xunit;

namespace KLC.UserManagement;

/// <summary>
/// Integration tests for the MyPermissionsController endpoint logic.
/// Validates that the permission retrieval correctly filters KLC group permissions
/// and returns only granted permissions for the current user.
/// </summary>
public abstract class MyPermissionsTests<TStartupModule> : KLCApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;

    protected MyPermissionsTests()
    {
        _permissionDefinitionManager = GetRequiredService<IPermissionDefinitionManager>();
    }

    /// <summary>
    /// Reproduces the controller logic: find KLC group, iterate permissions, check grants.
    /// </summary>
    private static async Task<List<string>> GetMyPermissionsAsync(
        IPermissionDefinitionManager permissionDefinitionManager,
        IPermissionChecker permissionChecker)
    {
        var groups = await permissionDefinitionManager.GetGroupsAsync();
        var klcGroup = groups.FirstOrDefault(g => g.Name == KLCPermissions.GroupName);
        if (klcGroup == null) return new List<string>();

        var allPermissions = klcGroup.GetPermissionsWithChildren();
        var granted = new List<string>();

        foreach (var permission in allPermissions)
        {
            if (await permissionChecker.IsGrantedAsync(permission.Name))
            {
                granted.Add(permission.Name);
            }
        }

        return granted;
    }

    [Fact]
    public async Task Should_Return_Empty_List_When_No_Permissions_Granted()
    {
        // Arrange: mock IPermissionChecker to deny all permissions
        var permissionChecker = Substitute.For<IPermissionChecker>();
        permissionChecker
            .IsGrantedAsync(Arg.Any<string>())
            .Returns(false);

        // Act
        var result = await GetMyPermissionsAsync(_permissionDefinitionManager, permissionChecker);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Return_All_KLC_Permissions_When_All_Granted()
    {
        // Arrange: use AlwaysAllow checker (default in test infra) — resolves from DI
        var permissionChecker = GetRequiredService<IPermissionChecker>();

        // Act
        var result = await GetMyPermissionsAsync(_permissionDefinitionManager, permissionChecker);

        // Assert: should contain all KLC permissions defined in KLCPermissionDefinitionProvider
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();

        // Verify well-known KLC permissions are present
        result.ShouldContain(KLCPermissions.Stations.Default);
        result.ShouldContain(KLCPermissions.Stations.Create);
        result.ShouldContain(KLCPermissions.Stations.Update);
        result.ShouldContain(KLCPermissions.Stations.Delete);
        result.ShouldContain(KLCPermissions.Connectors.Default);
        result.ShouldContain(KLCPermissions.Sessions.Default);
        result.ShouldContain(KLCPermissions.Faults.Default);
        result.ShouldContain(KLCPermissions.Payments.Default);
        result.ShouldContain(KLCPermissions.Monitoring.Default);
        result.ShouldContain(KLCPermissions.UserManagement.Default);
        result.ShouldContain(KLCPermissions.PowerSharing.Default);
        result.ShouldContain(KLCPermissions.Operators.Default);
        result.ShouldContain(KLCPermissions.Fleets.Default);
    }

    [Fact]
    public async Task Should_Only_Return_KLC_Group_Permissions()
    {
        // Arrange: use AlwaysAllow checker — grants everything
        var permissionChecker = GetRequiredService<IPermissionChecker>();

        // Act
        var result = await GetMyPermissionsAsync(_permissionDefinitionManager, permissionChecker);

        // Assert: every returned permission should start with the KLC group prefix
        result.ShouldNotBeEmpty();
        result.ShouldAllBe(p => p.StartsWith(KLCPermissions.GroupName + "."));
    }

    [Fact]
    public async Task Should_Return_Only_Granted_Permissions_When_Partial()
    {
        // Arrange: mock checker to grant only Stations.Default and Faults.Default
        var grantedSet = new HashSet<string>
        {
            KLCPermissions.Stations.Default,
            KLCPermissions.Faults.Default
        };

        var permissionChecker = Substitute.For<IPermissionChecker>();
        permissionChecker
            .IsGrantedAsync(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var permissionName = callInfo.Arg<string>();
                return Task.FromResult(grantedSet.Contains(permissionName));
            });

        // Act
        var result = await GetMyPermissionsAsync(_permissionDefinitionManager, permissionChecker);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(KLCPermissions.Stations.Default);
        result.ShouldContain(KLCPermissions.Faults.Default);
        result.ShouldNotContain(KLCPermissions.Stations.Create);
        result.ShouldNotContain(KLCPermissions.Sessions.Default);
    }

    [Fact]
    public async Task Should_Include_Child_Permissions_When_Granted()
    {
        // Arrange: grant Stations parent + children, but not other groups
        var grantedSet = new HashSet<string>
        {
            KLCPermissions.Stations.Default,
            KLCPermissions.Stations.Create,
            KLCPermissions.Stations.Update,
            KLCPermissions.Stations.Delete,
            KLCPermissions.Stations.Decommission
        };

        var permissionChecker = Substitute.For<IPermissionChecker>();
        permissionChecker
            .IsGrantedAsync(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var permissionName = callInfo.Arg<string>();
                return Task.FromResult(grantedSet.Contains(permissionName));
            });

        // Act
        var result = await GetMyPermissionsAsync(_permissionDefinitionManager, permissionChecker);

        // Assert: should return exactly the 5 Stations permissions
        result.Count.ShouldBe(5);
        result.ShouldContain(KLCPermissions.Stations.Default);
        result.ShouldContain(KLCPermissions.Stations.Create);
        result.ShouldContain(KLCPermissions.Stations.Update);
        result.ShouldContain(KLCPermissions.Stations.Delete);
        result.ShouldContain(KLCPermissions.Stations.Decommission);
    }

    [Fact]
    public async Task KLC_Permission_Group_Should_Exist()
    {
        // Verify the KLC permission group is registered in the definition manager
        var groups = await _permissionDefinitionManager.GetGroupsAsync();

        var klcGroup = groups.FirstOrDefault(g => g.Name == KLCPermissions.GroupName);
        klcGroup.ShouldNotBeNull();
        klcGroup.Name.ShouldBe("KLC");

        var allPermissions = klcGroup.GetPermissionsWithChildren();
        allPermissions.ShouldNotBeEmpty();
    }
}
