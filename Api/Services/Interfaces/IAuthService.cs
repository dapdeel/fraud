using Api.DTOs;

namespace Api.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterUserDTO model);
        Task<AuthResponse> LoginAsync(LoginDTO model);
        Task LogoutAsync(string userId);
        Task EnsureUserExists(string userId);
        Task<AuthResponse> RefreshTokenAsync(string token, string refreshToken);
    }
}
