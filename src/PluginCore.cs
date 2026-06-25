using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PitFireTeamFikaFix.Patches;

namespace PitFireTeamFikaFix
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    [BepInDependency("xyz.pit.fireteam", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    public class PluginCore : BaseUnityPlugin
    {
        internal static ManualLogSource FixLogger { get; private set; }
        internal static PluginCore Instance { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            FixLogger = base.Logger;
            _harmony = new Harmony(PluginInfo.GUID);

            var fikaLoaded = FikaSession.IsFikaLoaded();
            FixLogger.LogInfo($"[PITFIRE_FIKA] {PluginInfo.NAME} v{PluginInfo.VERSION} loading (fika={fikaLoaded})");

            if (!fikaLoaded)
            {
                FixLogger.LogWarning("[PITFIRE_FIKA] Fika.Core not detected — coop session guards may be inactive");
            }

            var hostOnlySpawn = HostOnlySpawnPatch.TryApply(_harmony, FixLogger);
            var clientSpawnGuard = ClientSpawnGuardPatch.TryApply(_harmony, FixLogger);
            var clientQueueDedupe = ClientQueueProfileDedupePatch.TryApply(_harmony, FixLogger);
            var observedDedupe = ClientObservedPlayerDedupePatch.TryApply(_harmony, FixLogger);
            var createBotDedupe = CreateBotDedupePatch.TryApply(_harmony, FixLogger);
            var raidCleanup = CoopRaidCleanupPatch.TryApply(_harmony, FixLogger);
            var debugGuard = DebugSpawnGuardPatch.TryApply(_harmony, FixLogger);

            if (!hostOnlySpawn && !clientSpawnGuard && !clientQueueDedupe
                && !observedDedupe && !createBotDedupe && !raidCleanup && !debugGuard)
            {
                FixLogger.LogWarning("[PITFIRE_FIKA] No patches applied (pitFireTeam / Fika host API missing?)");
            }

            FixLogger.LogInfo("[PITFIRE_FIKA] Load complete (spawn/dedupe v0.2.1 — recruit bridge disabled)");
        }
        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
