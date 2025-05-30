using WeatherChecker_Dominik_Szymczyk.Models;
using WeatherChecker_Dominik_Szymczyk.Security;
using System.Collections.Concurrent;

namespace WeatherChecker_Dominik_Szymczyk.Repositories
{
    public class UserRepository
    {
        private static List<User> users = new(); // nasza tymczasowa baza danych

        // 🔐 Statyczny słownik do śledzenia prób logowania z IP
        public static ConcurrentDictionary<string, IpLoginTracker> IpLoginAttempts { get; } = new();

        public List<User> GetAll() => users;

        public User? GetByEmail(string email) =>
            users.FirstOrDefault(u => u.Email == email);

        public void Add(User user) => users.Add(user);
    }
}
