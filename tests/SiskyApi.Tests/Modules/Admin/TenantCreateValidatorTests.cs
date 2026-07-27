using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using SiskyApi.Modules.Admin.DTOs;
using SiskyApi.Modules.Admin.Validators;
using SiskyApi.Shared.Data;
using SiskyApi.Shared.Models;
using SiskyApi.Tests.Shared;

namespace SiskyApi.Tests.Modules.Admin;

public class TenantCreateValidatorTests
{
    private Mock<IAppDbContext> CreateMockContext(bool subdomainExists = false)
    {
        var mockContext = new Mock<IAppDbContext>();

        var tenants = subdomainExists
            ? new List<Tenant> { new Tenant { Subdomain = "existente" } }.AsQueryable()
            : new List<Tenant>().AsQueryable();

        var mockSet = new Mock<DbSet<Tenant>>();
        mockSet.As<IAsyncEnumerable<Tenant>>()
            .Setup(m => m.GetAsyncEnumerator(default))
            .Returns(new TestAsyncEnumerator<Tenant>(tenants.GetEnumerator()));

        mockSet.As<IQueryable<Tenant>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<Tenant>(tenants.Provider));

        mockSet.As<IQueryable<Tenant>>().Setup(m => m.Expression).Returns(tenants.Expression);
        mockSet.As<IQueryable<Tenant>>().Setup(m => m.ElementType).Returns(tenants.ElementType);
        mockSet.As<IQueryable<Tenant>>().Setup(m => m.GetEnumerator()).Returns(tenants.GetEnumerator());

        mockContext.Setup(c => c.Tenants).Returns(mockSet.Object);

        return mockContext;
    }

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldFail()
    {
        var mockContext = CreateMockContext();
        var validator = new TenantCreateValidator(mockContext.Object);
        var dto = new TenantCreateDto { Name = "", Subdomain = "valido" };

        var result = await validator.ValidateAsync(dto);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WhenNameIsTooShort_ShouldFail()
    {
        var mockContext = CreateMockContext();
        var validator = new TenantCreateValidator(mockContext.Object);
        var dto = new TenantCreateDto { Name = "ab", Subdomain = "valido" };

        var result = await validator.ValidateAsync(dto);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WhenSubdomainIsEmpty_ShouldFail()
    {
        var mockContext = CreateMockContext();
        var validator = new TenantCreateValidator(mockContext.Object);
        var dto = new TenantCreateDto { Name = "Tenant Válido", Subdomain = "" };

        var result = await validator.ValidateAsync(dto);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Subdomain");
    }

    [Fact]
    public async Task Validate_WhenSubdomainHasInvalidChars_ShouldFail()
    {
        var mockContext = CreateMockContext();
        var validator = new TenantCreateValidator(mockContext.Object);
        var dto = new TenantCreateDto { Name = "Tenant Válido", Subdomain = "sub dominio!" };

        var result = await validator.ValidateAsync(dto);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Subdomain");
    }

    [Fact]
    public async Task Validate_WhenSubdomainAlreadyExists_ShouldFail()
    {
        var mockContext = CreateMockContext(subdomainExists: true);
        var validator = new TenantCreateValidator(mockContext.Object);
        var dto = new TenantCreateDto { Name = "Tenant Válido", Subdomain = "existente" };

        var result = await validator.ValidateAsync(dto);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Subdomain");
    }

    [Fact]
    public async Task Validate_WhenAllFieldsAreValid_ShouldPass()
    {
        var mockContext = CreateMockContext(subdomainExists: false);
        var validator = new TenantCreateValidator(mockContext.Object);
        var dto = new TenantCreateDto { Name = "Tenant Válido", Subdomain = "novo-tenant" };

        var result = await validator.ValidateAsync(dto);

        result.IsValid.ShouldBeTrue();
    }
}