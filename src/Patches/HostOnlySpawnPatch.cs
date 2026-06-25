using System.Reflection;
using HarmonyLib;

namespace PitFireTeamFikaFix.Patches
{
    /// <summary>
    /// P0: Fika clients must not run PitFireTeam follower spawn (host-authoritative AI).
    /// </summary>
    internal static class HostOnlySpawnPatch
    {
        internal static bool TryApply(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
        {
            var spawnFollowersType = AccessTools.TypeByName("pitTeam.Patches.BotsEventsControllerSpawnPatch");
            if (spawnFollowersType == null)
            {
                logger.LogWarning("[PITFIRE_FIKA] BotsEventsControllerSpawnPatch type not found");
                return false;
            }

            var spawnFollowers = AccessTools.Method(spawnFollowersType, "SpawnFollowers");
            if (spawnFollowers == null)
            {
                logger.LogWarning("[PITFIRE_FIKA] SpawnFollowers method not found");
                return false;
            }

            harmony.Patch(spawnFollowers, prefix: new HarmonyMethod(typeof(HostOnlySpawnPatch), nameof(SpawnFollowersPrefix)));
            logger.LogInfo("[PITFIRE_FIKA] Patched pitTeam SpawnFollowers (host-only in Fika coop)");
            return true;
        }

        private static bool SpawnFollowersPrefix()
        {
            if (!FikaSession.IsRemoteClient())
            {
                return true;
            }

            PluginCore.FixLogger?.LogInfo("[PITFIRE_FIKA] Skipped SpawnFollowers on Fika client");
            return false;
        }
    }
}
