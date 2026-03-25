using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Neustart.Api.Data;
using Neustart.Api.DTOs;
using Neustart.Api.Models;
using Neustart.Api.Services;
using System.Security.Claims;

namespace Neustart.Tests;

public class AuthTests
{
    private readonly DbContextOptions<AppDbContext> _dbContextOptions;
    private readonly AppDbContext _context;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<RegisterService>> _mockLogger;

    public AuthTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(_dbContextOptions);
        
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c.GetSection("JwtSettings")).Returns(new Mock<IConfigurationSection>().Object);
        _mockConfiguration.Setup(c => c.GetSection("JwtSettings")["Secret"]).Returns("SuperGeheimesUndLangesPasswort123!");
        _mockConfiguration.Setup(c => c.GetSection("JwtSettings")["Issuer"]).Returns("NeustartApi");
        _mockConfiguration.Setup(c => c.GetSection("JwtSettings")["Audience"]).Returns("NeustartUser");
        
        _mockLogger = new Mock<ILogger<RegisterService>>();
    }

    private RegisterService CreateService(UserManager<User> userManager)
    {
        return new RegisterService(userManager, _mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Register_ShouldCreateUserAndReturnToken_WhenDataIsValid()
    {
        // Arrange
        var userManager = GetUserManager();
        var service = CreateService(userManager);
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            Geburtsdatum = new DateOnly(2000, 1, 1)
        };

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.NotEmpty(result.Token);
        
        var user = await userManager.FindByEmailAsync("test@example.com");
        Assert.NotNull(user);
        Assert.Equal(new DateOnly(2000, 1, 1), user.Geburtsdatum);
    }

    [Fact]
    public async Task Login_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var userManager = GetUserManager();
        var service = CreateService(userManager);
        var email = "login@example.com";
        var password = "Password123!";
        
        var user = new User { UserName = email, Email = email, Geburtsdatum = new DateOnly(1990, 1, 1) };
        await userManager.CreateAsync(user, password);

        var loginRequest = new LoginRequest { Email = email, Password = password };

        // Act
        var result = await service.LoginAsync(loginRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task Login_ShouldReturnNull_WhenPasswordIsIncorrect()
    {
        // Arrange
        var userManager = GetUserManager();
        var service = CreateService(userManager);
        var email = "wrong@example.com";
        
        var user = new User { UserName = email, Email = email, Geburtsdatum = new DateOnly(1990, 1, 1) };
        await userManager.CreateAsync(user, "CorrectPassword123!");

        var loginRequest = new LoginRequest { Email = email, Password = "WrongPassword" };

        // Act
        var result = await service.LoginAsync(loginRequest);

        // Assert
        Assert.Null(result);
    }

    private UserManager<User> GetUserManager()
    {
        var options = new Mock<IOptions<IdentityOptions>>();
        var idOptions = new IdentityOptions();
        idOptions.Lockout.AllowedForNewUsers = false;
        idOptions.Password.RequireDigit = false;
        idOptions.Password.RequireLowercase = false;
        idOptions.Password.RequireNonAlphanumeric = false;
        idOptions.Password.RequireUppercase = false;
        idOptions.Password.RequiredLength = 6;
        
        options.Setup(o => o.Value).Returns(idOptions);
        
        var userValidators = new List<IUserValidator<User>> { new UserValidator<User>() };
        var passwordValidators = new List<IPasswordValidator<User>> { new PasswordValidator<User>() };
        
        var logger = new Mock<ILogger<UserManager<User>>>();

        var userManager = new UserManager<User>(
            new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<User>(_context),
            options.Object,
            new PasswordHasher<User>(),
            userValidators,
            passwordValidators,
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null, // IServiceProvider is allowed to be null here for simple tests
            logger.Object);

        return userManager;
    }
}
