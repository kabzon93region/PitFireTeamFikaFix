using System;

using System.Collections.Generic;

using System.Reflection;

using System.Threading.Tasks;

using Comfort.Common;

using EFT;

using Fika.Core.Networking;

using HarmonyLib;

using UnityEngine;

using Player = EFT.Player;



namespace PitFireTeamFikaFix.Patches

{

    /// <summary>

    /// Fika client: remove stray duplicate AI bodies for host PitFire companions (ghost at spawn).

    /// Prefer keeping the networked ObservedPlayer; drop static spawn duplicates on client only.

    /// </summary>

    internal static class ClientObservedPlayerDedupePatch

    {

        private static FieldInfo _observedNetIdField;



        internal static bool TryApply(Harmony harmony, BepInEx.Logging.ManualLogSource logger)

        {

            try

            {

                var observedType = AccessTools.TypeByName("Fika.Core.Main.Players.ObservedPlayer");

                var createMethod = observedType != null

                    ? AccessTools.Method(observedType, "CreateObservedPlayer")

                    : null;



                if (createMethod == null)

                {

                    logger.LogWarning("[PITFIRE_FIKA] ObservedPlayer.CreateObservedPlayer not found");

                    return false;

                }



                _observedNetIdField = observedType?.GetField("NetId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);



                harmony.Patch(

                    createMethod,

                    postfix: new HarmonyMethod(typeof(ClientObservedPlayerDedupePatch), nameof(CreateObservedPlayerPostfix)));

                logger.LogInfo("[PITFIRE_FIKA] Patched ObservedPlayer.CreateObservedPlayer async dedupe");

                return true;

            }

            catch (Exception ex)

            {

                logger.LogError($"[PITFIRE_FIKA] ObservedPlayer dedupe patch failed: {ex.Message}");

                return false;

            }

        }



        private static void CreateObservedPlayerPostfix(object __result, Profile profile, bool aiControl)

        {

            if (!FikaSession.IsRemoteClient() || !aiControl || profile == null || __result == null)

            {

                return;

            }



            ClientGhostReconcile.Schedule(profile.Id);



            if (__result is Task task)

            {

                _ = AwaitAndReconcileAsync(task, profile.Id);

            }

        }



        private static async Task AwaitAndReconcileAsync(Task playerTask, string profileId)

        {

            try

            {

                await playerTask.ConfigureAwait(false);



                if (playerTask.IsFaulted || playerTask.IsCanceled)

                {

                    return;

                }



                var resultProperty = playerTask.GetType().GetProperty("Result");

                var canonical = resultProperty?.GetValue(playerTask) as Player;

                if (canonical == null || string.IsNullOrEmpty(profileId))

                {

                    return;

                }



                SweepProfile(profileId);

            }

            catch (Exception ex)

            {

                PluginCore.FixLogger?.LogWarning($"[PITFIRE_FIKA] Async dedupe failed profileId={profileId}: {ex.Message}");

            }

        }



        internal static void SweepProfile(string profileId)

        {

            if (!FikaSession.IsRemoteClient() || string.IsNullOrEmpty(profileId))

            {

                return;

            }



            if (!Singleton<GameWorld>.Instantiated)

            {

                return;

            }



            var duplicates = CollectDuplicates(profileId);

            if (duplicates.Count <= 1)

            {

                return;

            }



            var canonical = PickCanonical(duplicates);

            if (canonical == null)

            {

                return;

            }



            for (int i = 0; i < duplicates.Count; i++)

            {

                var candidate = duplicates[i];

                if (candidate == null || ReferenceEquals(candidate, canonical))

                {

                    continue;

                }



                PluginCore.FixLogger?.LogWarning(

                    $"[PITFIRE_FIKA] Removing client duplicate AI body profileId={profileId} type={candidate.GetType().Name} netId={GetObservedNetId(candidate)} keepNetId={GetObservedNetId(canonical)}");

                TryDisposePlayer(candidate);

            }

        }



        private static List<Player> CollectDuplicates(string profileId)

        {

            var duplicates = new List<Player>();

            var world = Singleton<GameWorld>.Instance;

            var players = world.AllAlivePlayersList;

            if (players == null)

            {

                return duplicates;

            }



            for (int i = 0; i < players.Count; i++)

            {

                var candidate = players[i] as Player;

                if (candidate == null)

                {

                    continue;

                }



                if (!string.Equals(candidate.ProfileId, profileId, StringComparison.Ordinal))

                {

                    continue;

                }



                duplicates.Add(candidate);

            }



            return duplicates;

        }



        private static Player PickCanonical(List<Player> duplicates)

        {

            Player best = null;

            var bestScore = int.MinValue;



            for (int i = 0; i < duplicates.Count; i++)

            {

                var player = duplicates[i];

                if (player == null)

                {

                    continue;

                }



                var score = ScoreCandidate(player);

                if (score > bestScore)

                {

                    bestScore = score;

                    best = player;

                }

            }



            return best;

        }



        private static int ScoreCandidate(Player player)

        {

            var score = 0;



            if (IsCoopRegisteredPlayer(player))

            {

                score += 1000;

            }



            var netId = GetObservedNetId(player);

            if (netId >= 0)

            {

                score += netId;

            }



            var position = player.Transform != null ? player.Transform.position : Vector3.zero;

            if (position.y < -4000f)

            {

                score -= 500;

            }



            if (HasActiveMovement(player))

            {

                score += 100;

            }



            return score;

        }



        private static bool HasActiveMovement(Player player)

        {

            try

            {

                var movementContext = player.MovementContext;

                if (movementContext == null)

                {

                    return false;

                }



                return movementContext.IsAI || movementContext.PlayerAnimator != null;

            }

            catch

            {

                return false;

            }

        }



        private static int GetObservedNetId(Player player)

        {

            if (player == null || _observedNetIdField == null)

            {

                return -1;

            }



            try

            {

                var value = _observedNetIdField.GetValue(player);

                return value is int netId ? netId : -1;

            }

            catch

            {

                return -1;

            }

        }



        private static bool IsCoopRegisteredPlayer(Player player)

        {

            try

            {

                var networkManager = Singleton<IFikaNetworkManager>.Instance;

                var handler = networkManager?.CoopHandler;

                if (handler == null || player == null)

                {

                    return false;

                }



                foreach (var registered in handler.Players.Values)

                {

                    if (ReferenceEquals(registered, player))

                    {

                        return true;

                    }

                }

            }

            catch

            {

                // ignore

            }



            return false;

        }



        private static void TryDisposePlayer(Player player)

        {

            try

            {

                if (player == null)

                {

                    return;

                }



                var dispose = AccessTools.Method(typeof(Player), "Dispose");

                dispose?.Invoke(player, null);



                if (player.gameObject != null)

                {

                    UnityEngine.Object.Destroy(player.gameObject);

                }

            }

            catch (Exception ex)

            {

                PluginCore.FixLogger?.LogWarning($"[PITFIRE_FIKA] Failed to dispose duplicate player: {ex.Message}");

            }

        }

    }

}


