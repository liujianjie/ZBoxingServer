using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(ZBRoomManagerComponent))]
    [FriendOf(typeof(ZBRoomManagerComponent))]
    [FriendOf(typeof(ZBRoomComponent))]
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

            // 移除玩家映射
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
                var guestAccount = accountComponent.GetAccountByPlayerId(room.Guest.PlayerId);
                if (guestAccount != null)
                {
                    info.Guest = accountComponent.ToPlayerBrief(guestAccount);
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
