using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StorageConnector.Contracts.Auth;
using StorageConnector.IdentityService.Controllers;
using StorageConnector.Infrastructure.Data;
using StorageConnector.Infrastructure.Email;
using Xunit;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace StorageConnector.Tests.Controllers;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Register_ReturnsAccepted_WhenUserCreatedSuccessfully()
    {
        // Arrange
        var userManager = CreateUserManager();
        userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("token-123");

        var signInManager = CreateSignInManager(userManager);
        var emailSender = new Mock<IEmailSender>();

        var controller = BuildController(userManager.Object, signInManager.Object, emailSender.Object);

        var dto = new RegisterDto("user@example.com", "Password123!");

        // Act
        var result = await controller.Register(dto);

        // Assert
        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.NotNull(accepted.Value);

        userManager.Verify(m => m.CreateAsync(It.Is<ApplicationUser>(u => u.Email == dto.Email), dto.Password), Times.Once);
        userManager.Verify(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()), Times.Once);
        emailSender.Verify(s => s.SendAsync(dto.Email, It.IsAny<string>(), It.Is<string>(body => body.Contains("https://example.com/confirm"))), Times.Once);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenUserCreationFails()
    {
        // Arrange
        var failure = IdentityResult.Failed(new IdentityError { Description = "Email already used" });

        var userManager = CreateUserManager();
        userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(failure);

        var signInManager = CreateSignInManager(userManager);
        var emailSender = new Mock<IEmailSender>();

        var controller = BuildController(userManager.Object, signInManager.Object, emailSender.Object);
        var dto = new RegisterDto("user@example.com", "Password123!");

        // Act
        var result = await controller.Register(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        emailSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserMissing()
    {
        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager);
        var emailSender = new Mock<IEmailSender>();

        userManager.Setup(m => m.FindByEmailAsync("missing@example.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var controller = BuildController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.Login(new LoginDto("missing@example.com", "whatever"));

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsForbid_WhenEmailNotConfirmed()
    {
        var user = new ApplicationUser { Email = "user@example.com" };

        var userManager = CreateUserManager();
        userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManager.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(false);

        var signInManager = CreateSignInManager(userManager);
        var emailSender = new Mock<IEmailSender>();

        var controller = BuildController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.Login(new LoginDto(user.Email!, "Password123!"));

        Assert.IsType<ForbidResult>(result);
        signInManager.Verify(m => m.PasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Login_ReturnsOk_WhenCredentialsValid()
    {
        var user = new ApplicationUser { Email = "user@example.com" };

        var userManager = CreateUserManager();
        userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManager.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);

        var signInManager = CreateSignInManager(userManager);
        signInManager.Setup(m => m.PasswordSignInAsync(user, "Password123!", true, true))
            .ReturnsAsync(SignInResult.Success);

        var emailSender = new Mock<IEmailSender>();

        var controller = BuildController(userManager.Object, signInManager.Object, emailSender.Object);

        var result = await controller.Login(new LoginDto(user.Email!, "Password123!"));

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordInvalid()
    {
        var user = new ApplicationUser { Email = "user@example.com" };

        var userManager = CreateUserManager();
        userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManager.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);

        var signInManager = CreateSignInManager(userManager);
        signInManager.Setup(m => m.PasswordSignInAsync(user, "WrongPassword!", true, true))
            .ReturnsAsync(SignInResult.Failed);

        var controller = BuildController(userManager.Object, signInManager.Object, Mock.Of<IEmailSender>());

        var result = await controller.Login(new LoginDto(user.Email!, "WrongPassword!"));

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions());

        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            options.Object,
            Mock.Of<IPasswordHasher<ApplicationUser>>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<ApplicationUser>>>());
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManager(Mock<UserManager<ApplicationUser>> userManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        contextAccessor.SetupGet(c => c.HttpContext).Returns(new DefaultHttpContext());

        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions());

        return new Mock<SignInManager<ApplicationUser>>(
            userManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            options.Object,
            Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<ApplicationUser>>());
    }

    private static AuthController BuildController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IEmailSender email)
    {
        var controller = new AuthController(users, signIn, email)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            Url = new StubUrlHelper()
        };

        controller.Request.Scheme = "https";
        controller.Request.Host = new HostString("example.com");

        return controller;
    }

    private sealed class StubUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext => new();

        public string? Action(UrlActionContext actionContext) => "https://example.com/confirm";
        public string? Action(string? action, string? controller, object? values, string? protocol, string? host, string? fragment) => "https://example.com/confirm";
        public string? Action(string? action, string? controller, object? values, string? protocol, string? host) => "https://example.com/confirm";
        public string? Action(string? action, string? controller, object? values, string? protocol) => "https://example.com/confirm";
        public string? Action(string? action, string? controller, object? values) => "https://example.com/confirm";
        public string? Action(string? action, string? controller) => "https://example.com/confirm";
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => true;
        public string? Link(string? routeName, object? values) => "https://example.com/link";
        public string? RouteUrl(UrlRouteContext routeContext) => "https://example.com/route";
    }
}
