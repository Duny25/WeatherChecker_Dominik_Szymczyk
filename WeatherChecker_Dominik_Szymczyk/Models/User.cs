namespace WeatherChecker_Dominik_Szymczyk.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public bool Is2FaEnabled { get; set; } = false;
        public string? TwoFactorCode { get; set; }
        public DateTime? TwoFactorExpiry { get; set; }
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }
        public DateTime? LastFailedLogin { get; set; }
        

    }
}
