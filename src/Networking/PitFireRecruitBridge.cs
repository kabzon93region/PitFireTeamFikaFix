using System;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using HarmonyLib;
using UnityEngine;

namespace PitFireTeamFikaFix.Networking
{
    internal static class PitFireRecruitBridge
    {
        private const float RecruitPhraseDistance = 15f;

        private static ManualLogSource _logger;
        private static IFikaNetworkManager _networkManager;
        private static bool _packetsRegistered;

        internal static void Initialize(ManualLogSource logger)
        {
            _logger = logger;
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerDestroyedEvent>(OnNetworkManagerDestroyed);
            TryRegisterPackets();
        }

        internal static void Shutdown()
        {
            FikaEventDispatcher.UnsubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
            FikaEventDispatcher.UnsubscribeEvent<FikaNetworkManagerDestroyedEvent>(OnNetworkManagerDestroyed);
            _networkManager = null;
            _packetsRegistered = false;
        }

        internal static void SendRecruitRequest(string requesterProfileId, string targetProfileId, EPhraseTrigger phrase)
        {
            if (!FikaSession.IsRemoteClient() || string.IsNullOrEmpty(requesterProfileId))
            {
                return;
            }

            if (!TryRegisterPackets() || _networkManager == null)
            {
                _logger?.LogWarning("[PITFIRE_FIKA] Recruit request skipped — network unavailable");
                return;
            }

            var packet = new PitFireRecruitRequestPacket
            {
                RequesterProfileId = requesterProfileId,
                TargetProfileId = targetProfileId ?? string.Empty,
                PhraseTrigger = (int)phrase
            };

            _networkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            _logger?.LogInfo($"[PITFIRE_FIKA] Recruit request sent phrase={phrase} target={targetProfileId ?? "(ambient)"}");
        }

        private static void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent evt)
        {
            _networkManager = evt.Manager;
            TryRegisterPackets();
        }

        private static void OnNetworkManagerDestroyed(FikaNetworkManagerDestroyedEvent evt)
        {
            _networkManager = null;
            _packetsRegistered = false;
        }

        private static bool TryRegisterPackets()
        {
            if (_packetsRegistered)
            {
                return _networkManager != null;
            }

            if (_networkManager == null)
            {
                _networkManager = Singleton<FikaServer>.Instance as IFikaNetworkManager
                    ?? Singleton<FikaClient>.Instance as IFikaNetworkManager;
            }

            if (_networkManager == null)
            {
                return false;
            }

            _networkManager.RegisterPacket<PitFireRecruitRequestPacket>(OnRecruitRequestReceived);
            _packetsRegistered = true;
            _logger?.LogInfo("[PITFIRE_FIKA] Recruit bridge packets registered");
            return true;
        }

        private static void OnRecruitRequestReceived(PitFireRecruitRequestPacket packet)
        {
            if (!FikaBackendUtils.IsServer || packet == null)
            {
                return;
            }

            if (!PitFireTeamReflection.IsAvailable() || !PitFireTeamReflection.IsPickupEnabled())
            {
                return;
            }

            if (!PitFireTeamReflection.IsPlayerBoss(packet.RequesterProfileId))
            {
                _logger?.LogWarning($"[PITFIRE_FIKA] Recruit denied — requester is not boss profileId={packet.RequesterProfileId}");
                return;
            }

            if (!Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            var requester = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(packet.RequesterProfileId);
            if (requester == null)
            {
                _logger?.LogWarning($"[PITFIRE_FIKA] Recruit denied — requester not found profileId={packet.RequesterProfileId}");
                return;
            }

            var phrase = (EPhraseTrigger)packet.PhraseTrigger;
            if (phrase == EPhraseTrigger.Cooperation)
            {
                if (!TryFindBotOwner(packet.TargetProfileId, out var targetBot))
                {
                    _logger?.LogWarning($"[PITFIRE_FIKA] Recruit denied — target bot not found profileId={packet.TargetProfileId}");
                    return;
                }

                if (!CanRecruitTarget(requester, targetBot))
                {
                    return;
                }

                var controller = targetBot.BotsGroup?.RequestsController;
                var tryAsk = AccessTools.Method(typeof(BotGroupRequestController), "TryAskFollowMeRequest");
                if (controller != null && tryAsk != null)
                {
                    tryAsk.Invoke(controller, new object[] { requester, targetBot });
                }

                _logger?.LogInfo($"[PITFIRE_FIKA] Host recruit Cooperation target={packet.TargetProfileId}");
                return;
            }

            if (phrase == EPhraseTrigger.FollowMe)
            {
                var sayPhrase = AccessTools.Method(typeof(BotEventHandler), "SayPhrase");
                if (Singleton<BotEventHandler>.Instantiated && sayPhrase != null)
                {
                    sayPhrase.Invoke(Singleton<BotEventHandler>.Instance, new object[] { requester, EPhraseTrigger.FollowMe });
                    _logger?.LogInfo("[PITFIRE_FIKA] Host ambient FollowMe via SayPhrase");
                }
            }
        }

        private static bool CanRecruitTarget(Player requester, BotOwner bot)
        {
            if (bot == null || bot.IsDead || bot.BotState != EBotState.Active)
            {
                return false;
            }

            if (PitFireTeamReflection.IsFollower(bot) || PitFireTeamReflection.IsFollowerProfileId(bot.ProfileId))
            {
                return false;
            }

            if (requester.Side != bot.Side)
            {
                return false;
            }

            if (bot.Memory?.HaveEnemy == true)
            {
                return false;
            }

            float maxDist = RecruitPhraseDistance * RecruitPhraseDistance;
            if ((bot.Position - requester.Position).sqrMagnitude > maxDist)
            {
                return false;
            }

            return true;
        }

        internal static bool TryFindBotOwner(string profileId, out BotOwner botOwner)
        {
            botOwner = null;
            if (string.IsNullOrEmpty(profileId) || !Singleton<GameWorld>.Instantiated)
            {
                return false;
            }

            var player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(profileId);
            if (player?.AIData?.BotOwner != null && !player.AIData.BotOwner.IsDead)
            {
                botOwner = player.AIData.BotOwner;
                return true;
            }

            var botGame = Singleton<IBotGame>.Instance;
            var controller = botGame?.BotsController;
            if (controller?.Bots == null)
            {
                return false;
            }

            foreach (var candidate in controller.Bots.BotOwners)
            {
                if (candidate != null && string.Equals(candidate.ProfileId, profileId, StringComparison.Ordinal))
                {
                    botOwner = candidate;
                    return !botOwner.IsDead;
                }
            }

            return false;
        }
    }
}
