namespace ET.Server
{
    /// <summary>
    /// 房间状态枚举
    /// </summary>
    public static class ZBRoomState
    {
        public const int Waiting = 0;   // 等待中（有空位）
        public const int Full = 1;      // 已满（2人，但未全准备）
        public const int Fighting = 2;  // 对战中
    }

    /// <summary>
    /// 房间内玩家信息
    /// </summary>
    [EnableClass]
    public class ZBRoomPlayer
    {
        public long PlayerId;
        public string Nickname;
        public Session Session; // 玩家Session引用（推送消息用）
        public bool IsReady;
    }

    /// <summary>
    /// ZBoxing房间组件，作为ZBRoomManagerComponent的子Entity
    /// 代表一个独立的游戏房间，容纳两名玩家（Host + Guest）
    /// </summary>
    [ChildOf(typeof(ZBRoomManagerComponent))]
    public class ZBRoomComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 房间ID（自增分配）
        /// </summary>
        public int RoomId;

        /// <summary>
        /// 房间名称
        /// </summary>
        public string RoomName;

        /// <summary>
        /// 房主信息
        /// </summary>
        public ZBRoomPlayer Host;

        /// <summary>
        /// 客人信息（null表示空位）
        /// </summary>
        public ZBRoomPlayer Guest;

        /// <summary>
        /// 房间状态：0=等待, 1=已满, 2=对战中
        /// </summary>
        public int State;

        /// <summary>
        /// 房间创建时间（服务端毫秒时间戳，用于等待超时Bot注入）
        /// </summary>
        public long CreateTime;
    }
}
