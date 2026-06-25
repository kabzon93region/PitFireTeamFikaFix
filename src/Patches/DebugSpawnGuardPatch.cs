using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;

namespace PitFireTeamFikaFix.Patches
{
    /// <summary>
    /// Blocks PitFireTeam in-raid debug spawn on Fika clients.
    /// </summary>
    internal static class DebugSpawnGuardPatch
    {
        internal static bool TryApply(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
        {
            var botsPatchType = AccessTools.TypeByName("pitTeam.Patches.BotsControllerPatch");
            if (botsPatchType == null)
            {
                logger.LogWarning("[PITFIRE_FIKA] BotsControllerPatch type not found");
                return false;
            }

            var spawnDebug = AccessTools.Method(botsPatchType, "SpawnDebugFollower");
            if (spawnDebug == null)
            {
                logger.LogWarning("[PITFIRE_FIKA] SpawnDebugFollower not found");
                return false;
            }

            harmony.Patch(spawnDebug, prefix: new HarmonyMethod(typeof(DebugSpawnGuardPatch), nameof(SpawnDebugFollowerPrefix)));
            logger.LogInfo("[PITFIRE_FIKA] Patched pitTeam SpawnDebugFollower (host-only in Fika coop)");
            return true;
        }

        private static bool SpawnDebugFollowerPrefix(ref Task<bool> __result)
        {
            if (!FikaSession.IsRemoteClient())
            {
                return true;
            }

            PluginCore.FixLogger?.LogInfo("[PITFIRE_FIKA] Blocked SpawnDebugFollower on Fika client");
            __result = Task.FromResult(false);
            return false;
        }
    }
}
