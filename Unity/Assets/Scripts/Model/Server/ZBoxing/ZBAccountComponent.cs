using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 账户信息（内存存储，Demo用）
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
    /// ZBoxing账户管理组件，挂载在Gate Scene上
    /// 管理所有玩家账户信息和在线状态
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ZBAccountComponent : Entity, IAwake
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
    }
}
