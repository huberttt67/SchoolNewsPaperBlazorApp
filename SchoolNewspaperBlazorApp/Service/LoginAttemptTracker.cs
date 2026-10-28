using System.Collections.Concurrent;

namespace SchoolNewspaperBlazorApp.Service
{
    // Blokada logowania: po 5 błędnych hasłach z rzędu trzeba odczekać 5 minut.
    // Liczniki są w pamięci serwera (po restarcie aplikacji są zerowane).
    public class LoginAttemptTracker
    {
        public const int MaxAttempts = 5;
        public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(5);

        private sealed class Entry
        {
            public int Failures;
            public DateTime LockedUntil = DateTime.MinValue;
            public DateTime LastFailure = DateTime.MinValue;
        }

        private readonly ConcurrentDictionary<string, Entry> _entries = new();

        private static string Key(string email, string? ip) =>
            $"{email.Trim().ToLowerInvariant()}|{ip}";

        // Zwraca pozostały czas blokady albo null, gdy można się logować.
        public TimeSpan? GetLockRemaining(string email, string? ip)
        {
            if (!_entries.TryGetValue(Key(email, ip), out var entry))
            {
                return null;
            }

            lock (entry)
            {
                var remaining = entry.LockedUntil - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : null;
            }
        }

        // Zapisuje błędną próbę. Zwraca true, jeśli właśnie założono blokadę.
        public bool RegisterFailure(string email, string? ip)
        {
            var entry = _entries.GetOrAdd(Key(email, ip), _ => new Entry());

            lock (entry)
            {
                var now = DateTime.UtcNow;

                // Po dawnej blokadzie albo długiej przerwie liczymy od zera.
                if ((entry.LockedUntil != DateTime.MinValue && entry.LockedUntil <= now) ||
                    now - entry.LastFailure > LockDuration * 3)
                {
                    entry.Failures = 0;
                    entry.LockedUntil = DateTime.MinValue;
                }

                entry.Failures++;
                entry.LastFailure = now;

                if (entry.Failures >= MaxAttempts)
                {
                    entry.LockedUntil = now + LockDuration;
                    return true;
                }

                return false;
            }
        }

        public void Reset(string email, string? ip) =>
            _entries.TryRemove(Key(email, ip), out _);
    }
}