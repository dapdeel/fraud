namespace AuthApi.DTOs
{
    using Microsoft.AspNetCore.Identity;
    using System.Collections.Generic;

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public IEnumerable<IdentityError> Errors { get; set; }
    }
}
