using System.Collections.Concurrent;

namespace PitFireTeamFikaFix
{
    /// <summary>
    /// Prevents parallel BotCreator/CreateBot for the same profile (PitFireTeam double-spawn + Fika).
    /// </summary>
    internal static class SpawnProfileGuard
    {
        private static readonly ConcurrentDictionary<string, byte> InFlight = new ConcurrentDictionary<string, byte>();

        public static bool TryBegin(string profileId)
        {
            if (string.IsNullOrEmpty(profileId))
            {
                return true;
            }

            return InFlight.TryAdd(profileId, 0);
        }

        public static bool IsInFlight(string profileId)
        {
            return !string.IsNullOrEmpty(profileId) && InFlight.ContainsKey(profileId);
        }

        public static void End(string profileId)
        {
            if (string.IsNullOrEmpty(profileId))
            {
                return;
            }

            InFlight.TryRemove(profileId, out _);
        }
    }
}
