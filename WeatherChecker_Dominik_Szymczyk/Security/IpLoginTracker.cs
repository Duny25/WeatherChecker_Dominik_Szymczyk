namespace WeatherChecker_Dominik_Szymczyk.Security
{
    public class IpLoginTracker
    {
        public int FailedAttempts { get; set; } = 0;
        public DateTime? BlockedUntil { get; set; }
    }
}
