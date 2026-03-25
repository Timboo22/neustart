using Neustart.Api.DTOs;

namespace Neustart.Api.Services.Contracts;

public interface IRegisterService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
}
