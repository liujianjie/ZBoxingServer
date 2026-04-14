namespace ET.Server
{
    [EntitySystemOf(typeof(ZBAccountComponent))]
    [FriendOf(typeof(ZBAccountComponent))]
    public static partial class ZBAccountComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ZBAccountComponent self)
        {
            self.Accounts.Clear();
            self.PlayerIdToUsername.Clear();
            self.OnlineSessions.Clear();
            self.NextPlayerId = 10001;
            self.IsDirty = false;

            // 从文件加载已有账户数据
            self.LoadAccounts();
        }

        [EntitySystem]
        private static void Destroy(this ZBAccountComponent self)
        {
            // 关闭时保存未持久化的数据
            self.IsDirty = true; // 强制保存
            self.SaveAccounts();
            Log.Info("[ZBoxing] 账户组件销毁，数据已保存");
        }

        /// <summary>
        /// 注册新账户
        /// </summary>
        /// <returns>错误码，0=成功</returns>
        public static int Register(this ZBAccountComponent self, string username, string password, string nickname, out ZBAccountInfo account)
        {
            account = null;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return ZBErrorCode.EmptyField;
            }

            if (self.Accounts.ContainsKey(username))
            {
                return ZBErrorCode.UsernameTaken;
            }

            if (!string.IsNullOrEmpty(nickname) && nickname.Length > 20)
            {
                return ZBErrorCode.NicknameTooLong;
            }

            // 昵称默认与用户名相同
            string nick = string.IsNullOrEmpty(nickname) ? username : nickname;

            account = new ZBAccountInfo
            {
                Username = username,
                Password = password,
                PlayerId = self.NextPlayerId++,
                Nickname = nick,
                Gold = 1000, // 初始金币
                WinCount = 0,
                LoseCount = 0,
            };

            self.Accounts[username] = account;
            self.PlayerIdToUsername[account.PlayerId] = username;

            // 注册后自动持久化
            self.MarkDirty();
            self.SaveAccounts();

            return ZBErrorCode.Success;
        }

        /// <summary>
        /// 登录验证
        /// </summary>
        /// <returns>错误码，0=成功</returns>
        public static int Login(this ZBAccountComponent self, string username, string password, out ZBAccountInfo account)
        {
            account = null;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return ZBErrorCode.EmptyField;
            }

            if (!self.Accounts.TryGetValue(username, out account))
            {
                return ZBErrorCode.AccountNotFound;
            }

            if (account.Password != password)
            {
                account = null;
                return ZBErrorCode.WrongPassword;
            }

            return ZBErrorCode.Success;
        }

        /// <summary>
        /// 标记玩家上线
        /// </summary>
        public static void SetOnline(this ZBAccountComponent self, long playerId, long sessionInstanceId)
        {
            self.OnlineSessions[playerId] = sessionInstanceId;
        }

        /// <summary>
        /// 标记玩家下线
        /// </summary>
        public static void SetOffline(this ZBAccountComponent self, long playerId)
        {
            self.OnlineSessions.Remove(playerId);
        }

        /// <summary>
        /// 检查玩家是否在线
        /// </summary>
        public static bool IsOnline(this ZBAccountComponent self, long playerId)
        {
            return self.OnlineSessions.ContainsKey(playerId);
        }

        /// <summary>
        /// 获取玩家在线Session InstanceId
        /// </summary>
        public static long GetOnlineSessionId(this ZBAccountComponent self, long playerId)
        {
            self.OnlineSessions.TryGetValue(playerId, out long sessionId);
            return sessionId;
        }

        /// <summary>
        /// 从AccountInfo创建ZBPlayerBrief消息
        /// </summary>
        public static ZBPlayerBrief ToPlayerBrief(this ZBAccountComponent self, ZBAccountInfo info)
        {
            var brief = ZBPlayerBrief.Create();
            brief.PlayerId = info.PlayerId;
            brief.Nickname = info.Nickname;
            brief.Gold = info.Gold;
            brief.WinCount = info.WinCount;
            brief.LoseCount = info.LoseCount;
            return brief;
        }

        /// <summary>
        /// 更新玩家战绩（战斗结算后调用）
        /// </summary>
        public static void UpdatePlayerStats(this ZBAccountComponent self, long playerId, bool isWin, int goldDelta)
        {
            if (!self.PlayerIdToUsername.TryGetValue(playerId, out string username))
            {
                return;
            }

            if (!self.Accounts.TryGetValue(username, out ZBAccountInfo account))
            {
                return;
            }

            if (isWin)
            {
                account.WinCount++;
            }
            else
            {
                account.LoseCount++;
            }

            account.Gold += goldDelta;
            if (account.Gold < 0) account.Gold = 0;

            // 战绩变更后持久化
            self.MarkDirty();
            self.SaveAccounts();
        }

        /// <summary>
        /// 根据玩家ID获取账户信息
        /// </summary>
        public static ZBAccountInfo GetAccountByPlayerId(this ZBAccountComponent self, long playerId)
        {
            if (!self.PlayerIdToUsername.TryGetValue(playerId, out string username))
            {
                return null;
            }

            self.Accounts.TryGetValue(username, out ZBAccountInfo account);
            return account;
        }
    }
}
