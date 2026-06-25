using System.Collections;
using System.Collections.Concurrent;
using PitFireTeamFikaFix.Patches;

namespace PitFireTeamFikaFix
{
    internal static class ClientGhostReconcile
    {
        private static readonly ConcurrentDictionary<string, byte> Scheduled = new ConcurrentDictionary<string, byte>();

        internal static void Schedule(string profileId)
        {
            if (!FikaSession.IsRemoteClient() || string.IsNullOrEmpty(profileId))
            {
                return;
            }

            if (!Scheduled.TryAdd(profileId, 0))
            {
                return;
            }

            if (PluginCore.Instance != null)
            {
                PluginCore.Instance.StartCoroutine(ReconcileRoutine(profileId));
            }
        }

        internal static void Clear()
        {
            Scheduled.Clear();
        }

        private static IEnumerator ReconcileRoutine(string profileId)
        {
            var delays = new[] { 0.25f, 1f, 3f, 8f };
            for (int i = 0; i < delays.Length; i++)
            {
                yield return new UnityEngine.WaitForSeconds(delays[i]);
                ClientObservedPlayerDedupePatch.SweepProfile(profileId);
            }

            Scheduled.TryRemove(profileId, out _);
        }
    }
}
