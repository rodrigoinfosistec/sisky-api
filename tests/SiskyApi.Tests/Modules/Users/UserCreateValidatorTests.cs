using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using SiskyApi.Modules.Users.DTOs;
using SiskyApi.Modules.Users.Validators;
using SiskyApi.Shared.Data;
using SiskyApi.Shared.Models;
using SiskyApi.Tests.Shared;

namespace SiskyApi.Tests.Modules.Users;

public class UserCreateValidatorTests
{
    private Mock<IAppDbContext> CreateMockContext(bool emailExists = false)
    {
        var mockContext = new Mock<IAppDbContext>();

        var users = emailExists
            ? new List<User> { new User { Email = "existente@email.com" } }.AsQueryable()
            : new List<User>().AsQueryable();

        var mockSet = new Mock<DbSet<User>>();
        mockSet.As<IAsyncEnumerable<User>>()
            .Setup(m => m.GetAsyncEnumerator(default))
            .Returns(new TestAsyncEnumerator<User>(users.GetEnumerator()));

        mockSet.As<IQueryable<User>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<User>(users.Provider));

        mockSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(users.Expression);
        mockSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(users.ElementType);
        mockSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());

        mockContext.Setup(c => c.Users).Returns(mockSet.Object);

        return mockContext;
    }

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldFail()
    {
        var mockContext = CreateMockContext();
        var validator = new UserCreateValidator(mockContext.Object);
        var dto = new UserCreateDto { Name = "", Email = "novo@email.com", Password = "senha123" };

        var result = await validator.ValidateAsync(dto);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WhenEmailIsInvalid_ShouldFail()
    {
        var mockContext = CreateMockContext();
        var validator = new UserCreateValidator(mockContext.Object);
        var dto = new UserCreateDto { Name = "Nome Válido", Email = "emailinvalido", Password = "senha123" };

        var result = await validator.ValidateAsync(dto);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_WhenPasswordIsTooShort_ShouldFail()
    {
        var mockContext = CreateMockContext();
        var validator = new UserCreateValidator(mockContext.Object);
        var dto = new UserCreateDto { Name = "Nome Válido", Email = "novo@email.com", Password = "123" };

        var result = await validator.ValidateAsync(dto);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Validate_WhenEmailAlreadyExists_ShouldFail()
    {
        var mockContext = CreateMockContext(emailExists: true);
        var validator = new UserCreateValidator(mockContext.Object);
        var dto = new UserCreateDto { Name = "Nome Válido", Email = "existente@email.com", Password = "senha123" };

        var result = await validator.ValidateAsync(dto);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_WhenAllFieldsAreValid_ShouldPass()
    {
        var mockContext = CreateMockContext(emailExists: false);
        var validator = new UserCreateValidator(mockContext.Object);
        var dto = new UserCreateDto { Name = "Nome Válido", Email = "novo@email.com", Password = "senha123" };

        var result = await validator.ValidateAsync(dto);

        result.IsValid.ShouldBeTrue();
    }
}