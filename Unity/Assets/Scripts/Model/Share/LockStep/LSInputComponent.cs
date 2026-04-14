using ProtoBuf;

namespace ET
{
    [ComponentOf(typeof(LSUnit))]
    [ProtoContract]
    public partial class LSInputComponent: LSEntity, ILSUpdate, IAwake, ISerializeToEntity
    {
        public LSInput LSInput { get; set; }
    }
}
