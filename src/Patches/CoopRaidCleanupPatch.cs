using System;
using System.Reflection;
using Fika.Core.Main.GameMode;
using HarmonyLib;

namespace PitFireTeamFikaFix.Patches
{
    internal static class CoopRaidCleanupPatch
    {
        internal static bool TryApply(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
        {
            try
            {
                var stopMethod = AccessTools.Method(typeof(CoopGame), "Stop");
                if (stopMethod == null)
                {
                    logger.LogWarning("[PITFIRE_FIKA] CoopGame.Stop not found");
                    return false;
                }

                harmony.Patch(
                    stopMethod,
                    postfix: new HarmonyMethod(typeof(CoopRaidCleanupPatch), nameof(CoopStopPostfix)));

                logger.LogInfo("[PITFIRE_FIKA] Patched CoopGame.Stop raid cleanup");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"[PITFIRE_FIKA] Coop raid cleanup patch failed: {ex.Message}");
                return false;
            }
        }

        private static void CoopStopPostfix()
        {
            ClientProfileSpawnTracker.Clear();
            ClientGhostReconcile.Clear();
        }
    }
}
