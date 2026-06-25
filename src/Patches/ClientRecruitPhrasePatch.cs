using EFT;
using HarmonyLib;
using PitFireTeamFikaFix.Networking;

namespace PitFireTeamFikaFix.Patches
{
    /// <summary>
    /// Fika PlayDirect on host does not call BotEventHandler.SayPhrase — forward recruit to host via RPC.
    /// </summary>
    internal static class ClientRecruitPhrasePatch
    {
        internal static bool TryApply(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
        {
            var target = AccessTools.Method(typeof(Player), nameof(Player.Say));
            if (target == null)
            {
                logger.LogWarning("[PITFIRE_FIKA] Player.Say not found");
                return false;
            }

            harmony.Patch(target, postfix: new HarmonyMethod(typeof(ClientRecruitPhrasePatch), nameof(SayPostfix)));
            logger.LogInfo("[PITFIRE_FIKA] Patched Player.Say for host recruit bridge");
            return true;
        }

        private static void SayPostfix(Player __instance, EPhraseTrigger phrase)
        {
            if (!FikaSession.IsRemoteClient() || !PitFireTeamReflection.IsPickupEnabled())
            {
                return;
            }

            if (!PitFireTeamReflection.IsPlayerBoss(__instance.ProfileId))
            {
                return;
            }

            if (phrase == EPhraseTrigger.Cooperation)
            {
                var target = ClientCooperationUiPatch.ResolveRecruitTarget(__instance) ?? __instance.InteractablePlayer;
                if (!ClientCooperationUiPatch.IsValidObservedRecruitTarget(__instance, target))
                {
                    return;
                }

                PitFireRecruitBridge.SendRecruitRequest(__instance.ProfileId, target.ProfileId, phrase);
                return;
            }

            if (phrase == EPhraseTrigger.FollowMe)
            {
                PitFireRecruitBridge.SendRecruitRequest(__instance.ProfileId, null, phrase);
            }
        }
    }
}
