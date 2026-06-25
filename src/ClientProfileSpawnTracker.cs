using System.Collections.Concurrent;



namespace PitFireTeamFikaFix

{

    /// <summary>

    /// Client-side guard against duplicate SendCharacter or QueueProfile calls for the same AI profile.

    /// Fika normal flow uses both once per bot — track each path separately.

    /// </summary>

    internal static class ClientProfileSpawnTracker

    {

        private static readonly ConcurrentDictionary<string, byte> CharacterSentProfiles = new ConcurrentDictionary<string, byte>();

        private static readonly ConcurrentDictionary<string, byte> QueuedProfiles = new ConcurrentDictionary<string, byte>();



        public static bool TryTrackCharacterSent(string profileId)

        {

            if (string.IsNullOrEmpty(profileId))

            {

                return true;

            }



            return CharacterSentProfiles.TryAdd(profileId, 0);

        }



        public static bool TryTrackProfileQueued(string profileId)

        {

            if (string.IsNullOrEmpty(profileId))

            {

                return true;

            }



            return QueuedProfiles.TryAdd(profileId, 0);

        }



        public static void Clear()

        {

            CharacterSentProfiles.Clear();

            QueuedProfiles.Clear();

        }

    }

}


