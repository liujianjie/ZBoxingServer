using ProtoBuf;

namespace ET
{
    [Message(ushort.MaxValue)]
    [ProtoContract]
    public partial class MessageResponse: MessageObject, IResponse
    {
        [ProtoMember(1)]
        public int RpcId { get; set; }
        [ProtoMember(2)]
        public int Error { get; set; }
        [ProtoMember(3)]
        public string Message { get; set; }
    }
}