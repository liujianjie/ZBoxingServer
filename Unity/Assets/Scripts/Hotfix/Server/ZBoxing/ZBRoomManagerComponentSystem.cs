using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(ZBRoomManagerComponent))]
    [FriendOf(typeof(ZBRoomManagerComponent))]
    [FriendOf(typeof(ZBRoomComponent))]
    [FriendOf(typeof(ZBBattleRoom))]
    public static partial class ZBRoomManagerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ZBRoomManagerComponent self)
        {
            self.RoomIdToInstanceId.Clear();
            self.PlayerToRoomId.Clear();
            self.NextRoomId = 1;
        }

        [EntitySystem]
        private static void Destroy(this ZBRoomManagerComponent self)
        {
            self.RoomIdToInstanceId.Clear();
            self.PlayerToRoomId.Clear();
            Log.Info("[ZBoxing] 房间管理器销毁");
        }

        // 等待超时阈值（毫秒）—— 创建房间后30秒无人加入则注入Bot
        private const long RoomWaitTimeoutMs = 30000;
        private const long BotPlayerId = -1;
        private const string BotNickname = "AI Bot";

        /// <summary>
        /// 每帧检查等待中的房间，超时30秒无人加入则自动注入Bot
        /// </summary>
        [EntitySystem]
        private static void Update(this ZBRoomManagerComponent self)
        {
            long now = TimeInfo.Instance.ServerNow();
            long clientNow = TimeInfo.Instance.ClientNow();

            // 收集需要注入Bot的房间（避免遍历中修改集合）
            List<ZBRoomComponent> timeoutRooms = null;
            foreach (var kv in self.RoomIdToInstanceId)
            {
                var room = self.GetChild<ZBRoomComponent>(kv.Value);
                if (room == null) continue;
                if (room.State != ZBRoomState.Waiting) continue;
                if (room.Guest != null) continue;
                if (room.CreateTime <= 0) continue;

                // 刷新等待中房主的Session活跃时间，防止等待Bot时被空闲超时踢掉
                if (room.Host?.Session != null && !room.Host.Session.IsDisposed)
                {
                    room.Host.Session.LastRecvTime = clientNow;
                }

                if (now - room.CreateTime >= RoomWaitTimeoutMs)
                {
                    timeoutRooms ??= new List<ZBRoomComponent>();
                    timeoutRooms.Add(room);
                }
            }

            if (timeoutRooms == null) return;

            Scene root = self.Root();
            ZBAccountComponent accountComponent = root.GetComponent<ZBAccountComponent>();

            foreach (var room in timeoutRooms)
            {
                Log.Info($"[ZBoxing] 房间等待超时，注入Bot: RoomId={room.RoomId}, Host={room.Host?.Nickname}");

                // 注入Bot为Guest
                room.Guest = new ZBRoomPlayer
                {
                    PlayerId = BotPlayerId,
                    Nickname = BotNickname,
                    Session = null,
                    IsReady = true,
                };
                room.State = ZBRoomState.Full;

                // 房主也标记为准备
                if (room.Host != null)
                {
                    room.Host.IsReady = true;
                }

                // 推送房间更新（Bot加入通知）
                if (accountComponent != null)
                {
                    self.BroadcastRoomUpdate(room, accountComponent);
                }

                // 尝试开战
                bool started = self.TryStartBattle(room);
                if (!started)
                {
                    self.DissolveRoom(room);
                    Log.Warning($"[ZBoxing] Bot房间开战失败，解散: RoomId={room.RoomId}");
                }
            }
        }

        /// <summary>
        /// 创建房间
        /// </summary>
        public static int CreateRoom(this ZBRoomManagerComponent self, string roomName, long playerId, string nickname, Session session, out ZBRoomComponent room)
        {
            room = null;

            // 玩家已在房间中
            if (self.PlayerToRoomId.ContainsKey(playerId))
            {
                return ZBErrorCode.AlreadyInRoom;
            }

            // 创建房间Entity
            room = self.AddChild<ZBRoomComponent>();
            room.RoomId = self.NextRoomId++;
            room.RoomName = string.IsNullOrEmpty(roomName) ? $"房间{room.RoomId}" : roomName;
            room.State = ZBRoomState.Waiting;
            room.CreateTime = TimeInfo.Instance.ServerNow();

            // 创建者成为房主
            room.Host = new ZBRoomPlayer
            {
                PlayerId = playerId,
                Nickname = nickname,
                Session = session,
                IsReady = false,
            };

            // 注册索引
            self.RoomIdToInstanceId[room.RoomId] = room.InstanceId;
            self.PlayerToRoomId[playerId] = room.RoomId;

            Log.Info($"[ZBoxing] 创建房间: RoomId={room.RoomId}, 房主={nickname}(ID={playerId})");

            return ZBErrorCode.Success;
        }

        /// <summary>
        /// 加入房间
        /// </summary>
        public static int JoinRoom(this ZBRoomManagerComponent self, int roomId, long playerId, string nickname, Session session, out ZBRoomComponent room)
        {
            room = null;

            // 玩家已在房间中
            if (self.PlayerToRoomId.ContainsKey(playerId))
            {
                return ZBErrorCode.AlreadyInRoom;
            }

            // 房间不存在
            if (!self.RoomIdToInstanceId.TryGetValue(roomId, out long instanceId))
            {
                return ZBErrorCode.RoomNotFound;
            }

            room = self.GetChild<ZBRoomComponent>(instanceId);
            if (room == null)
            {
                self.RoomIdToInstanceId.Remove(roomId);
                return ZBErrorCode.RoomNotFound;
            }

            // 房间已满或不在等待状态
            if (room.Guest != null || room.State != ZBRoomState.Waiting)
            {
                return ZBErrorCode.RoomFull;
            }

            // 加入为客人
            room.Guest = new ZBRoomPlayer
            {
                PlayerId = playerId,
                Nickname = nickname,
                Session = session,
                IsReady = false,
            };
            room.State = ZBRoomState.Full;

            self.PlayerToRoomId[playerId] = roomId;

            Log.Info($"[ZBoxing] 加入房间: RoomId={roomId}, 玩家={nickname}(ID={playerId})");

            return ZBErrorCode.Success;
        }

        /// <summary>
        /// 离开房间
        /// </summary>
        public static int LeaveRoom(this ZBRoomManagerComponent self, long playerId, out ZBRoomComponent affectedRoom, out bool roomDissolved)
        {
            affectedRoom = null;
            roomDissolved = false;

            // 玩家不在任何房间
            if (!self.PlayerToRoomId.TryGetValue(playerId, out int roomId))
            {
                return ZBErrorCode.NotInRoom;
            }

            if (!self.RoomIdToInstanceId.TryGetValue(roomId, out long instanceId))
            {
                self.PlayerToRoomId.Remove(playerId);
                return ZBErrorCode.RoomNotFound;
            }

            var room = self.GetChild<ZBRoomComponent>(instanceId);
            if (room == null)
            {
                self.RoomIdToInstanceId.Remove(roomId);
                self.PlayerToRoomId.Remove(playerId);
                return ZBErrorCode.RoomNotFound;
            }

            // 移除玩家映射（对战中也允许离开，战斗Entity自包含不依赖房间）
            self.PlayerToRoomId.Remove(playerId);

            if (room.Host != null && room.Host.PlayerId == playerId)
            {
                // 房主离开
                if (room.Guest != null)
                {
                    // 客人提升为房主
                    room.Host = room.Guest;
                    room.Guest = null;
                    room.Host.IsReady = false;
                    room.State = ZBRoomState.Waiting;
                    affectedRoom = room;

                    Log.Info($"[ZBoxing] 房主离开，客人晋升房主: RoomId={roomId}, 新房主={room.Host.Nickname}");
                }
                else
                {
                    // 房间无人，解散
                    self.DissolveRoom(room);
                    roomDissolved = true;

                    Log.Info($"[ZBoxing] 房主离开，房间解散: RoomId={roomId}");
                }
            }
            else if (room.Guest != null && room.Guest.PlayerId == playerId)
            {
                // 客人离开
                room.Guest = null;
                room.State = ZBRoomState.Waiting;
                affectedRoom = room;

                Log.Info($"[ZBoxing] 客人离开房间: RoomId={roomId}, PlayerId={playerId}");
            }

            return ZBErrorCode.Success;
        }

        /// <summary>
        /// 获取房间列表（等待中 + 已满，不含对战中）
        /// </summary>
        public static List<ZBRoomComponent> GetRoomList(this ZBRoomManagerComponent self)
        {
            var result = new List<ZBRoomComponent>();

            foreach (var kv in self.RoomIdToInstanceId)
            {
                var room = self.GetChild<ZBRoomComponent>(kv.Value);
                if (room != null && room.State != ZBRoomState.Fighting)
                {
                    result.Add(room);
                }
            }

            return result;
        }

        /// <summary>
        /// 通过玩家ID获取其所在房间
        /// </summary>
        public static ZBRoomComponent GetRoomByPlayerId(this ZBRoomManagerComponent self, long playerId)
        {
            if (!self.PlayerToRoomId.TryGetValue(playerId, out int roomId))
            {
                return null;
            }

            if (!self.RoomIdToInstanceId.TryGetValue(roomId, out long instanceId))
            {
                return null;
            }

            return self.GetChild<ZBRoomComponent>(instanceId);
        }

        /// <summary>
        /// 通过房间ID获取房间
        /// </summary>
        public static ZBRoomComponent GetRoomById(this ZBRoomManagerComponent self, int roomId)
        {
            if (!self.RoomIdToInstanceId.TryGetValue(roomId, out long instanceId))
            {
                return null;
            }

            return self.GetChild<ZBRoomComponent>(instanceId);
        }

        /// <summary>
        /// 解散房间
        /// </summary>
        public static void DissolveRoom(this ZBRoomManagerComponent self, ZBRoomComponent room)
        {
            if (room.Host != null)
            {
                self.PlayerToRoomId.Remove(room.Host.PlayerId);
            }
            if (room.Guest != null)
            {
                self.PlayerToRoomId.Remove(room.Guest.PlayerId);
            }

            self.RoomIdToInstanceId.Remove(room.RoomId);
            room.Dispose();
        }

        /// <summary>
        /// 将ZBRoomComponent转换为协议消息ZBRoomInfo
        /// </summary>
        public static ZBRoomInfo ToRoomInfo(this ZBRoomManagerComponent self, ZBRoomComponent room, ZBAccountComponent accountComponent)
        {
            var info = ZBRoomInfo.Create();
            info.RoomId = room.RoomId;
            info.RoomName = room.RoomName;
            info.State = room.State;
            info.HostReady = room.Host?.IsReady ?? false;
            info.GuestReady = room.Guest?.IsReady ?? false;

            if (room.Host != null)
            {
                var hostAccount = accountComponent.GetAccountByPlayerId(room.Host.PlayerId);
                if (hostAccount != null)
                {
                    info.Host = accountComponent.ToPlayerBrief(hostAccount);
                }
            }

            if (room.Guest != null)
            {
                if (room.Guest.PlayerId == BotPlayerId)
                {
                    // Bot玩家没有账号记录，手动构造简介
                    var botBrief = ZBPlayerBrief.Create();
                    botBrief.PlayerId = BotPlayerId;
                    botBrief.Nickname = BotNickname;
                    info.Guest = botBrief;
                }
                else
                {
                    var guestAccount = accountComponent.GetAccountByPlayerId(room.Guest.PlayerId);
                    if (guestAccount != null)
                    {
                        info.Guest = accountComponent.ToPlayerBrief(guestAccount);
                    }
                }
            }

            return info;
        }

        /// <summary>
        /// 向房间内所有玩家推送房间状态更新
        /// </summary>
        public static void BroadcastRoomUpdate(this ZBRoomManagerComponent self, ZBRoomComponent room, ZBAccountComponent accountComponent)
        {
            var roomInfo = self.ToRoomInfo(room, accountComponent);

            if (room.Host != null)
            {
                SendToPlayer(room.Host.Session, roomInfo);
            }

            if (room.Guest != null)
            {
                SendToPlayer(room.Guest.Session, roomInfo);
            }
        }

        /// <summary>
        /// 向指定Session推送房间更新
        /// </summary>
        private static void SendToPlayer(Session session, ZBRoomInfo roomInfo)
        {
            if (session == null || session.IsDisposed)
            {
                return;
            }

            var notify = G2C_ZBRoomUpdate.Create();
            notify.Room = roomInfo;
            session.Send(notify);
        }

        /// <summary>
        /// 设置玩家准备状态
        /// </summary>
        public static int SetReady(this ZBRoomManagerComponent self, long playerId, bool ready, out ZBRoomComponent room)
        {
            room = self.GetRoomByPlayerId(playerId);
            if (room == null)
            {
                return ZBErrorCode.NotInRoom;
            }

            // 对战中不允许改变准备状态
            if (room.State == ZBRoomState.Fighting)
            {
                return ZBErrorCode.NotInRoom;
            }

            if (room.Host != null && room.Host.PlayerId == playerId)
            {
                room.Host.IsReady = ready;
            }
            else if (room.Guest != null && room.Guest.PlayerId == playerId)
            {
                room.Guest.IsReady = ready;
            }
            else
            {
                return ZBErrorCode.NotInRoom;
            }

            Log.Info($"[ZBoxing] 玩家准备状态: PlayerId={playerId}, Ready={ready}, RoomId={room.RoomId}");

            return ZBErrorCode.Success;
        }

        /// <summary>
        /// 检查房间是否满足开战条件（双方都在且都已准备），满足则创建战斗Entity并通知双方
        /// </summary>
        public static bool TryStartBattle(this ZBRoomManagerComponent self, ZBRoomComponent room)
        {
            // 必须两人都在房间且都已准备
            if (room.Host == null || room.Guest == null)
            {
                return false;
            }

            if (!room.Host.IsReady || !room.Guest.IsReady)
            {
                return false;
            }

            // 切换房间状态为对战中
            room.State = ZBRoomState.Fighting;

            // 获取或创建战斗管理器
            Scene root = self.Root();
            ZBBattleComponent battleComponent = root.GetComponent<ZBBattleComponent>();
            if (battleComponent == null)
            {
                battleComponent = root.AddComponent<ZBBattleComponent>();
            }

            // 创建战斗Entity
            ZBBattleRoom battle = battleComponent.CreateBattle(room.RoomId, room.Host, room.Guest);
            if (battle == null)
            {
                Log.Error($"[ZBoxing] 创建战斗失败: RoomId={room.RoomId}");
                room.State = ZBRoomState.Full; // 回滚房间状态
                return false;
            }

            // 向双方推送开战通知（含玩家ID）
            var battleStart = G2C_ZBBattleStart.Create();
            battleStart.RoomId = room.RoomId;
            battleStart.Countdown = ZBBattleConst.CountdownSec;
            battleStart.Player1Id = room.Host.PlayerId;
            battleStart.Player2Id = room.Guest.PlayerId;

            if (room.Host.Session != null && !room.Host.Session.IsDisposed)
            {
                room.Host.Session.Send(battleStart);
            }

            if (room.Guest.Session != null && !room.Guest.Session.IsDisposed)
            {
                room.Guest.Session.Send(battleStart);
            }

            Log.Info($"[ZBoxing] 对战开始: RoomId={room.RoomId}, BattleId={battle.BattleId}, " +
                     $"Host={room.Host.Nickname} vs Guest={room.Guest.Nickname}");

            return true;
        }

        /// <summary>
        /// 玩家断线时清理房间
        /// </summary>
        public static void OnPlayerDisconnect(this ZBRoomManagerComponent self, long playerId, ZBAccountComponent accountComponent)
        {
            if (!self.PlayerToRoomId.ContainsKey(playerId))
            {
                return;
            }

            self.LeaveRoom(playerId, out ZBRoomComponent affectedRoom, out bool roomDissolved);

            if (!roomDissolved && affectedRoom != null)
            {
                self.BroadcastRoomUpdate(affectedRoom, accountComponent);
            }
        }

    }
}
