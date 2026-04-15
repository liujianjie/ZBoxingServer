using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 账户信息
    /// </summary>
    [EnableClass]
    public class ZBAccountInfo
    {
        public string Username;
        public string Password;
        public long PlayerId;
        public string Nickname;
        public int Gold;
        public int WinCount;
        public int LoseCount;
    }

    /// <summary>
    /// 持久化存档数据结构（JSON序列化用）
    /// </summary>
    [EnableClass]
    public class ZBAccountSaveData
    {
        public long NextPlayerId { get; set; } = 10001;
        public List<ZBAccountInfo> AccountList { get; set; } = new();
    }

    /// <summary>
    /// ZBoxing账户管理组件，挂载在Gate Scene上
    /// 管理所有玩家账户信息和在线状态
    /// 支持JSON文件持久化，服务端重启不丢数据
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ZBAccountComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 用户名 → 账户信息
        /// </summary>
        public Dictionary<string, ZBAccountInfo> Accounts = new();

        /// <summary>
        /// 玩家ID → 用户名（反查）
        /// </summary>
        public Dictionary<long, string> PlayerIdToUsername = new();

        /// <summary>
        /// 玩家ID → 登录Session的InstanceId（在线追踪）
        /// </summary>
        public Dictionary<long, long> OnlineSessions = new();

        /// <summary>
        /// 下一个可分配的玩家ID
        /// </summary>
        public long NextPlayerId = 10001;

        /// <summary>
        /// 存档文件路径
        /// </summary>
        public string SaveFilePath;

        /// <summary>
        /// 是否有未保存的变更
        /// </summary>
        public bool IsDirty;
    }

    /// <summary>
    /// ZBoxing错误码定义
    /// </summary>
    public static class ZBErrorCode
    {
        public const int Success = 0;
        public const int EmptyField = 1;         // 用户名或密码为空
        public const int AccountNotFound = 2;     // 账号不存在
        public const int WrongPassword = 3;       // 密码错误
        public const int AlreadyLoggedIn = 4;     // 已在其他地方登录
        public const int UsernameTaken = 5;       // 用户名已被占用
        public const int NicknameTooLong = 6;     // 昵称过长

        // 房间系统错误码 (10~19)
        public const int NotLoggedIn = 10;        // 未登录
        public const int AlreadyInRoom = 11;      // 已在房间中
        public const int RoomNotFound = 12;       // 房间不存在
        public const int RoomFull = 13;           // 房间已满
        public const int NotInRoom = 14;          // 不在任何房间
        public const int NotReady = 15;           // 未准备
        public const int NotRoomHost = 16;        // 不是房主

        // 战斗系统错误码 (20~29)
        public const int NotInBattle = 20;        // 不在战斗中
        public const int AlreadyInBattle = 21;    // 已在战斗中
        public const int BattleNotFound = 22;     // 战斗不存在
        public const int BattleCreateFailed = 23; // 战斗创建失败

        // 服务端内部错误 (99)
        public const int ServerError = 99;        // 服务端内部异常
    }
}
