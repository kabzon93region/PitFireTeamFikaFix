using System;
using System.Reflection;
using EFT;
using HarmonyLib;

namespace PitFireTeamFikaFix
{
    /// <summary>
    /// Reflection bridge to pitFireTeam 0.8.x (pitTeam.Modules.BossPlayers).
    /// IsFollower(BotOwner, AIBossPlayer) uses EFT global AIBossPlayer — not pitTeam.Components.
    /// </summary>
    internal static class PitFireTeamReflection
    {
        private static Type _bossPlayersType;
        private static MethodInfo _isPlayerBoss;
        private static MethodInfo _isFollowerProfileId;
        private static MethodInfo _isFollowerBot;
        private static MethodInfo _getFollowerByProfileId;
        private static FieldInfo _pickupEnabled;
        private static bool _probeDone;

        internal static bool IsAvailable()
        {
            EnsureCached();
            return _bossPlayersType != null && _isPlayerBoss != null;
        }

        internal static bool IsPickupEnabled()
        {
            EnsureCached();
            if (_pickupEnabled == null)
            {
                return true;
            }

            try
            {
                var entry = _pickupEnabled.GetValue(null);
                var valueProp = entry?.GetType().GetProperty("Value");
                return valueProp != null && (bool)valueProp.GetValue(entry);
            }
            catch
            {
                return true;
            }
        }

        internal static bool IsPlayerBoss(string profileId)
        {
            EnsureCached();
            if (string.IsNullOrEmpty(profileId) || _isPlayerBoss == null)
            {
                return false;
            }

            try
            {
                return (bool)_isPlayerBoss.Invoke(null, new object[] { profileId });
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsFollowerProfileId(string profileId)
        {
            EnsureCached();
            if (string.IsNullOrEmpty(profileId) || _isFollowerProfileId == null)
            {
                return false;
            }

            try
            {
                return (bool)_isFollowerProfileId.Invoke(null, new object[] { profileId });
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsFollower(BotOwner bot)
        {
            EnsureCached();
            if (bot == null)
            {
                return false;
            }

            if (TryInvokeIsFollower(bot))
            {
                return true;
            }

            if (IsFollowerProfileId(bot.ProfileId))
            {
                return true;
            }

            if (TryGetFollowerByProfileId(bot.ProfileId) != null)
            {
                return true;
            }

            return IsFollowerByBotFollowerState(bot);
        }

        internal static bool IsFollowerByBotFollowerState(BotOwner bot)
        {
            if (bot?.BotFollower?.HaveBoss != true)
            {
                return false;
            }

            var bossToFollow = bot.BotFollower.BossToFollow;
            if (bossToFollow == null)
            {
                return false;
            }

            try
            {
                var playerMethod = bossToFollow.GetType().GetMethod("Player", BindingFlags.Instance | BindingFlags.Public);
                var player = playerMethod?.Invoke(bossToFollow, null) as Player;
                if (player != null && IsPlayerBoss(player.ProfileId))
                {
                    return true;
                }
            }
            catch
            {
                // ignored
            }

            return false;
        }

        internal static bool ProbeDiagnostics(out string summary)
        {
            EnsureCached();
            summary =
                $"BossPlayers={_bossPlayersType != null} IsPlayerBoss={_isPlayerBoss != null} " +
                $"IsFollower={_isFollowerBot != null} IsFollowerProfileId={_isFollowerProfileId != null} " +
                $"GetFollowerByProfileId={_getFollowerByProfileId != null} AIBossPlayerType={typeof(AIBossPlayer).FullName}";
            return _bossPlayersType != null && _isFollowerBot != null;
        }

        internal static bool HasFollowerRecord(string profileId) =>
            TryGetFollowerByProfileId(profileId) != null;

        private static bool TryInvokeIsFollower(BotOwner bot)
        {
            if (_isFollowerBot == null)
            {
                return false;
            }

            try
            {
                var parameters = _isFollowerBot.GetParameters();
                var args = parameters.Length >= 2
                    ? new object[] { bot, null }
                    : new object[] { bot };
                return (bool)_isFollowerBot.Invoke(null, args);
            }
            catch
            {
                return false;
            }
        }

        private static object TryGetFollowerByProfileId(string profileId)
        {
            if (string.IsNullOrEmpty(profileId) || _getFollowerByProfileId == null)
            {
                return null;
            }

            try
            {
                return _getFollowerByProfileId.Invoke(null, new object[] { profileId });
            }
            catch
            {
                return null;
            }
        }

        private static void EnsureCached()
        {
            if (_probeDone && _bossPlayersType != null)
            {
                return;
            }

            _probeDone = true;

            _bossPlayersType = AccessTools.TypeByName("pitTeam.Modules.BossPlayers");
            if (_bossPlayersType == null)
            {
                return;
            }

            _isPlayerBoss = AccessTools.Method(_bossPlayersType, "IsPlayerBoss", new[] { typeof(string) });
            _isFollowerProfileId = AccessTools.Method(_bossPlayersType, "IsFollowerProfileId", new[] { typeof(string) });
            _getFollowerByProfileId = AccessTools.Method(_bossPlayersType, "GetFollowerByProfileId", new[] { typeof(string) });
            _isFollowerBot = AccessTools.Method(
                _bossPlayersType,
                "IsFollower",
                new[] { typeof(BotOwner), typeof(AIBossPlayer) });

            var pitFireTeamType = AccessTools.TypeByName("pitTeam.pitFireTeam");
            if (pitFireTeamType != null)
            {
                _pickupEnabled = AccessTools.Field(pitFireTeamType, "pickupEnabled");
            }
        }
    }
}
