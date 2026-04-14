using ProtoBuf;
using System.Collections.Generic;

namespace ET
{
    [ProtoContract]
    [Message(ClientMessage.Main2NetClient_Login)]
    [ResponseType(nameof(NetClient2Main_Login))]
    public partial class Main2NetClient_Login : MessageObject, IRequest
    {
        public static Main2NetClient_Login Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(Main2NetClient_Login), isFromPool) as Main2NetClient_Login;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int OwnerFiberId { get; set; }

        /// <summary>
        /// 账号
        /// </summary>
        [ProtoMember(3)]
        public string Account { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [ProtoMember(4)]
        public string Password { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.OwnerFiberId = default;
            this.Account = default;
            this.Password = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(ClientMessage.NetClient2Main_Login)]
    public partial class NetClient2Main_Login : MessageObject, IResponse
    {
        public static NetClient2Main_Login Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(NetClient2Main_Login), isFromPool) as NetClient2Main_Login;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Error { get; set; }

        [ProtoMember(3)]
        public string Message { get; set; }

        [ProtoMember(4)]
        public long PlayerId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.PlayerId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    public static class ClientMessage
    {
        public const ushort Main2NetClient_Login = 1001;
        public const ushort NetClient2Main_Login = 1002;
    }
}