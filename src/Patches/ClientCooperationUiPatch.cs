using System.Reflection;
using EFT;
using HarmonyLib;

namespace PitFireTeamFikaFix.Patches
{
    /// <summary>
    /// Fika clients see bots as ObservedPlayer without BotOwner — stock PitFireTeam hides Cooperation.
    /// </summary>
    internal static class ClientCooperationUiPatch
    {
        private const float MaxInteractionDistance = 3.5f;
        private static MethodInfo _setPhraseVisible;

        internal static bool TryApply(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
        {
            var target = AccessTools.Method(typeof(EFT.UI.Gestures.GesturesQuickPanel), "method_1");
            if (target == null)
            {
                logger.LogWarning("[PITFIRE_FIKA] GesturesQuickPanel.method_1 not found");
                return false;
            }

            _setPhraseVisible = AccessTools.Method(typeof(EFT.UI.Gestures.GesturesQuickPanel), "method_7");
            if (_setPhraseVisible == null)
            {
                logger.LogWarning("[PITFIRE_FIKA] GesturesQuickPanel.method_7 not found");
                return false;
            }

            harmony.Patch(
                target,
                postfix: new HarmonyMethod(typeof(ClientCooperationUiPatch), nameof(QuickPanelPostfix))
                {
                    priority = Priority.Last
                });

            logger.LogInfo("[PITFIRE_FIKA] Patched GesturesQuickPanel for ObservedPlayer Cooperation UI");
            return true;
        }

        private static void QuickPanelPostfix(object __instance)
        {
            if (!FikaSession.IsRemoteClient() || !PitFireTeamReflection.IsPickupEnabled())
            {
                return;
            }

            var player = AccessTools.Field(typeof(EFT.UI.Gestures.GesturesQuickPanel), "player_0").GetValue(__instance) as Player;
            if (player == null || !PitFireTeamReflection.IsPlayerBoss(player.ProfileId))
            {
                return;
            }

            var target = ResolveRecruitTarget(player);
            if (!IsValidObservedRecruitTarget(player, target))
            {
                return;
            }

            _setPhraseVisible.Invoke(__instance, new object[] { EPhraseTrigger.Cooperation, true });
        }

        internal static Player ResolveRecruitTarget(Player player)
        {
            if (player == null)
            {
                return null;
            }

            var stockTarget = player.InteractablePlayer;
            if (IsValidObservedRecruitTarget(player, stockTarget))
            {
                return stockTarget;
            }

            return null;
        }

        internal static bool IsValidObservedRecruitTarget(Player requester, Player candidate)
        {
            if (requester == null ||
                candidate == null ||
                candidate == requester ||
                !candidate.IsAI ||
                candidate.HealthController?.IsAlive != true ||
                candidate.Side != requester.Side)
            {
                return false;
            }

            if (!IsObservedAi(candidate) && candidate.AIData?.BotOwner == null)
            {
                return false;
            }

            if (PitFireTeamReflection.IsFollowerProfileId(candidate.ProfileId))
            {
                return false;
            }

            if (candidate.AIData?.BotOwner != null && PitFireTeamReflection.IsFollower(candidate.AIData.BotOwner))
            {
                return false;
            }

            return UnityEngine.Vector3.Distance(requester.Position, candidate.Position) <= MaxInteractionDistance;
        }

        private static bool IsObservedAi(Player player)
        {
            if (player == null)
            {
                return false;
            }

            var observedType = AccessTools.TypeByName("Fika.Core.Main.Players.ObservedPlayer");
            if (observedType == null || !observedType.IsInstanceOfType(player))
            {
                return false;
            }

            var field = AccessTools.Field(observedType, "IsObservedAI");
            return field != null && (bool)field.GetValue(player);
        }
    }
}
