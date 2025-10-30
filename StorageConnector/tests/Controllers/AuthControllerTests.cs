using System;
using System.Threading.Tasks;
// single using for Http types
using Microsoft.AspNetCore.Mvc;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Contracts.Auth;
using Microsoft.AspNetCore.Http;
using IdentityService.Controllers;
using Infrastructure.Email;
using Xunit;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Tests.Controllers;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Register_ReturnsAccepted_WhenUserCreatedSuccessfully()
    {
        var userService = new Mock<IUserService>();
        userService.Setup(u => u.CreateAsync("user@example.com", "Password123!"))
            .ReturnsAsync((true, Array.Empty<string>(), "user-1"));
        userService.Setup(u => u.GenerateEmailConfirmationTokenAsync("user-1"))
            .ReturnsAsync("token-123");

        var emailSender = new Mock<IEmailSender>();

        var controller = BuildController(userService.Object, emailSender.Object);

        var dto = new RegisterDto("user@example.com", "Password123!");

        var result = await controller.Register(dto);

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.NotNull(accepted.Value);

        userService.Verify(m => m.CreateAsync(dto.Email, dto.Password), Times.Once);
        userService.Verify(m => m.GenerateEmailConfirmationTokenAsync("user-1"), Times.Once);
        emailSender.Verify(s => s.SendAsync(dto.Email, It.IsAny<string>(), It.Is<string>(body => body.Contains("https://example.com/api/auth/confirm-email"))), Times.Once);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenUserCreationFails()
    {
        var userService = new Mock<IUserService>();
        userService.Setup(u => u.CreateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((false, new[] { "Email already used" }, (string?)null));

        var emailSender = new Mock<IEmailSender>();
        var controller = BuildController(userService.Object, emailSender.Object);
        var dto = new RegisterDto("user@example.com", "Password123!");

        var result = await controller.Register(dto);

        Assert.IsType<BadRequestObjectResult>(result);
        emailSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserMissing()
    {
        var userService = new Mock<IUserService>();
        userService.Setup(u => u.FindByEmailAsync("missing@example.com")).ReturnsAsync(((string?)null, (string?)null));

        var controller = BuildController(userService.Object, Mock.Of<IEmailSender>());

        var result = await controller.Login(new LoginDto("missing@example.com", "whatever"));

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsForbid_WhenEmailNotConfirmed()
    {
        var email = "user@example.com";
        var userService = new Mock<IUserService>();
        userService.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(("user-1", email));
        userService.Setup(u => u.IsEmailConfirmedAsync("user-1")).ReturnsAsync(false);

        var controller = BuildController(userService.Object, Mock.Of<IEmailSender>());

        var result = await controller.Login(new LoginDto(email, "Password123!"));

        Assert.IsType<ForbidResult>(result);
        userService.Verify(u => u.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Login_ReturnsOk_WhenCredentialsValid()
    {
        var email = "user@example.com";
        var userService = new Mock<IUserService>();
        userService.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(("user-1", email));
        userService.Setup(u => u.IsEmailConfirmedAsync("user-1")).ReturnsAsync(true);
        userService.Setup(u => u.PasswordSignInAsync(email, "Password123!", true, true)).ReturnsAsync(true);

        var controller = BuildController(userService.Object, Mock.Of<IEmailSender>());

        var result = await controller.Login(new LoginDto(email, "Password123!"));

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordInvalid()
    {
        var email = "user@example.com";
        var userService = new Mock<IUserService>();
        userService.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(("user-1", email));
        userService.Setup(u => u.IsEmailConfirmedAsync("user-1")).ReturnsAsync(true);
        userService.Setup(u => u.PasswordSignInAsync(email, "WrongPassword!", true, true)).ReturnsAsync(false);

        var controller = BuildController(userService.Object, Mock.Of<IEmailSender>());

        var result = await controller.Login(new LoginDto(email, "WrongPassword!"));

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static AuthController BuildController(IUserService users, IEmailSender email)
    {
        var httpContext = new DefaultHttpContext();
        // UrlHelper expects HttpContext.RequestServices to be non-null; provide a service provider with a minimal IRouter
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.AspNetCore.Routing.IRouter>(new TestFakeRouter());
        httpContext.RequestServices = services.BuildServiceProvider();

        var linkGeneratorMock = new Mock<Application.Interfaces.IConfirmationLinkGenerator>();
        linkGeneratorMock.Setup(l => l.GenerateEmailConfirmationLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string, string, string>((id, token, scheme, host) => $"{scheme}://{host}/api/auth/confirm-email?userId={id}&token={token}");

        var controller = new AuthController(users, email, linkGeneratorMock.Object, Mock.Of<ILogger<AuthController>>())
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
