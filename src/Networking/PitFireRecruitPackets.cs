using Fika.Core.Networking.LiteNetLib.Utils;

namespace PitFireTeamFikaFix.Networking
{
    internal sealed class PitFireRecruitRequestPacket : INetSerializable
    {
        public string RequesterProfileId = string.Empty;
        public string TargetProfileId = string.Empty;
        public int PhraseTrigger;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RequesterProfileId ?? string.Empty);
            writer.Put(TargetProfileId ?? string.Empty);
            writer.Put(PhraseTrigger);
        }

        public void Deserialize(NetDataReader reader)
        {
            RequesterProfileId = reader.GetString();
            TargetProfileId = reader.GetString();
            PhraseTrigger = reader.GetInt();
        }
    }
}
