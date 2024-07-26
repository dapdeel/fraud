using AuthApi.DTOs;

namespace AuthApi.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterUserDTO model);
        Task<AuthResponse> LoginAsync(LoginDTO model);

        Task<AuthResponse> RefreshTokenAsync(string token, string refreshToken);
    }
}
