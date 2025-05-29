using Microsoft.AspNetCore.Mvc;
using WeatherChecker_Dominik_Szymczyk.Models;
using WeatherChecker_Dominik_Szymczyk.Services;

namespace WeatherChecker_Dominik_Szymczyk.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IJwtTokenService _tokenService;

        public AccountController(IJwtTokenService tokenService)
        {
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] Account account)
        {
            // Uproszczone logowanie – zawsze się „udaje”
            var token = _tokenService.GenerateToken(account.Email);
            return Ok(token);
        }
    }
}
