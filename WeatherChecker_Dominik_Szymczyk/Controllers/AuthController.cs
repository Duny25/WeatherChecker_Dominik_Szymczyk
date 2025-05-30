using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using WeatherChecker_Dominik_Szymczyk.Models;
using WeatherChecker_Dominik_Szymczyk.Repositories;
using WeatherChecker_Dominik_Szymczyk.Services;
using WeatherChecker_Dominik_Szymczyk.Security;

namespace WeatherChecker_Dominik_Szymczyk.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserRepository _repo;
        private readonly IJwtTokenService _tokenService;
        private static Dictionary<string, IpLoginTracker> _ipFailures = new();
        private const int MaxIpFailures = 100;
        private const int IpBlockMinutes = 60;

        public AuthController(UserRepository repo, IJwtTokenService tokenService)
        {
            _repo = repo;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
            if (_repo.GetByEmail(dto.Email) != null)
                return BadRequest("Użytkownik już istnieje.");

            if (dto.Password != dto.ConfirmPassword)
                return BadRequest("Hasła się nie zgadzają.");

            using var sha = SHA256.Create();
            var hash = Convert.ToBase64String(
                sha.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)));

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = hash
            };

            _repo.Add(user);
            return Ok("Zarejestrowano pomyślnie.");
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Sprawdź czy IP jest zablokowane
            if (UserRepository.IpLoginAttempts.TryGetValue(ip, out var tracker))
            {
                if (tracker.BlockedUntil.HasValue && tracker.BlockedUntil > DateTime.UtcNow)
                {
                    return Unauthorized($"Zbyt wiele prób logowania z adresu IP {ip}. Zablokowano do {tracker.BlockedUntil.Value}.");
                }
            }

            var user = _repo.GetByEmail(dto.Email);
            if (user == null)
            {
                IncrementFailedIpAttempt(ip);
                return Unauthorized("Nieprawidłowy email lub hasło");
            }

            using var sha = SHA256.Create();
            var hash = Convert.ToBase64String(
                sha.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)));

            if (user.PasswordHash != hash)
            {
                user.FailedLoginAttempts++;
                user.LastFailedLogin = DateTime.UtcNow;

                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
                    return Unauthorized("Zbyt wiele nieudanych prób. Konto zablokowane na 10 minut.");
                }

                IncrementFailedIpAttempt(ip);
                return Unauthorized("Nieprawidłowy email lub hasło");
            }

            // Sprawdź czy konto nie jest zablokowane
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                return Unauthorized($"Konto zablokowane do {user.LockoutEnd.Value}.");
            }

            // Reset prób IP po udanym logowaniu
            if (UserRepository.IpLoginAttempts.ContainsKey(ip))
                UserRepository.IpLoginAttempts.TryRemove(ip, out _);

            // 2FA
            var code = new Random().Next(100000, 999999).ToString();
            user.TwoFactorCode = code;
            user.TwoFactorExpiry = DateTime.UtcNow.AddMinutes(5);

            return Ok($"Kod 2FA: {code}. Podaj go w metodzie /confirm2fa");
        }

        // Pomocnicza metoda – inkrementacja prób z IP
        private void IncrementFailedIpAttempt(string ip)
        {
            var tracker = UserRepository.IpLoginAttempts.GetOrAdd(ip, new IpLoginTracker());
            tracker.FailedAttempts++;

            if (tracker.FailedAttempts >= 100)
            {
                tracker.BlockedUntil = DateTime.UtcNow.AddHours(1);
            }
        }


        [HttpPost("confirm2fa")]
        public IActionResult Confirm2FA(Confirm2FADto dto)
        {
            var user = _repo.GetByEmail(dto.Email);
            if (user == null)
                return Unauthorized("Użytkownik nie istnieje.");

            if (user.TwoFactorCode == null || user.TwoFactorExpiry == null)
                return BadRequest("Kod 2FA nie został jeszcze wygenerowany.");

            if (user.TwoFactorExpiry < DateTime.UtcNow)
                return Unauthorized("Kod 2FA wygasł.");

            if (user.TwoFactorCode != dto.Code)
                return Unauthorized("Nieprawidłowy kod 2FA.");

            user.TwoFactorCode = null;
            user.TwoFactorExpiry = null;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (_ipFailures.ContainsKey(ip))
            {
                _ipFailures[ip].FailedAttempts = 0;
                _ipFailures[ip].BlockedUntil = null;
            }

            var token = _tokenService.GenerateToken(user.Email);
            return Ok(new { token });
        }
    }
}
