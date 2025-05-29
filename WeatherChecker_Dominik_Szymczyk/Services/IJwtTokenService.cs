namespace WeatherChecker_Dominik_Szymczyk.Services
{
    public interface IJwtTokenService
    {
        string GenerateToken(string email);
    }
}
