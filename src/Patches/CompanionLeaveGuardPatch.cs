using System.Reflection;
using EFT;
using HarmonyLib;

namespace PitFireTeamFikaFix.Patches
{
    /// <summary>
    /// Mass LeaveAll must not dismiss Pit Fire Team squad followers (e.g. BossSpawnControl clear button).
    /// </summary>
    internal static class CompanionLeaveGuardPatch
    {
        private static bool _applied;

        internal static bool TryApply(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
        {
            if (_applied)
            {
                return true;
            }

            if (!PitFireTeamReflection.IsAvailable())
            {
                logger.LogInfo("[PITFIRE_FIKA] CompanionLeaveGuard skipped (pitFireTeam not loaded)");
                return false;
            }

            var leaveAll = AccessTools.Method(typeof(ZoneLeaveControllerClass), nameof(ZoneLeaveControllerClass.LeaveAll));
            if (leaveAll == null)
            {
                logger.LogWarning("[PITFIRE_FIKA] ZoneLeaveControllerClass.LeaveAll not found");
                return false;
            }

            harmony.Patch(leaveAll, prefix: new HarmonyMethod(typeof(CompanionLeaveGuardPatch), nameof(LeaveAllPrefix)));
            logger.LogInfo("[PITFIRE_FIKA] Patched ZoneLeaveControllerClass.LeaveAll (protect PitFire companions)");
            _applied = true;
            return true;
        }

        private static bool LeaveAllPrefix(ZoneLeaveControllerClass __instance)
        {
            if (__instance?.BotsClass?.BotOwners == null)
            {
                return true;
            }

            var removed = 0;
            var protectedCount = 0;

            foreach (var bot in __instance.BotsClass.BotOwners)
            {
                if (bot == null)
                {
                    continue;
                }

                if (CompanionGuard.IsProtectedBot(bot))
                {
                    protectedCount++;
                    continue;
                }

                bot.LeaveData?.DoLeaveExternal();
                removed++;
            }

            if (protectedCount > 0 || removed > 0)
            {
                PluginCore.FixLogger?.LogInfo(
                    $"[PITFIRE_FIKA] LeaveAll filtered: removed={removed} protectedCompanions={protectedCount}");
            }

            return false;
        }
    }
}
