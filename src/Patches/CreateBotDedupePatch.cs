using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EFT;
using Fika.Core.Main.GameMode;
using HarmonyLib;
using Player = EFT.Player;

namespace PitFireTeamFikaFix.Patches
{
    /// <summary>
    /// P2: parallel HostGameController.CreateBot(profileId) → ghost body + Dictionary.Add crash.
    /// Async: Bots.Add lives in state machine MoveNext — transpiler patches MoveNext, not the async wrapper.
    /// </summary>
    internal static class CreateBotDedupePatch
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> ProfileLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        private static readonly ThreadLocal<string> HeldProfileId = new ThreadLocal<string>();

        internal static bool TryApply(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
        {
            var createBot = AccessTools.Method(
                typeof(HostGameController),
                "CreateBot",
                new[] { typeof(GameWorld), typeof(Profile), typeof(UnityEngine.Vector3) });

            if (createBot == null)
            {
                logger.LogWarning("[PITFIRE_FIKA] HostGameController.CreateBot not found");
                return false;
            }

            harmony.Patch(
                createBot,
                prefix: new HarmonyMethod(typeof(CreateBotDedupePatch), nameof(CreateBotPrefix)),
                postfix: new HarmonyMethod(typeof(CreateBotDedupePatch), nameof(CreateBotPostfix)),
                finalizer: new HarmonyMethod(typeof(CreateBotDedupePatch), nameof(CreateBotFinalizer)));

            var moveNext = ResolveAsyncMoveNext(createBot);
            if (moveNext != null)
            {
                harmony.Patch(
                    moveNext,
                    transpiler: new HarmonyMethod(typeof(CreateBotDedupePatch), nameof(CreateBotTranspiler)));
                logger.LogInfo($"[PITFIRE_FIKA] Patched CreateBot state machine MoveNext ({moveNext.DeclaringType?.Name})");
            }
            else
            {
                harmony.Patch(
                    createBot,
                    transpiler: new HarmonyMethod(typeof(CreateBotDedupePatch), nameof(CreateBotTranspiler)));
                logger.LogWarning("[PITFIRE_FIKA] CreateBot MoveNext not found — transpiler on wrapper only");
            }

            logger.LogInfo("[PITFIRE_FIKA] Patched HostGameController.CreateBot dedupe (async MoveNext + prefix)");
            return true;
        }

        private static MethodInfo ResolveAsyncMoveNext(MethodInfo asyncMethod)
        {
            var attr = asyncMethod.GetCustomAttribute<AsyncStateMachineAttribute>();
            if (attr?.StateMachineType == null)
            {
                return null;
            }

            return AccessTools.Method(attr.StateMachineType, "MoveNext");
        }

        private static bool CreateBotPrefix(HostGameController __instance, Profile profile, ref Task<LocalPlayer> __result)
        {
            if (profile == null || string.IsNullOrEmpty(profile.Id))
            {
                return true;
            }

            var profileId = profile.Id;

            if (__instance.Bots != null && __instance.Bots.TryGetValue(profileId, out var existing) && existing != null)
            {
                PluginCore.FixLogger?.LogInfo($"[PITFIRE_FIKA] CreateBot dedupe (already registered) profileId={profileId}");
                __result = Task.FromResult(existing as LocalPlayer);
                return false;
            }

            var gate = ProfileLocks.GetOrAdd(profileId, _ => new SemaphoreSlim(1, 1));
            gate.Wait();
            HeldProfileId.Value = profileId;

            if (__instance.Bots != null && __instance.Bots.TryGetValue(profileId, out existing) && existing != null)
            {
                ReleaseHeldLock();
                PluginCore.FixLogger?.LogInfo($"[PITFIRE_FIKA] CreateBot dedupe (after wait) profileId={profileId}");
                __result = Task.FromResult(existing as LocalPlayer);
                return false;
            }

            return true;
        }

        private static void CreateBotPostfix(Profile profile, Task<LocalPlayer> __result)
        {
            if (profile == null || string.IsNullOrEmpty(profile.Id))
            {
                ReleaseHeldLock();
                return;
            }

            var profileId = profile.Id;
            if (__result == null)
            {
                ReleaseHeldLock();
                return;
            }

            if (__result.IsCompleted)
            {
                ReleaseHeldLock();
                return;
            }

            _ = __result.ContinueWith(
                _ => ReleaseLock(profileId),
                TaskContinuationOptions.ExecuteSynchronously);
        }

        private static Exception CreateBotFinalizer(Profile profile, Exception __exception)
        {
            ReleaseHeldLock();
            if (profile != null && !string.IsNullOrEmpty(profile.Id))
            {
                ReleaseLock(profile.Id);
            }

            if (IsDuplicateKeyException(__exception))
            {
                PluginCore.FixLogger?.LogWarning(
                    $"[PITFIRE_FIKA] CreateBot swallowed duplicate Bots.Add profileId={profile?.Id}");
                return null;
            }

            return __exception;
        }

        private static IEnumerable<CodeInstruction> CreateBotTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var addMethod = AccessTools.Method(
                typeof(Dictionary<string, Player>),
                nameof(Dictionary<string, Player>.Add));

            var tryAddMethod = AccessTools.Method(
                typeof(CreateBotDedupePatch),
                nameof(TryAddBot));

            var replaced = false;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(addMethod))
                {
                    yield return new CodeInstruction(OpCodes.Call, tryAddMethod);
                    replaced = true;
                    continue;
                }

                yield return instruction;
            }

            if (!replaced)
            {
                PluginCore.FixLogger?.LogWarning("[PITFIRE_FIKA] CreateBot transpiler: Dictionary.Add not found in IL");
            }
        }

        /// <summary>
        /// Replaces Dictionary.Add inside CreateBot MoveNext — returns true if added, false if duplicate.
        /// </summary>
        public static bool TryAddBot(Dictionary<string, Player> bots, string key, Player player)
        {
            if (bots == null)
            {
                return false;
            }

            if (bots.TryGetValue(key, out var existing) && existing != null)
            {
                PluginCore.FixLogger?.LogWarning($"[PITFIRE_FIKA] Bots.Add deduped key={key}");
                if (player != null && !ReferenceEquals(player, existing))
                {
                    TryDisposeDuplicatePlayer(player);
                }

                return false;
            }

            bots.Add(key, player);
            return true;
        }

        private static void TryDisposeDuplicatePlayer(Player player)
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
                PluginCore.FixLogger?.LogWarning($"[PITFIRE_FIKA] Failed to dispose duplicate bot body: {ex.Message}");
            }
        }

        private static bool IsDuplicateKeyException(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is ArgumentException argumentException
                    && argumentException.Message.IndexOf("same key", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ReleaseHeldLock()
        {
            var profileId = HeldProfileId.Value;
            HeldProfileId.Value = null;
            if (!string.IsNullOrEmpty(profileId))
            {
                ReleaseLock(profileId);
            }
        }

        private static void ReleaseLock(string profileId)
        {
            if (string.IsNullOrEmpty(profileId))
            {
                return;
            }

            if (ProfileLocks.TryGetValue(profileId, out var gate))
            {
                try
                {
                    gate.Release();
                }
                catch (SemaphoreFullException)
                {
                    // already released
                }
            }
        }
    }
}
