using System;
using System.Reflection;
using EFT;
using HarmonyLib;

namespace PitFireTeamFikaFix
{
    internal static class PitFireTeamReflection
    {
        private static Type _bossPlayersType;
        private static Type _pitFireTeamType;
        private static MethodInfo _isPlayerBoss;
        private static MethodInfo _isFollowerProfileId;
        private static MethodInfo _isFollowerBot;
        private static FieldInfo _pickupEnabled;

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
            if (bot == null || _isFollowerBot == null)
            {
                return false;
            }

            try
            {
                return (bool)_isFollowerBot.Invoke(null, new object[] { bot, null });
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureCached()
        {
            if (_bossPlayersType != null)
            {
                return;
            }

            _bossPlayersType = AccessTools.TypeByName("pitTeam.Modules.BossPlayers");
            _pitFireTeamType = AccessTools.TypeByName("pitTeam.pitFireTeam");
            if (_bossPlayersType != null)
            {
                _isPlayerBoss = AccessTools.Method(_bossPlayersType, "IsPlayerBoss", new[] { typeof(string) });
                _isFollowerProfileId = AccessTools.Method(_bossPlayersType, "IsFollowerProfileId", new[] { typeof(string) });
                _isFollowerBot = AccessTools.Method(_bossPlayersType, "IsFollower", new[] { typeof(BotOwner), AccessTools.TypeByName("pitTeam.Components.AIBossPlayer") });
            }

            if (_pitFireTeamType != null)
            {
                _pickupEnabled = AccessTools.Field(_pitFireTeamType, "pickupEnabled");
            }
        }
    }
}
