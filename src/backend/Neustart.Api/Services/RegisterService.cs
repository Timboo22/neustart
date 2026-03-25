using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Neustart.Api.DTOs;
using Neustart.Api.Models;
using Neustart.Api.Services.Contracts;
using Microsoft.Extensions.Logging;

namespace Neustart.Api.Services;

public class RegisterService : IRegisterService
{
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RegisterService> _logger;

    public RegisterService(
        UserManager<User> userManager, 
        IConfiguration configuration,
        ILogger<RegisterService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        _logger.LogInformation("Registering user {Email}", request.Email);

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            Geburtsdatum = request.Geburtsdatum
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Registration failed for {Email}: {Errors}", request.Email, errors);
            return null;
        }
        
        _logger.LogInformation("User {Email} registered successfully", request.Email);
        var token = GenerateJwtToken(user);
        return new AuthResponse { Token = token, Email = user.Email ?? request.Email };
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Login attempt for user {Email}", request.Email);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed: User {Email} not found", request.Email);
            return null;
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Login failed: Invalid password for user {Email}", request.Email);
            return null;
        }

        _logger.LogInformation("User {Email} logged in successfully", request.Email);
        var token = GenerateJwtToken(user);
        return new AuthResponse { Token = token, Email = user.Email ?? request.Email };
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not found");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? "")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
