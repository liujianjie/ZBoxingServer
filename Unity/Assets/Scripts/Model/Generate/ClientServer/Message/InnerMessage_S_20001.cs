using ProtoBuf;
using System.Collections.Generic;

namespace ET
{
    [ProtoContract]
    [Message(InnerMessage.ObjectQueryRequest)]
    [ResponseType(nameof(ObjectQueryResponse))]
    public partial class ObjectQueryRequest : MessageObject, IRequest
    {
        public static ObjectQueryRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectQueryRequest), isFromPool) as ObjectQueryRequest;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public long Key { get; set; }

        [ProtoMember(3)]
        public long InstanceId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Key = default;
            this.InstanceId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.M2A_Reload)]
    [ResponseType(nameof(A2M_Reload))]
    public partial class M2A_Reload : MessageObject, IRequest
    {
        public static M2A_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2A_Reload), isFromPool) as M2A_Reload;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.A2M_Reload)]
    public partial class A2M_Reload : MessageObject, IResponse
    {
        public static A2M_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(A2M_Reload), isFromPool) as A2M_Reload;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Error { get; set; }

        [ProtoMember(3)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.G2G_LockRequest)]
    [ResponseType(nameof(G2G_LockResponse))]
    public partial class G2G_LockRequest : MessageObject, IRequest
    {
        public static G2G_LockRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2G_LockRequest), isFromPool) as G2G_LockRequest;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public long Id { get; set; }

        [ProtoMember(3)]
        public string Address { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Id = default;
            this.Address = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.G2G_LockResponse)]
    public partial class G2G_LockResponse : MessageObject, IResponse
    {
        public static G2G_LockResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2G_LockResponse), isFromPool) as G2G_LockResponse;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Error { get; set; }

        [ProtoMember(3)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.G2G_LockReleaseRequest)]
    [ResponseType(nameof(G2G_LockReleaseResponse))]
    public partial class G2G_LockReleaseRequest : MessageObject, IRequest
    {
        public static G2G_LockReleaseRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2G_LockReleaseRequest), isFromPool) as G2G_LockReleaseRequest;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public long Id { get; set; }

        [ProtoMember(3)]
        public string Address { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Id = default;
            this.Address = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.G2G_LockReleaseResponse)]
    public partial class G2G_LockReleaseResponse : MessageObject, IResponse
    {
        public static G2G_LockReleaseResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2G_LockReleaseResponse), isFromPool) as G2G_LockReleaseResponse;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Error { get; set; }

        [ProtoMember(3)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.ObjectAddRequest)]
    [ResponseType(nameof(ObjectAddResponse))]
    public partial class ObjectAddRequest : MessageObject, IRequest
    {
        public static ObjectAddRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectAddRequest), isFromPool) as ObjectAddRequest;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Type { get; set; }

        [ProtoMember(3)]
        public long Key { get; set; }

        [ProtoMember(4)]
        public ActorId ActorId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Type = default;
            this.Key = default;
            this.ActorId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.ObjectAddResponse)]
    public partial class ObjectAddResponse : MessageObject, IResponse
    {
        public static ObjectAddResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectAddResponse), isFromPool) as ObjectAddResponse;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Error { get; set; }

        [ProtoMember(3)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.ObjectLockRequest)]
    [ResponseType(nameof(ObjectLockResponse))]
    public partial class ObjectLockRequest : MessageObject, IRequest
    {
        public static ObjectLockRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectLockRequest), isFromPool) as ObjectLockRequest;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Type { get; set; }

        [ProtoMember(3)]
        public long Key { get; set; }

        [ProtoMember(4)]
        public ActorId ActorId { get; set; }

        [ProtoMember(5)]
        public int Time { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Type = default;
            this.Key = default;
            this.ActorId = default;
            this.Time = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.ObjectLockResponse)]
    public partial class ObjectLockResponse : MessageObject, IResponse
    {
        public static ObjectLockResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectLockResponse), isFromPool) as ObjectLockResponse;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Error { get; set; }

        [ProtoMember(3)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.ObjectUnLockRequest)]
    [ResponseType(nameof(ObjectUnLockResponse))]
    public partial class ObjectUnLockRequest : MessageObject, IRequest
    {
        public static ObjectUnLockRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectUnLockRequest), isFromPool) as ObjectUnLockRequest;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Type { get; set; }

        [ProtoMember(3)]
        public long Key { get; set; }

        [ProtoMember(4)]
        public ActorId OldActorId { get; set; }

        [ProtoMember(5)]
        public ActorId NewActorId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Type = default;
            this.Key = default;
            this.OldActorId = default;
            this.NewActorId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.ObjectUnLockResponse)]
    public partial class ObjectUnLockResponse : MessageObject, IResponse
    {
        public static ObjectUnLockResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectUnLockResponse), isFromPool) as ObjectUnLockResponse;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Error { get; set; }

        [ProtoMember(3)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.ObjectRemoveRequest)]
    [ResponseType(nameof(ObjectRemoveResponse))]
    public partial class ObjectRemoveRequest : MessageObject, IRequest
    {
        public static ObjectRemoveRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectRemoveRequest), isFromPool) as ObjectRemoveRequest;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Type { get; set; }

        [ProtoMember(3)]
        public long Key { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Type = default;
            this.Key = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.ObjectRemoveResponse)]
    public partial class ObjectRemoveResponse : MessageObject, IResponse
    {
        public static ObjectRemoveResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectRemoveResponse), isFromPool) as ObjectRemoveResponse;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Error { get; set; }

        [ProtoMember(3)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.ObjectGetRequest)]
    [ResponseType(nameof(ObjectGetResponse))]
    public partial class ObjectGetRequest : MessageObject, IRequest
    {
        public static ObjectGetRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectGetRequest), isFromPool) as ObjectGetRequest;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Type { get; set; }

        [ProtoMember(3)]
        public long Key { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Type = default;
            this.Key = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.ObjectGetResponse)]
    public partial class ObjectGetResponse : MessageObject, IResponse
    {
        public static ObjectGetResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectGetResponse), isFromPool) as ObjectGetResponse;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Error { get; set; }

        [ProtoMember(3)]
        public string Message { get; set; }

        [ProtoMember(4)]
        public int Type { get; set; }

        [ProtoMember(5)]
        public ActorId ActorId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Type = default;
            this.ActorId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.R2G_GetLoginKey)]
    [ResponseType(nameof(G2R_GetLoginKey))]
    public partial class R2G_GetLoginKey : MessageObject, IRequest
    {
        public static R2G_GetLoginKey Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(R2G_GetLoginKey), isFromPool) as R2G_GetLoginKey;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public string Account { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Account = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.G2R_GetLoginKey)]
    public partial class G2R_GetLoginKey : MessageObject, IResponse
    {
        public static G2R_GetLoginKey Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2R_GetLoginKey), isFromPool) as G2R_GetLoginKey;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Error { get; set; }

        [ProtoMember(3)]
        public string Message { get; set; }

        [ProtoMember(4)]
        public long Key { get; set; }

        [ProtoMember(5)]
        public long GateId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Key = default;
            this.GateId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.G2M_SessionDisconnect)]
    public partial class G2M_SessionDisconnect : MessageObject, ILocationMessage
    {
        public static G2M_SessionDisconnect Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2M_SessionDisconnect), isFromPool) as G2M_SessionDisconnect;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.ObjectQueryResponse)]
    public partial class ObjectQueryResponse : MessageObject, IResponse
    {
        public static ObjectQueryResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectQueryResponse), isFromPool) as ObjectQueryResponse;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Error { get; set; }

        [ProtoMember(3)]
        public string Message { get; set; }

        [ProtoMember(4)]
        public byte[] Entity { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Entity = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.M2M_UnitTransferRequest)]
    [ResponseType(nameof(M2M_UnitTransferResponse))]
    public partial class M2M_UnitTransferRequest : MessageObject, IRequest
    {
        public static M2M_UnitTransferRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2M_UnitTransferRequest), isFromPool) as M2M_UnitTransferRequest;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public ActorId OldActorId { get; set; }

        [ProtoMember(3)]
        public byte[] Unit { get; set; }

        [ProtoMember(4)]
        public List<byte[]> Entitys { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.OldActorId = default;
            this.Unit = default;
            this.Entitys.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(InnerMessage.M2M_UnitTransferResponse)]
    public partial class M2M_UnitTransferResponse : MessageObject, IResponse
    {
        public static M2M_UnitTransferResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2M_UnitTransferResponse), isFromPool) as M2M_UnitTransferResponse;
        }

        [ProtoMember(1)]
        public int RpcId { get; set; }

        [ProtoMember(2)]
        public int Error { get; set; }

        [ProtoMember(3)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    public static class InnerMessage
    {
        public const ushort ObjectQueryRequest = 20002;
        public const ushort M2A_Reload = 20003;
        public const ushort A2M_Reload = 20004;
        public const ushort G2G_LockRequest = 20005;
        public const ushort G2G_LockResponse = 20006;
        public const ushort G2G_LockReleaseRequest = 20007;
        public const ushort G2G_LockReleaseResponse = 20008;
        public const ushort ObjectAddRequest = 20009;
        public const ushort ObjectAddResponse = 20010;
        public const ushort ObjectLockRequest = 20011;
        public const ushort ObjectLockResponse = 20012;
        public const ushort ObjectUnLockRequest = 20013;
        public const ushort ObjectUnLockResponse = 20014;
        public const ushort ObjectRemoveRequest = 20015;
        public const ushort ObjectRemoveResponse = 20016;
        public const ushort ObjectGetRequest = 20017;
        public const ushort ObjectGetResponse = 20018;
        public const ushort R2G_GetLoginKey = 20019;
        public const ushort G2R_GetLoginKey = 20020;
        public const ushort G2M_SessionDisconnect = 20021;
        public const ushort ObjectQueryResponse = 20022;
        public const ushort M2M_UnitTransferRequest = 20023;
        public const ushort M2M_UnitTransferResponse = 20024;
    }
}