namespace Api.Models
{
    using Microsoft.AspNetCore.Identity;
    using System;

    public class ApplicationUser : IdentityUser
    {
        public string? Firstname { get; set; }
        public string?Lastname { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public ICollection<Observatory> Observatories { get; } = [];
        public ICollection<UserObservatory> UserObservatories { get; } = [];
    }
}
