// single using for Http types
using Microsoft.AspNetCore.Mvc;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using IdentityService.Api.DTOs;
using Microsoft.AspNetCore.Http;
using IdentityService.Api.Controllers;
using Microsoft.Extensions.Logging;

namespace IdentityService.Tests.Controllers;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Register_ReturnsAccepted_WhenUserCreatedSuccessfully()
    {
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var userService = new Mock<Application.Interfaces.IUserService>();
        userService.Setup(u => u.CreateAsync("user@example.com", "Password123!"))
            .ReturnsAsync((true, Array.Empty<string>(), userId));
        userService.Setup(u => u.GenerateEmailConfirmationTokenAsync(userId))
            .ReturnsAsync("token-123");

        var emailSender = new Mock<Infrastructure.Email.IEmailSender>();

        var controller = BuildController(userService.Object, emailSender.Object);

        var dto = new RegisterDto("user@example.com", "Password123!");

        var result = await controller.Register(dto);

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.NotNull(accepted.Value);

        userService.Verify(m => m.CreateAsync(dto.Email, dto.Password), Times.Once);
        userService.Verify(m => m.GenerateEmailConfirmationTokenAsync(userId), Times.Once);
        emailSender.Verify(s => s.SendAsync(dto.Email, It.IsAny<string>(), It.Is<string>(body => body.Contains("https://example.com/api/auth/confirm-email"))), Times.Once);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenUserCreationFails()
    {
        var userService = new Mock<Application.Interfaces.IUserService>();
        userService.Setup(u => u.CreateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((false, new[] { "Email already used" }, null));

        var emailSender = new Mock<Infrastructure.Email.IEmailSender>();
        var controller = BuildController(userService.Object, emailSender.Object);
        var dto = new RegisterDto("user@example.com", "Password123!");

        var result = await controller.Register(dto);

        Assert.IsType<BadRequestObjectResult>(result);
        emailSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserMissing()
    {
        var userService = new Mock<Application.Interfaces.IUserService>();
        userService.Setup(u => u.FindByEmailAsync("missing@example.com")).ReturnsAsync(((Guid?)null, (string?)null));

        var controller = BuildController(userService.Object, Mock.Of<Infrastructure.Email.IEmailSender>());

        var result = await controller.Login(new LoginDto("missing@example.com", "whatever"));

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsForbid_WhenEmailNotConfirmed()
    {
        var email = "user@example.com";
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var userService = new Mock<Application.Interfaces.IUserService>();
        userService.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync((userId, email));
        userService.Setup(u => u.IsEmailConfirmedAsync(userId)).ReturnsAsync(false);

        var controller = BuildController(userService.Object, Mock.Of<Infrastructure.Email.IEmailSender>());

        var result = await controller.Login(new LoginDto(email, "Password123!"));

        Assert.IsType<ForbidResult>(result);
        userService.Verify(u => u.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_ReturnsOk_WhenCredentialsValid()
    {
        var email = "user@example.com";
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var user = new Domain.User
        {
            Id = userId,
            Email = email,
            EmailConfirmed = true,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        var userService = new Mock<Application.Interfaces.IUserService>();
        userService.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync((userId, email));
        userService.Setup(u => u.IsEmailConfirmedAsync(userId)).ReturnsAsync(true);
        userService.Setup(u => u.ValidateCredentialsAsync(email, "Password123!")).ReturnsAsync(user);

        var controller = BuildController(userService.Object, Mock.Of<Infrastructure.Email.IEmailSender>());

        var result = await controller.Login(new LoginDto(email, "Password123!"));

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordInvalid()
    {
        var email = "user@example.com";
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var userService = new Mock<Application.Interfaces.IUserService>();
        userService.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync((userId, email));
        userService.Setup(u => u.IsEmailConfirmedAsync(userId)).ReturnsAsync(true);
        userService.Setup(u => u.ValidateCredentialsAsync(email, "WrongPassword!")).ReturnsAsync((Domain.User?)null);

        var controller = BuildController(userService.Object, Mock.Of<Infrastructure.Email.IEmailSender>());

        var result = await controller.Login(new LoginDto(email, "WrongPassword!"));

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static AuthController BuildController(Application.Interfaces.IUserService users, Infrastructure.Email.IEmailSender email)
    {
        var httpContext = new DefaultHttpContext();
        // UrlHelper expects HttpContext.RequestServices to be non-null; provide a service provider with a minimal IRouter
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.AspNetCore.Routing.IRouter>(new TestFakeRouter());
        httpContext.RequestServices = services.BuildServiceProvider();

        var linkGeneratorMock = new Mock<Application.Interfaces.IConfirmationLinkGenerator>();
        linkGeneratorMock.Setup(l => l.GenerateEmailConfirmationLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string, string, string>((id, token, scheme, host) => $"{scheme}://{host}/api/auth/confirm-email?userId={id}&token={token}");

        var jwtServiceMock = new Mock<Infrastructure.Services.IJwtService>();
        jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<Domain.User>()))
            .Returns("fake-jwt-token");

        var controller = new AuthController(users, jwtServiceMock.Object, email, linkGeneratorMock.Object, Mock.Of<ILogger<AuthController>>())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };

        controller.Request.Scheme = "https";
        controller.Request.Host = new HostString("example.com");

        return controller;
    }

    private sealed class TestFakeRouter : Microsoft.AspNetCore.Routing.IRouter
    {
        public Microsoft.AspNetCore.Routing.VirtualPathData? GetVirtualPath(Microsoft.AspNetCore.Routing.VirtualPathContext context) => null;

        public Task RouteAsync(Microsoft.AspNetCore.Routing.RouteContext context)
        {
            return Task.CompletedTask;
        }
    }



}
