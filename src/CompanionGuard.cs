using EFT;

namespace PitFireTeamFikaFix
{
    /// <summary>
    /// Public API: Pit Fire Team squad followers that must not be removed by mass bot clear.
    /// </summary>
    public static class CompanionGuard
    {
        public static bool IsAvailable => PitFireTeamReflection.IsAvailable();

        public static bool IsProtectedBot(BotOwner bot) =>
            PitFireTeamReflection.IsFollower(bot);

        public static bool IsProtectedProfileId(string profileId) =>
            PitFireTeamReflection.IsFollowerProfileId(profileId);
    }
}
