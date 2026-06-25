using System.Reflection;
using System.Threading.Tasks;
using EFT;
using HarmonyLib;

namespace PitFireTeamFikaFix.Patches
{
    /// <summary>
    /// P0b: Fika clients must not run PitFireTeam local bot spawn / prefetch (host-authoritative AI).
    /// PitFire HasFika() uses wrong type name, so stock guards are inactive in coop.
    /// </summary>
    internal static class ClientSpawnGuardPatch
    {
        internal static bool TryApply(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
        {
            var botsPatchType = AccessTools.TypeByName("pitTeam.Patches.BotsControllerPatch");
            if (botsPatchType == null)
            {
                logger.LogWarning("[PITFIRE_FIKA] BotsControllerPatch type not found");
                return false;
            }

            var applied = false;

            applied |= TryPatch(harmony, botsPatchType, "SpawnGroupBots", nameof(SpawnGroupBotsPrefix));
            applied |= TryPatch(harmony, botsPatchType, "SpawnBossFollower", nameof(SpawnBossFollowerPrefix));
            applied |= TryPatch(harmony, botsPatchType, "CreateFollowerProfiles", nameof(CreateFollowerProfilesPrefix));
            applied |= TryPatch(harmony, botsPatchType, "PreFetchBossProfiles", nameof(PreFetchBossProfilesPrefix));
            applied |= TryPatch(harmony, botsPatchType, "ActivateBotAtPosition", nameof(ActivateBotAtPositionPrefix));

            if (applied)
            {
                logger.LogInfo("[PITFIRE_FIKA] Patched pitTeam client spawn guards (host-only in Fika coop)");
            }

            return applied;
        }

        private static bool TryPatch(Harmony harmony, System.Type targetType, string methodName, string prefixName)
        {
            var method = AccessTools.Method(targetType, methodName);
            if (method == null)
            {
                return false;
            }

            harmony.Patch(method, prefix: new HarmonyMethod(typeof(ClientSpawnGuardPatch), prefixName));
            return true;
        }

        private static bool SpawnGroupBotsPrefix(ref Task __result)
        {
            if (!FikaSession.IsRemoteClient())
            {
                return true;
            }

            PluginCore.FixLogger?.LogInfo("[PITFIRE_FIKA] Blocked SpawnGroupBots on Fika client");
            __result = Task.CompletedTask;
            return false;
        }

        private static bool SpawnBossFollowerPrefix(ref Task __result)
        {
            if (!FikaSession.IsRemoteClient())
            {
                return true;
            }

            PluginCore.FixLogger?.LogInfo("[PITFIRE_FIKA] Blocked SpawnBossFollower on Fika client");
            __result = Task.CompletedTask;
            return false;
        }

        private static bool CreateFollowerProfilesPrefix(ref Task<System.Collections.Generic.Dictionary<string, Profile>> __result)
        {
            if (!FikaSession.IsRemoteClient())
            {
                return true;
            }

            PluginCore.FixLogger?.LogInfo("[PITFIRE_FIKA] Blocked CreateFollowerProfiles on Fika client");
            __result = Task.FromResult(new System.Collections.Generic.Dictionary<string, Profile>());
            return false;
        }

        private static bool PreFetchBossProfilesPrefix(ref Task<BotCreationDataClass> __result)
        {
            if (!FikaSession.IsRemoteClient())
            {
                return true;
            }

            PluginCore.FixLogger?.LogInfo("[PITFIRE_FIKA] Blocked PreFetchBossProfiles on Fika client");
            __result = Task.FromResult<BotCreationDataClass>(null);
            return false;
        }

        private static bool ActivateBotAtPositionPrefix(ref Task<bool> __result)
        {
            if (!FikaSession.IsRemoteClient())
            {
                return true;
            }

            PluginCore.FixLogger?.LogInfo("[PITFIRE_FIKA] Blocked ActivateBotAtPosition on Fika client");
            __result = Task.FromResult(false);
            return false;
        }
    }
}
