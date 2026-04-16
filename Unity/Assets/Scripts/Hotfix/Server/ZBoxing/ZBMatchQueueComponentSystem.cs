namespace ET.Server
{
    [EntitySystemOf(typeof(ZBMatchQueueComponent))]
    [FriendOf(typeof(ZBMatchQueueComponent))]
    [FriendOf(typeof(ZBRoomComponent))]
    [FriendOf(typeof(ZBRoomManagerComponent))]
    public static partial class ZBMatchQueueComponentSystem
    {
        // 匹配超时阈值（毫秒）
        private const long MatchTimeoutMs = 30000;

        // Bot虚拟PlayerId
        private const long BotPlayerId = -1;

        // Bot昵称
        private const string BotNickname = "AI Bot";

        [EntitySystem]
        private static void Awake(this ZBMatchQueueComponent self)
        {
            self.Queue.Clear();
            self.PlayerMap.Clear();
        }

        [EntitySystem]
        private static void Destroy(this ZBMatchQueueComponent self)
        {
            self.Queue.Clear();
            self.PlayerMap.Clear();
            Log.Info("[ZBoxing] 匹配队列销毁");
        }

        /// <summary>
        /// Update：每帧检查队列首个玩家是否等待超时，超时则创建Bot对战
        /// </summary>
        [EntitySystem]
        private static void Update(this ZBMatchQueueComponent self)
        {
            // 队列为空则无需检查
            if (self.Queue.Count == 0)
            {
                return;
            }

            // 清理断线玩家后再检查
            self.CleanDisconnected();

            if (self.Queue.Count == 0)
            {
                return;
            }

            ZBMatchPlayer first = self.Queue[0];
            long now = TimeInfo.Instance.ServerNow();

            // 检查等待时长是否超过30秒
            if (now - first.EnqueueTime >= MatchTimeoutMs)
            {
                Log.Info($"[ZBoxing] 匹配超时，为 {first.Nickname}(ID={first.PlayerId}) 创建Bot对战");
                self.CreateMatchWithBot(first);
            }
        }

        /// <summary>
        /// 匹配超时 — 为指定玩家创建Bot对战
        /// </summary>
        private static void CreateMatchWithBot(this ZBMatchQueueComponent self, ZBMatchPlayer player)
        {
            // 从队列移除该玩家
            self.Queue.Remove(player);
            self.PlayerMap.Remove(player.PlayerId);

            Scene root = self.Root();

            // 获取或创建房间管理器
            ZBRoomManagerComponent roomManager = root.GetComponent<ZBRoomManagerComponent>();
            if (roomManager == null)
            {
                roomManager = root.AddComponent<ZBRoomManagerComponent>();
            }

            // 玩家作为房主创建房间
            int err = roomManager.CreateRoom(
                "Bot对战房间",
                player.PlayerId,
                player.Nickname,
                player.Session,
                out ZBRoomComponent room
            );

            if (err != ZBErrorCode.Success || room == null)
            {
                Log.Warning($"[ZBoxing] Bot对战：创建房间失败 err={err}, PlayerId={player.PlayerId}");
                return;
            }

            // 直接内联方式将Bot加入为Guest，绕过JoinRoom对PlayerId的唯一性检查
            room.Guest = new ZBRoomPlayer
            {
                PlayerId = BotPlayerId,
                Nickname = BotNickname,
                Session = null,   // Bot无Session
                IsReady = true,   // Bot直接准备
            };
            room.State = ZBRoomState.Full;

            // 玩家也标记为准备
            if (room.Host != null)
            {
                room.Host.IsReady = true;
            }

            Log.Info($"[ZBoxing] Bot已加入房间: RoomId={room.RoomId}, Bot={BotNickname}");

            // 双方都已准备，尝试开战
            bool started = roomManager.TryStartBattle(room);
            if (!started)
            {
                // 开战失败则解散房间，避免状态遗留
                roomManager.DissolveRoom(room);
                Log.Warning($"[ZBoxing] Bot对战：启动战斗失败, RoomId={room.RoomId}");
            }
        }

        /// <summary>
        /// 加入匹配队列
        /// </summary>
        public static int Enqueue(this ZBMatchQueueComponent self, long playerId, string nickname, Session session)
        {
            // 已在队列中
            if (self.PlayerMap.ContainsKey(playerId))
            {
                return ZBErrorCode.AlreadyInRoom;
            }

            // 检查玩家是否已在房间中（不能同时在房间和匹配队列）
            ZBRoomManagerComponent roomManager = self.Root().GetComponent<ZBRoomManagerComponent>();
            if (roomManager != null)
            {
                var existingRoom = roomManager.GetRoomByPlayerId(playerId);
                if (existingRoom != null)
                {
                    return ZBErrorCode.AlreadyInRoom;
                }
            }

            var matchPlayer = new ZBMatchPlayer
            {
                PlayerId = playerId,
                Nickname = nickname,
                Session = session,
                EnqueueTime = TimeInfo.Instance.ServerNow(),
            };

            self.Queue.Add(matchPlayer);
            self.PlayerMap[playerId] = matchPlayer;

            Log.Info($"[ZBoxing] 加入匹配队列: PlayerId={playerId}, 队列人数={self.Queue.Count}");

            return ZBErrorCode.Success;
        }

        /// <summary>
        /// 取消匹配（离开队列）
        /// </summary>
        public static int Dequeue(this ZBMatchQueueComponent self, long playerId)
        {
            if (!self.PlayerMap.TryGetValue(playerId, out ZBMatchPlayer matchPlayer))
            {
                return ZBErrorCode.NotInRoom; // 不在队列中
            }

            self.Queue.Remove(matchPlayer);
            self.PlayerMap.Remove(playerId);

            Log.Info($"[ZBoxing] 取消匹配: PlayerId={playerId}, 剩余队列人数={self.Queue.Count}");

            return ZBErrorCode.Success;
        }

        /// <summary>
        /// 检查是否在队列中
        /// </summary>
        public static bool IsInQueue(this ZBMatchQueueComponent self, long playerId)
        {
            return self.PlayerMap.ContainsKey(playerId);
        }

        /// <summary>
        /// 尝试撮合匹配（队列中取前2人创建房间）
        /// 返回是否成功撮合
        /// </summary>
        public static bool TryMatch(this ZBMatchQueueComponent self)
        {
            // 清理已断线的玩家
            self.CleanDisconnected();

            if (self.Queue.Count < 2)
            {
                return false;
            }

            // 取出前两名玩家
            ZBMatchPlayer player1 = self.Queue[0];
            ZBMatchPlayer player2 = self.Queue[1];

            self.Queue.RemoveRange(0, 2);
            self.PlayerMap.Remove(player1.PlayerId);
            self.PlayerMap.Remove(player2.PlayerId);

            // 通过房间管理器创建房间
            Scene root = self.Root();
            ZBRoomManagerComponent roomManager = root.GetComponent<ZBRoomManagerComponent>();
            if (roomManager == null)
            {
                roomManager = root.AddComponent<ZBRoomManagerComponent>();
            }

            ZBAccountComponent accountComponent = root.GetComponent<ZBAccountComponent>();

            // Player1作为房主创建房间
            int err = roomManager.CreateRoom(
                $"匹配房间",
                player1.PlayerId,
                player1.Nickname,
                player1.Session,
                out ZBRoomComponent room
            );

            if (err != ZBErrorCode.Success || room == null)
            {
                Log.Warning($"[ZBoxing] 匹配创建房间失败: err={err}");
                return false;
            }

            // Player2加入房间
            err = roomManager.JoinRoom(
                room.RoomId,
                player2.PlayerId,
                player2.Nickname,
                player2.Session,
                out ZBRoomComponent joinedRoom
            );

            if (err != ZBErrorCode.Success)
            {
                // 加入失败，回滚：解散房间
                roomManager.DissolveRoom(room);
                Log.Warning($"[ZBoxing] 匹配加入房间失败: err={err}");
                return false;
            }

            // 匹配成功，向双方推送G2C_ZBMatchFound
            ZBRoomInfo roomInfo = accountComponent != null
                ? roomManager.ToRoomInfo(room, accountComponent)
                : null;

            if (roomInfo != null)
            {
                NotifyMatchFound(player1.Session, roomInfo);
                NotifyMatchFound(player2.Session, roomInfo);
            }

            Log.Info($"[ZBoxing] 匹配成功: {player1.Nickname}(ID={player1.PlayerId}) vs {player2.Nickname}(ID={player2.PlayerId}), RoomId={room.RoomId}");

            return true;
        }

        /// <summary>
        /// 清理已断线的玩家
        /// </summary>
        private static void CleanDisconnected(this ZBMatchQueueComponent self)
        {
            for (int i = self.Queue.Count - 1; i >= 0; i--)
            {
                var player = self.Queue[i];
                if (player.Session == null || player.Session.IsDisposed)
                {
                    self.Queue.RemoveAt(i);
                    self.PlayerMap.Remove(player.PlayerId);
                    Log.Info($"[ZBoxing] 清理断线匹配玩家: PlayerId={player.PlayerId}");
                }
            }
        }

        /// <summary>
        /// 向玩家推送匹配成功通知
        /// </summary>
        private static void NotifyMatchFound(Session session, ZBRoomInfo roomInfo)
        {
            if (session == null || session.IsDisposed)
            {
                return;
            }

            var notify = G2C_ZBMatchFound.Create();
            notify.Room = roomInfo;
            session.Send(notify);
        }

        /// <summary>
        /// 玩家断线时清理匹配队列
        /// </summary>
        public static void OnPlayerDisconnect(this ZBMatchQueueComponent self, long playerId)
        {
            self.Dequeue(playerId);
        }
    }
}
