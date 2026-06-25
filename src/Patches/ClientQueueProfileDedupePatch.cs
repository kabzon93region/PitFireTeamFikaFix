using System;
using System.Reflection;
using EFT;
using Fika.Core.Main.Components;
using HarmonyLib;
using UnityEngine;

namespace PitFireTeamFikaFix.Patches
{
    /// <summary>
    /// Client: reject duplicate SendCharacter or duplicate QueueProfile for the same AI profile.
    /// Fika spawns bots via SendCharacter then QueueProfile — both must be allowed once per profileId.
    /// </summary>
    internal static class ClientQueueProfileDedupePatch
    {
        internal static bool TryApply(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
        {
            try
            {
                var queueMethod = AccessTools.Method(
                    typeof(CoopHandler),
                    "QueueProfile",
                    new[]
                    {
                        typeof(Profile),
                        typeof(byte[]),
                        typeof(Vector3),
                        typeof(int),
                        typeof(bool),
                        typeof(bool),
                        typeof(MongoID),
                        typeof(ushort),
                        typeof(bool),
                        typeof(MongoID?),
                        typeof(EHandsControllerType)
                    });

                if (queueMethod == null)
                {
                    logger.LogWarning("[PITFIRE_FIKA] CoopHandler.QueueProfile not found");
                    return false;
                }

                harmony.Patch(
                    queueMethod,
                    prefix: new HarmonyMethod(typeof(ClientQueueProfileDedupePatch), nameof(QueueProfilePrefix)));

                var executeMethod = AccessTools.Method(
                    typeof(Fika.Core.Networking.Packets.Generic.SubPackets.SendCharacterPacket),
                    "Execute");

                if (executeMethod != null)
                {
                    harmony.Patch(
                        executeMethod,
                        prefix: new HarmonyMethod(typeof(ClientQueueProfileDedupePatch), nameof(SendCharacterExecutePrefix)));
                }

                logger.LogInfo("[PITFIRE_FIKA] Patched client QueueProfile / SendCharacter dedupe");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"[PITFIRE_FIKA] Client QueueProfile dedupe failed: {ex.Message}");
                return false;
            }
        }

        private static bool QueueProfilePrefix(Profile profile, bool isAI)
        {
            if (!FikaSession.IsRemoteClient() || !isAI || profile == null)
            {
                return true;
            }

            if (ClientProfileSpawnTracker.TryTrackProfileQueued(profile.ProfileId))
            {
                return true;
            }

            PluginCore.FixLogger?.LogWarning(
                $"[PITFIRE_FIKA] Blocked duplicate QueueProfile on client profileId={profile.ProfileId}");
            return false;
        }

        private static bool SendCharacterExecutePrefix(
            Fika.Core.Networking.Packets.Generic.SubPackets.SendCharacterPacket __instance)
        {
            if (!FikaSession.IsRemoteClient() || !__instance.IsAI)
            {
                return true;
            }

            var profile = __instance.PlayerInfoPacket.Profile;
            if (profile == null)
            {
                return true;
            }

            if (ClientProfileSpawnTracker.TryTrackCharacterSent(profile.ProfileId))
            {
                return true;
            }

            PluginCore.FixLogger?.LogWarning(
                $"[PITFIRE_FIKA] Blocked duplicate SendCharacter.Execute on client profileId={profile.ProfileId}");
            return false;
        }
    }
}
