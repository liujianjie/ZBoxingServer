using ProtoBuf;
using System.Collections.Generic;

namespace ET
{
    // ============================================================
    // ZBoxing 共享数据类型
    // ============================================================
    [ProtoContract]
    [Message(ZBoxingOuter.ZBPlayerBrief)]
    public partial class ZBPlayerBrief : MessageObject
    {
        public static ZBPlayerBrief Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ZBPlayerBrief), isFromPool) as ZBPlayerBrief;
        }

        [ProtoMember(1)]
        public long PlayerId { get; set; }

        [ProtoMember(2)]
        public string Nickname { get; set; }

        [ProtoMember(3)]
        public int Gold { get; set; }

        [ProtoMember(4)]
        public int WinCount { get; set; }

        [ProtoMember(5)]
        public int LoseCount { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.PlayerId = default;
            this.Nickname = default;
            this.Gold = default;
            this.WinCount = default;
            this.LoseCount = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(ZBoxingOuter.ZBRoomInfo)]
    public partial class ZBRoomInfo : MessageObject
    {
        public static ZBRoomInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ZBRoomInfo), isFromPool) as ZBRoomInfo;
        }

        [ProtoMember(1)]
        public int RoomId { get; set; }

        [ProtoMember(2)]
        public string RoomName { get; set; }

        [ProtoMember(3)]
        public ZBPlayerBrief Host { get; set; }

        [ProtoMember(4)]
        public ZBPlayerBrief Guest { get; set; }

        [ProtoMember(5)]
        public bool HostReady { get; set; }

        [ProtoMember(6)]
        public bool GuestReady { get; set; }

        /// <summary>
        /// 0=等待中, 1=已满, 2=对战中
        /// </summary>
        [ProtoMember(7)]
        public int State { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RoomId = default;
            this.RoomName = default;
            this.Host = default;
            this.Guest = default;
            this.HostReady = default;
            this.GuestReady = default;
            this.State = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(ZBoxingOuter.ZBPlayerState)]
    public partial class ZBPlayerState : MessageObject
    {
        public static ZBPlayerState Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ZBPlayerState), isFromPool) as ZBPlayerState;
        }

        [ProtoMember(1)]
        public long PlayerId { get; set; }

        [ProtoMember(2)]
        public float PosX { get; set; }

        [ProtoMember(3)]
        public float PosY { get; set; }

        [ProtoMember(4)]
        public int Hp { get; set; }

        [ProtoMember(5)]
        public int Stamina { get; set; }

        [ProtoMember(6)]
        public int AnimState { get; set; }

        [ProtoMember(7)]
        public int AnimFrame { get; set; }

        [ProtoMember(8)]
        public bool FacingRight { get; set; }

        [ProtoMember(9)]
        public int ComboCount { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.PlayerId = default;
            this.PosX = default;
            this.PosY = default;
            this.Hp = default;
            this.Stamina = default;
            this.AnimState = default;
            this.AnimFrame = default;
            this.FacingRight = default;
            this.ComboCount = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // ============================================================
    // 系统协议
    // ============================================================
    [ProtoContract]
    [Message(ZBoxingOuter.C2G_ZBHeartbeat)]
    [ResponseType(nameof(G2C_ZBHeartbeat))]
    public partial class C2G_ZBHeartbeat : MessageObject, ISessionRequest
    {
        public static C2G_ZBHeartbeat Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_ZBHeartbeat), isFromPool) as C2G_ZBHeartbeat;
        }

        [ProtoMember(1)]
        public long ClientTime { get; set; }

        [ProtoMember(90)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ClientTime = default;
            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(ZBoxingOuter.G2C_ZBHeartbeat)]
    public partial class G2C_ZBHeartbeat : MessageObject, ISessionResponse
    {
        public static G2C_ZBHeartbeat Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_ZBHeartbeat), isFromPool) as G2C_ZBHeartbeat;
        }

        [ProtoMember(1)]
        public long ServerTime { get; set; }

        [ProtoMember(90)]
        public int RpcId { get; set; }

        [ProtoMember(91)]
        public int Error { get; set; }

        [ProtoMember(92)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ServerTime = default;
            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    /// <summary>
    /// 服务端踢人通知
    /// </summary>
    [ProtoContract]
    [Message(ZBoxingOuter.G2C_ZBKick)]
    public partial class G2C_ZBKick : MessageObject, ISessionMessage
    {
        public static G2C_ZBKick Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_ZBKick), isFromPool) as G2C_ZBKick;
        }

        /// <summary>
        /// 1=重复登录, 2=服务器维护
        /// </summary>
        [ProtoMember(1)]
        public int Reason { get; set; }

        [ProtoMember(2)]
        public string KickMessage { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Reason = default;
            this.KickMessage = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // ============================================================
    // 认证协议
    // ============================================================
    [ProtoContract]
    [Message(ZBoxingOuter.C2G_ZBLogin)]
    [ResponseType(nameof(G2C_ZBLogin))]
    public partial class C2G_ZBLogin : MessageObject, ISessionRequest
    {
        public static C2G_ZBLogin Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_ZBLogin), isFromPool) as C2G_ZBLogin;
        }

        [ProtoMember(1)]
        public string Username { get; set; }

        [ProtoMember(2)]
        public string Password { get; set; }

        [ProtoMember(90)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Username = default;
            this.Password = default;
            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(ZBoxingOuter.G2C_ZBLogin)]
    public partial class G2C_ZBLogin : MessageObject, ISessionResponse
    {
        public static G2C_ZBLogin Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_ZBLogin), isFromPool) as G2C_ZBLogin;
        }

        [ProtoMember(1)]
        public int ErrorCode { get; set; }

        [ProtoMember(2)]
        public ZBPlayerBrief Player { get; set; }

        [ProtoMember(90)]
        public int RpcId { get; set; }

        [ProtoMember(91)]
        public int Error { get; set; }

        [ProtoMember(92)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ErrorCode = default;
            this.Player = default;
            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(ZBoxingOuter.C2G_ZBRegister)]
    [ResponseType(nameof(G2C_ZBRegister))]
    public partial class C2G_ZBRegister : MessageObject, ISessionRequest
    {
        public static C2G_ZBRegister Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_ZBRegister), isFromPool) as C2G_ZBRegister;
        }

        [ProtoMember(1)]
        public string Username { get; set; }

        [ProtoMember(2)]
        public string Password { get; set; }

        [ProtoMember(3)]
        public string Nickname { get; set; }

        [ProtoMember(90)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Username = default;
            this.Password = default;
            this.Nickname = default;
            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(ZBoxingOuter.G2C_ZBRegister)]
    public partial class G2C_ZBRegister : MessageObject, ISessionResponse
    {
        public static G2C_ZBRegister Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_ZBRegister), isFromPool) as G2C_ZBRegister;
        }

        [ProtoMember(1)]
        public int ErrorCode { get; set; }

        [ProtoMember(2)]
        public ZBPlayerBrief Player { get; set; }

        [ProtoMember(90)]
        public int RpcId { get; set; }

        [ProtoMember(91)]
        public int Error { get; set; }

        [ProtoMember(92)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ErrorCode = default;
            this.Player = default;
            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // ============================================================
    // 大厅协议
    // ============================================================
    [ProtoContract]
    [Message(ZBoxingOuter.C2G_ZBCreateRoom)]
    [ResponseType(nameof(G2C_ZBCreateRoom))]
    public partial class C2G_ZBCreateRoom : MessageObject, ISessionRequest
    {
        public static C2G_ZBCreateRoom Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_ZBCreateRoom), isFromPool) as C2G_ZBCreateRoom;
        }

        [ProtoMember(1)]
        public string RoomName { get; set; }

        [ProtoMember(90)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RoomName = default;
            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(ZBoxingOuter.G2C_ZBCreateRoom)]
    public partial class G2C_ZBCreateRoom : MessageObject, ISessionResponse
    {
        public static G2C_ZBCreateRoom Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_ZBCreateRoom), isFromPool) as G2C_ZBCreateRoom;
        }

        [ProtoMember(1)]
        public int ErrorCode { get; set; }

        [ProtoMember(2)]
        public ZBRoomInfo Room { get; set; }

        [ProtoMember(90)]
        public int RpcId { get; set; }

        [ProtoMember(91)]
        public int Error { get; set; }

        [ProtoMember(92)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ErrorCode = default;
            this.Room = default;
            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(ZBoxingOuter.C2G_ZBJoinRoom)]
    [ResponseType(nameof(G2C_ZBJoinRoom))]
    public partial class C2G_ZBJoinRoom : MessageObject, ISessionRequest
    {
        public static C2G_ZBJoinRoom Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_ZBJoinRoom), isFromPool) as C2G_ZBJoinRoom;
        }

        [ProtoMember(1)]
        public int RoomId { get; set; }

        [ProtoMember(90)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RoomId = default;
            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(ZBoxingOuter.G2C_ZBJoinRoom)]
    public partial class G2C_ZBJoinRoom : MessageObject, ISessionResponse
    {
        public static G2C_ZBJoinRoom Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_ZBJoinRoom), isFromPool) as G2C_ZBJoinRoom;
        }

        [ProtoMember(1)]
        public int ErrorCode { get; set; }

        [ProtoMember(2)]
        public ZBRoomInfo Room { get; set; }

        [ProtoMember(90)]
        public int RpcId { get; set; }

        [ProtoMember(91)]
        public int Error { get; set; }

        [ProtoMember(92)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ErrorCode = default;
            this.Room = default;
            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(ZBoxingOuter.C2G_ZBLeaveRoom)]
    [ResponseType(nameof(G2C_ZBLeaveRoom))]
    public partial class C2G_ZBLeaveRoom : MessageObject, ISessionRequest
    {
        public static C2G_ZBLeaveRoom Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_ZBLeaveRoom), isFromPool) as C2G_ZBLeaveRoom;
        }

        [ProtoMember(90)]
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
    [Message(ZBoxingOuter.G2C_ZBLeaveRoom)]
    public partial class G2C_ZBLeaveRoom : MessageObject, ISessionResponse
    {
        public static G2C_ZBLeaveRoom Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_ZBLeaveRoom), isFromPool) as G2C_ZBLeaveRoom;
        }

        [ProtoMember(1)]
        public int ErrorCode { get; set; }

        [ProtoMember(90)]
        public int RpcId { get; set; }

        [ProtoMember(91)]
        public int Error { get; set; }

        [ProtoMember(92)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ErrorCode = default;
            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(ZBoxingOuter.C2G_ZBRoomList)]
    [ResponseType(nameof(G2C_ZBRoomList))]
    public partial class C2G_ZBRoomList : MessageObject, ISessionRequest
    {
        public static C2G_ZBRoomList Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_ZBRoomList), isFromPool) as C2G_ZBRoomList;
        }

        [ProtoMember(90)]
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
    [Message(ZBoxingOuter.G2C_ZBRoomList)]
    public partial class G2C_ZBRoomList : MessageObject, ISessionResponse
    {
        public static G2C_ZBRoomList Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_ZBRoomList), isFromPool) as G2C_ZBRoomList;
        }

        [ProtoMember(1)]
        public List<ZBRoomInfo> Rooms { get; set; } = new();

        [ProtoMember(90)]
        public int RpcId { get; set; }

        [ProtoMember(91)]
        public int Error { get; set; }

        [ProtoMember(92)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Rooms.Clear();
            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(ZBoxingOuter.C2G_ZBReady)]
    [ResponseType(nameof(G2C_ZBReady))]
    public partial class C2G_ZBReady : MessageObject, ISessionRequest
    {
        public static C2G_ZBReady Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_ZBReady), isFromPool) as C2G_ZBReady;
        }

        [ProtoMember(1)]
        public bool Ready { get; set; }

        [ProtoMember(90)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Ready = default;
            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(ZBoxingOuter.G2C_ZBReady)]
    public partial class G2C_ZBReady : MessageObject, ISessionResponse
    {
        public static G2C_ZBReady Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_ZBReady), isFromPool) as G2C_ZBReady;
        }

        [ProtoMember(1)]
        public int ErrorCode { get; set; }

        [ProtoMember(90)]
        public int RpcId { get; set; }

        [ProtoMember(91)]
        public int Error { get; set; }

        [ProtoMember(92)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ErrorCode = default;
            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(ZBoxingOuter.C2G_ZBMatch)]
    [ResponseType(nameof(G2C_ZBMatch))]
    public partial class C2G_ZBMatch : MessageObject, ISessionRequest
    {
        public static C2G_ZBMatch Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_ZBMatch), isFromPool) as C2G_ZBMatch;
        }

        /// <summary>
        /// false=开始匹配, true=取消匹配
        /// </summary>
        [ProtoMember(1)]
        public bool Cancel { get; set; }

        [ProtoMember(90)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Cancel = default;
            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [ProtoContract]
    [Message(ZBoxingOuter.G2C_ZBMatch)]
    public partial class G2C_ZBMatch : MessageObject, ISessionResponse
    {
        public static G2C_ZBMatch Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_ZBMatch), isFromPool) as G2C_ZBMatch;
        }

        [ProtoMember(1)]
        public int ErrorCode { get; set; }

        /// <summary>
        /// 当前是否在队列中
        /// </summary>
        [ProtoMember(2)]
        public bool InQueue { get; set; }

        [ProtoMember(90)]
        public int RpcId { get; set; }

        [ProtoMember(91)]
        public int Error { get; set; }

        [ProtoMember(92)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ErrorCode = default;
            this.InQueue = default;
            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    /// <summary>
    /// 服务端推送: 匹配成功（自动创建房间）
    /// </summary>
    [ProtoContract]
    [Message(ZBoxingOuter.G2C_ZBMatchFound)]
    public partial class G2C_ZBMatchFound : MessageObject, ISessionMessage
    {
        public static G2C_ZBMatchFound Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_ZBMatchFound), isFromPool) as G2C_ZBMatchFound;
        }

        [ProtoMember(1)]
        public ZBRoomInfo Room { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Room = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    /// <summary>
    /// 服务端推送: 房间状态更新
    /// </summary>
    [ProtoContract]
    [Message(ZBoxingOuter.G2C_ZBRoomUpdate)]
    public partial class G2C_ZBRoomUpdate : MessageObject, ISessionMessage
    {
        public static G2C_ZBRoomUpdate Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_ZBRoomUpdate), isFromPool) as G2C_ZBRoomUpdate;
        }

        [ProtoMember(1)]
        public ZBRoomInfo Room { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Room = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    /// <summary>
    /// 服务端推送: 对战开始
    /// </summary>
    [ProtoContract]
    [Message(ZBoxingOuter.G2C_ZBBattleStart)]
    public partial class G2C_ZBBattleStart : MessageObject, ISessionMessage
    {
        public static G2C_ZBBattleStart Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_ZBBattleStart), isFromPool) as G2C_ZBBattleStart;
        }

        [ProtoMember(1)]
        public int RoomId { get; set; }

        [ProtoMember(2)]
        public int Countdown { get; set; }

        [ProtoMember(3)]
        public long Player1Id { get; set; }

        [ProtoMember(4)]
        public long Player2Id { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RoomId = default;
            this.Countdown = default;
            this.Player1Id = default;
            this.Player2Id = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // ============================================================
    // 战斗协议
    // ============================================================
    /// <summary>
    /// 客户端帧输入(每帧发送,无需响应)
    /// </summary>
    [ProtoContract]
    [Message(ZBoxingOuter.C2G_ZBBattleInput)]
    public partial class C2G_ZBBattleInput : MessageObject, ISessionMessage
    {
        public static C2G_ZBBattleInput Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_ZBBattleInput), isFromPool) as C2G_ZBBattleInput;
        }

        [ProtoMember(1)]
        public int Frame { get; set; }

        /// <summary>
        /// -1=左, 0=静止, 1=右
        /// </summary>
        [ProtoMember(2)]
        public int MoveDir { get; set; }

        /// <summary>
        /// 0=无, 1=Jab, 2=Cross, 3=Hook, 4=Uppercut, 5=Block, 6=Dodge
        /// </summary>
        [ProtoMember(3)]
        public int Action { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Frame = default;
            this.MoveDir = default;
            this.Action = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    /// <summary>
    /// 服务端状态快照(每Tick广播)
    /// </summary>
    [ProtoContract]
    [Message(ZBoxingOuter.G2C_ZBBattleSnapshot)]
    public partial class G2C_ZBBattleSnapshot : MessageObject, ISessionMessage
    {
        public static G2C_ZBBattleSnapshot Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_ZBBattleSnapshot), isFromPool) as G2C_ZBBattleSnapshot;
        }

        [ProtoMember(1)]
        public int ServerFrame { get; set; }

        [ProtoMember(2)]
        public ZBPlayerState Player1 { get; set; }

        [ProtoMember(3)]
        public ZBPlayerState Player2 { get; set; }

        [ProtoMember(4)]
        public int RemainingTime { get; set; }

        /// <summary>
        /// 0=倒计时, 1=战斗中, 2=KO, 3=时间到
        /// </summary>
        [ProtoMember(5)]
        public int BattlePhase { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ServerFrame = default;
            this.Player1 = default;
            this.Player2 = default;
            this.RemainingTime = default;
            this.BattlePhase = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    /// <summary>
    /// 服务端事件通知(命中/格挡等)
    /// </summary>
    [ProtoContract]
    [Message(ZBoxingOuter.G2C_ZBBattleEvent)]
    public partial class G2C_ZBBattleEvent : MessageObject, ISessionMessage
    {
        public static G2C_ZBBattleEvent Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_ZBBattleEvent), isFromPool) as G2C_ZBBattleEvent;
        }

        /// <summary>
        /// 1=命中, 2=格挡, 3=闪避, 4=KO
        /// </summary>
        [ProtoMember(1)]
        public int EventType { get; set; }

        [ProtoMember(2)]
        public long AttackerId { get; set; }

        [ProtoMember(3)]
        public long DefenderId { get; set; }

        [ProtoMember(4)]
        public int Damage { get; set; }

        [ProtoMember(5)]
        public int MoveType { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.EventType = default;
            this.AttackerId = default;
            this.DefenderId = default;
            this.Damage = default;
            this.MoveType = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    /// <summary>
    /// 战斗结束
    /// </summary>
    [ProtoContract]
    [Message(ZBoxingOuter.G2C_ZBBattleEnd)]
    public partial class G2C_ZBBattleEnd : MessageObject, ISessionMessage
    {
        public static G2C_ZBBattleEnd Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_ZBBattleEnd), isFromPool) as G2C_ZBBattleEnd;
        }

        /// <summary>
        /// 0=平局
        /// </summary>
        [ProtoMember(1)]
        public long WinnerId { get; set; }

        /// <summary>
        /// 1=KO, 2=时间到, 3=投降
        /// </summary>
        [ProtoMember(2)]
        public int Reason { get; set; }

        [ProtoMember(3)]
        public int GoldReward { get; set; }

        [ProtoMember(4)]
        public int Player1Hp { get; set; }

        [ProtoMember(5)]
        public int Player2Hp { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.WinnerId = default;
            this.Reason = default;
            this.GoldReward = default;
            this.Player1Hp = default;
            this.Player2Hp = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    public static class ZBoxingOuter
    {
        public const ushort ZBPlayerBrief = 30002;
        public const ushort ZBRoomInfo = 30003;
        public const ushort ZBPlayerState = 30004;
        public const ushort C2G_ZBHeartbeat = 30005;
        public const ushort G2C_ZBHeartbeat = 30006;
        public const ushort G2C_ZBKick = 30007;
        public const ushort C2G_ZBLogin = 30008;
        public const ushort G2C_ZBLogin = 30009;
        public const ushort C2G_ZBRegister = 30010;
        public const ushort G2C_ZBRegister = 30011;
        public const ushort C2G_ZBCreateRoom = 30012;
        public const ushort G2C_ZBCreateRoom = 30013;
        public const ushort C2G_ZBJoinRoom = 30014;
        public const ushort G2C_ZBJoinRoom = 30015;
        public const ushort C2G_ZBLeaveRoom = 30016;
        public const ushort G2C_ZBLeaveRoom = 30017;
        public const ushort C2G_ZBRoomList = 30018;
        public const ushort G2C_ZBRoomList = 30019;
        public const ushort C2G_ZBReady = 30020;
        public const ushort G2C_ZBReady = 30021;
        public const ushort C2G_ZBMatch = 30022;
        public const ushort G2C_ZBMatch = 30023;
        public const ushort G2C_ZBMatchFound = 30024;
        public const ushort G2C_ZBRoomUpdate = 30025;
        public const ushort G2C_ZBBattleStart = 30026;
        public const ushort C2G_ZBBattleInput = 30027;
        public const ushort G2C_ZBBattleSnapshot = 30028;
        public const ushort G2C_ZBBattleEvent = 30029;
        public const ushort G2C_ZBBattleEnd = 30030;
    }
}