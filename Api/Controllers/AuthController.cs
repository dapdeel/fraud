using Api.DTOs;
using Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDTO model)
        {
            var result = await _authService.RegisterAsync(model);
            if (!result.Success)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Status = "error",
                    Message = "Registration failed",
                    Error = new ApiError
                    {
                        Code = "REGISTRATION_FAILED",
                        Details = string.Join(", ", result.Errors.Select(e => e.Description))
                    }
                });
            }

            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "User registered successfully",
                Data = new { result.Token, result.RefreshToken }
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO model)
        {
            try
            {
                var result = await _authService.LoginAsync(model);
                if (!result.Success)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Status = "error",
                        Message = "Login failed",
                        Error = new ApiError
                        {
                            Code = "LOGIN_FAILED",
                            Details = string.Join(", ", result.Errors.Select(e => e.Description))
                        }
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Status = "success",
                    Message = "Login successful",
                    Data = new { result.Token, result.RefreshToken }
                });
            }
            catch (Exception Exception)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Status = "Error",
                    Message = Exception.Message,
                    Error = new ApiError
                    {
                        Code = "LOGIN_FAILED",
                        Details = string.Join(", ", Exception.InnerException.ToString())
                    }
                });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(TokenRequestDTO tokenRequest)
        {
            var result = await _authService.RefreshTokenAsync(tokenRequest.Token, tokenRequest.RefreshToken);
            if (!result.Success)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Status = "error",
                    Message = "Token refresh failed",
                    Error = new ApiError
                    {
                        Code = "TOKEN_REFRESH_FAILED",
                        Details = string.Join(", ", result.Errors.Select(e => e.Description))
                    }
                });
            }

            return Ok(new ApiResponse<object>
            {
                Status = "success",
                Message = "Token refreshed successfully",
                Data = new { result.Token, result.RefreshToken }
            });
        }
    }
}
