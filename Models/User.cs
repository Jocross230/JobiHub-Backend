using System;
using System.Collections.Generic;

namespace CVBuilder.API.Models
{
    public class User
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // JobSeeker, Business, or Admin
        public string Role { get; set; } = "JobSeeker";

        // Navigation: one user can own multiple CVs
        public List<Cv> Cvs { get; set; } = new();
        public Business? Business { get; set; }
    }
}