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

            long playerId = self.PlayerId;

            // 清除在线状态
            ZBAccountComponent accountComponent = root.GetComponent<ZBAccountComponent>();
            if (accountComponent != null)
            {
                accountComponent.SetOffline(playerId);
            }

            // 战斗断线处理（设Session=null，不立即结束战斗）
            ZBBattleComponent battleComponent = root.GetComponent<ZBBattleComponent>();
            if (battleComponent != null)
            {
                battleComponent.OnPlayerDisconnect(playerId);
            }

            // 房间断线处理（离开房间，可能解散）
            ZBRoomManagerComponent roomManager = root.GetComponent<ZBRoomManagerComponent>();
            if (roomManager != null)
            {
                roomManager.OnPlayerDisconnect(playerId, accountComponent);

                // 防御性清理：确保PlayerToRoomId无残留
                if (roomManager.PlayerToRoomId.ContainsKey(playerId))
                {
                    Log.Warning($"[ZBoxing] 断线后PlayerToRoomId残留，强制移除: PlayerId={playerId}");
                    roomManager.PlayerToRoomId.Remove(playerId);
                }
            }

            // 防御性清理：确保PlayerToBattleId无残留
            if (battleComponent != null && battleComponent.PlayerToBattleId.ContainsKey(playerId))
            {
                Log.Warning($"[ZBoxing] 断线后PlayerToBattleId残留，强制移除: PlayerId={playerId}");
                battleComponent.PlayerToBattleId.Remove(playerId);
            }

            // 匹配队列断线处理
            ZBMatchQueueComponent matchQueue = root.GetComponent<ZBMatchQueueComponent>();
            if (matchQueue != null)
            {
                matchQueue.OnPlayerDisconnect(playerId);
            }

            Log.Info($"[ZBoxing] 玩家下线: {self.Nickname} (ID={playerId})");
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
