namespace AuthApi.Models
{
    using Microsoft.AspNetCore.Identity;
    using System;

    public class ApplicationUser : IdentityUser
    {
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
    }
}
