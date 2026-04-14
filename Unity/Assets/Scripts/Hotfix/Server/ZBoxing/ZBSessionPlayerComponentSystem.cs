namespace ET.Server
{
    [EntitySystemOf(typeof(ZBSessionPlayerComponent))]
    [FriendOf(typeof(ZBSessionPlayerComponent))]
    [FriendOf(typeof(ZBAccountComponent))]
    public static partial class ZBSessionPlayerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ZBSessionPlayerComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this ZBSessionPlayerComponent self)
        {
            if (self.PlayerId == 0)
            {
                return;
            }

            Scene root = self.Root();
            if (root.IsDisposed)
            {
                return;
            }

            // 玩家下线，清除在线状态
            ZBAccountComponent accountComponent = root.GetComponent<ZBAccountComponent>();
            if (accountComponent != null)
            {
                accountComponent.SetOffline(self.PlayerId);
                Log.Info($"[ZBoxing] 玩家下线: {self.Nickname} (ID={self.PlayerId})");
            }
        }

        /// <summary>
        /// 设置Session关联的玩家信息
        /// </summary>
        public static void SetPlayer(this ZBSessionPlayerComponent self, long playerId, string username, string nickname)
        {
            self.PlayerId = playerId;
            self.Username = username;
            self.Nickname = nickname;
        }

        /// <summary>
        /// 获取玩家ID
        /// </summary>
        public static long GetPlayerId(this ZBSessionPlayerComponent self)
        {
            return self.PlayerId;
        }

        /// <summary>
        /// 获取昵称
        /// </summary>
        public static string GetNickname(this ZBSessionPlayerComponent self)
        {
            return self.Nickname;
        }
    }
}
