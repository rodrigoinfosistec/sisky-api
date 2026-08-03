using Shouldly;
using SiskyApi.Shared.Authorization;

namespace SiskyApi.Tests.Shared;

public class PermissionsConfigTests
{
    [Fact]
    public void Modules_ShouldHaveSixModules()
    {
        PermissionsConfig.Modules.Length.ShouldBe(6);
    }

    [Fact]
    public void Modules_UsersShouldBeCore()
    {
        var users = PermissionsConfig.Modules.FirstOrDefault(m => m.Slug == "users");
        users.ShouldNotBeNull();
        users.IsCore.ShouldBeTrue();
    }

    [Fact]
    public void Modules_AuditShouldBeCore()
    {
        var audit = PermissionsConfig.Modules.FirstOrDefault(m => m.Slug == "audit");
        audit.ShouldNotBeNull();
        audit.IsCore.ShouldBeTrue();
    }

    [Fact]
    public void Modules_InventoryShouldNotBeCore()
    {
        var inventory = PermissionsConfig.Modules.FirstOrDefault(m => m.Slug == "inventory");
        inventory.ShouldNotBeNull();
        inventory.IsCore.ShouldBeFalse();
    }

    [Fact]
    public void Modules_FiscalShouldNotBeCore()
    {
        var fiscal = PermissionsConfig.Modules.FirstOrDefault(m => m.Slug == "fiscal");
        fiscal.ShouldNotBeNull();
        fiscal.IsCore.ShouldBeFalse();
    }

    [Fact]
    public void Modules_TimeclockShouldNotBeCore()
    {
        var timeclock = PermissionsConfig.Modules.FirstOrDefault(m => m.Slug == "timeclock");
        timeclock.ShouldNotBeNull();
        timeclock.IsCore.ShouldBeFalse();
    }

    [Fact]
    public void Modules_SlugsShouldBeLowercase()
    {
        foreach (var module in PermissionsConfig.Modules)
        {
            module.Slug.ShouldBe(module.Slug.ToLower());
        }
    }

    [Fact]
    public void All_ShouldContainUsersViewPermission()
    {
        PermissionsConfig.All.ShouldContain("users.view");
    }

    [Fact]
    public void All_ShouldContainAuditViewPermission()
    {
        PermissionsConfig.All.ShouldContain("audit.view");
    }

    [Fact]
    public void All_SlugsShouldFollowPattern()
    {
        // Todos os slugs devem seguir o padrão "modulo.acao"
        foreach (var slug in PermissionsConfig.All)
        {
            var parts = slug.Split('.');
            parts.Length.ShouldBe(2);
            parts[0].ShouldNotBeNullOrEmpty();
            parts[1].ShouldNotBeNullOrEmpty();

        }
    }

    [Fact]
    public void Modules_IoTShouldNotBeCore()
    {
        var iot = PermissionsConfig.Modules.FirstOrDefault(m => m.Slug == "iot");
        iot.ShouldNotBeNull();
        iot.IsCore.ShouldBeFalse();
    }
}