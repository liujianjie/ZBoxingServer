using System.Collections.Generic;
using ProtoBuf;

namespace ET
{
    [ProtoContract]
    public partial class Replay: Object
    {
        [ProtoMember(1)]
        public List<LockStepUnitInfo> UnitInfos;

        [ProtoMember(2)]
        public List<OneFrameInputs> FrameInputs = new();

        [ProtoMember(3)]
        public List<byte[]> Snapshots = new();
    }
}
