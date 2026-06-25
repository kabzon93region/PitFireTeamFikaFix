using System;
using Fika.Core.Main.Utils;
using HarmonyLib;

namespace PitFireTeamFikaFix
{
    internal static class FikaSession
    {
        private const string FikaBackendUtilsType = "Fika.Core.Main.Utils.FikaBackendUtils";

        public static bool IsFikaLoaded()
        {
            return AccessTools.TypeByName(FikaBackendUtilsType) != null;
        }

        public static bool IsCoopSession()
        {
            if (!IsFikaLoaded())
            {
                return false;
            }

            try
            {
                return FikaBackendUtils.IsServer || FikaBackendUtils.IsClient;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsHost()
        {
            return IsCoopSession() && FikaBackendUtils.IsServer;
        }

        public static bool IsRemoteClient()
        {
            return IsCoopSession() && FikaBackendUtils.IsClient;
        }
    }
}
