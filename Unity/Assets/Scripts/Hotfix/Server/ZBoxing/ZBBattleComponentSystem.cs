using System.Collections.Generic;

namespace ET.Server
{
    // ============================================================
    // ZBBattleComponent 系统（战斗管理器生命周期）
    // ============================================================
    [EntitySystemOf(typeof(ZBBattleComponent))]
    [FriendOf(typeof(ZBBattleComponent))]
    [FriendOf(typeof(ZBBattleRoom))]
    public static partial class ZBBattleComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ZBBattleComponent self)
        {
            self.BattleIdToInstanceId.Clear();
            self.PlayerToBattleId.Clear();
            self.NextBattleId = 1;
            Log.Info("[ZBoxing] 战斗管理器初始化完成");
        }

        [EntitySystem]
        private static void Destroy(this ZBBattleComponent self)
        {
            self.BattleIdToInstanceId.Clear();
            self.PlayerToBattleId.Clear();
            Log.Info("[ZBoxing] 战斗管理器已销毁");
        }

        /// <summary>
        /// 创建一场战斗
        /// </summary>
        /// <param name="roomId">关联的房间ID</param>
        /// <param name="host">房主信息</param>
        /// <param name="guest">客人信息</param>
        /// <returns>创建的ZBBattleRoom，失败返回null</returns>
        public static ZBBattleRoom CreateBattle(this ZBBattleComponent self, int roomId, ZBRoomPlayer host, ZBRoomPlayer guest)
        {
            if (host == null || guest == null)
            {
                Log.Error("[ZBoxing] 创建战斗失败: 玩家信息为空");
                return null;
            }

            // 检查玩家是否已在其他战斗中
            if (self.PlayerToBattleId.ContainsKey(host.PlayerId))
            {
                Log.Error($"[ZBoxing] 创建战斗失败: 玩家{host.Nickname}(ID={host.PlayerId})已在战斗中");
                return null;
            }

            if (self.PlayerToBattleId.ContainsKey(guest.PlayerId))
            {
                Log.Error($"[ZBoxing] 创建战斗失败: 玩家{guest.Nickname}(ID={guest.PlayerId})已在战斗中");
                return null;
            }

            // 创建战斗Entity
            ZBBattleRoom battleRoom = self.AddChild<ZBBattleRoom>();
            battleRoom.BattleId = self.NextBattleId++;
            battleRoom.RoomId = roomId;

            // 初始化Player1（房主方）
            battleRoom.Player1 = new ZBBattlePlayer
            {
                PlayerId = host.PlayerId,
                Nickname = host.Nickname,
                Session = host.Session,
                Hp = ZBBattleConst.InitHp,
                Stamina = ZBBattleConst.InitStamina,
                PosX = ZBBattleConst.Player1StartX,
                PosY = 0f,
                FacingRight = true,
                AnimState = ZBAnimState.Idle,
                AnimFrame = 0,
                ComboCount = 0,
                InputBuffer = new List<ZBInputFrame>(),
                RoomId = roomId,
            };

            // 初始化Player2（客人方）
            battleRoom.Player2 = new ZBBattlePlayer
            {
                PlayerId = guest.PlayerId,
                Nickname = guest.Nickname,
                Session = guest.Session,
                Hp = ZBBattleConst.InitHp,
                Stamina = ZBBattleConst.InitStamina,
                PosX = ZBBattleConst.Player2StartX,
                PosY = 0f,
                FacingRight = false,
                AnimState = ZBAnimState.Idle,
                AnimFrame = 0,
                ComboCount = 0,
                InputBuffer = new List<ZBInputFrame>(),
                RoomId = roomId,
            };

            // 初始化帧与时间
            battleRoom.CurrentFrame = 0;
            battleRoom.RemainingFrames = ZBBattleConst.RoundFrames;
            battleRoom.StartTimeMs = TimeInfo.Instance.ServerNow();

            // 初始化30Hz固定帧率控制器
            battleRoom.FixedTimeCounter = new FixedTimeCounter(
                TimeInfo.Instance.ServerFrameTime(), 0, ZBBattleConst.TickIntervalMs);

            // 初始化阶段（倒计时）
            battleRoom.Phase = ZBBattlePhase.Countdown;
            battleRoom.CountdownFrames = ZBBattleConst.CountdownFrames;

            // 注册映射
            self.BattleIdToInstanceId[battleRoom.BattleId] = battleRoom.InstanceId;
            self.PlayerToBattleId[host.PlayerId] = battleRoom.BattleId;
            self.PlayerToBattleId[guest.PlayerId] = battleRoom.BattleId;

            Log.Info($"[ZBoxing] 战斗创建成功: BattleId={battleRoom.BattleId}, RoomId={roomId}, " +
                     $"{host.Nickname} vs {guest.Nickname}");

            return battleRoom;
        }

        /// <summary>
        /// 结束并销毁一场战斗
        /// </summary>
        public static void DestroyBattle(this ZBBattleComponent self, long battleId)
        {
            if (!self.BattleIdToInstanceId.TryGetValue(battleId, out long instanceId))
            {
                Log.Warning($"[ZBoxing] 销毁战斗失败: BattleId={battleId} 不存在");
                return;
            }

            ZBBattleRoom battleRoom = self.GetChild<ZBBattleRoom>(instanceId);
            if (battleRoom == null)
            {
                self.BattleIdToInstanceId.Remove(battleId);
                return;
            }

            // 移除玩家映射
            if (battleRoom.Player1 != null)
            {
                self.PlayerToBattleId.Remove(battleRoom.Player1.PlayerId);
            }

            if (battleRoom.Player2 != null)
            {
                self.PlayerToBattleId.Remove(battleRoom.Player2.PlayerId);
            }

            // 移除战斗映射
            self.BattleIdToInstanceId.Remove(battleId);

            Log.Info($"[ZBoxing] 战斗销毁: BattleId={battleId}, RoomId={battleRoom.RoomId}");

            // 销毁Entity
            battleRoom.Dispose();
        }

        /// <summary>
        /// 通过战斗ID获取战斗Entity
        /// </summary>
        public static ZBBattleRoom GetBattle(this ZBBattleComponent self, long battleId)
        {
            if (!self.BattleIdToInstanceId.TryGetValue(battleId, out long instanceId))
            {
                return null;
            }

            return self.GetChild<ZBBattleRoom>(instanceId);
        }

        /// <summary>
        /// 通过玩家ID获取其所在战斗
        /// </summary>
        public static ZBBattleRoom GetBattleByPlayerId(this ZBBattleComponent self, long playerId)
        {
            if (!self.PlayerToBattleId.TryGetValue(playerId, out long battleId))
            {
                return null;
            }

            return self.GetBattle(battleId);
        }

        /// <summary>
        /// 玩家是否在战斗中
        /// </summary>
        public static bool IsPlayerInBattle(this ZBBattleComponent self, long playerId)
        {
            return self.PlayerToBattleId.ContainsKey(playerId);
        }

        /// <summary>
        /// 玩家断线处理 — 将Session设为null，不立即结束战斗
        /// 实际的断线判负逻辑由游戏循环(E.2)处理
        /// </summary>
        public static void OnPlayerDisconnect(this ZBBattleComponent self, long playerId)
        {
            ZBBattleRoom battle = self.GetBattleByPlayerId(playerId);
            if (battle == null)
            {
                return;
            }

            ZBBattlePlayer player = battle.GetPlayer(playerId);
            if (player != null)
            {
                player.Session = null;
                Log.Info($"[ZBoxing] 战斗中玩家断线: {player.Nickname}(ID={playerId}), BattleId={battle.BattleId}");
            }
        }
    }

    // ============================================================
    // ZBBattleRoom 系统（单场战斗生命周期 + 30Hz游戏循环）
    // ============================================================
    [EntitySystemOf(typeof(ZBBattleRoom))]
    [FriendOf(typeof(ZBBattleRoom))]
    public static partial class ZBBattleRoomSystem
    {
        [EntitySystem]
        private static void Awake(this ZBBattleRoom self)
        {
        }

        [EntitySystem]
        private static void Destroy(this ZBBattleRoom self)
        {
            self.FixedTimeCounter = null;
            self.Player1 = null;
            self.Player2 = null;
        }

        // ============================================================
        // 30Hz固定帧率游戏循环（ET IUpdate自动调用）
        // ============================================================
        [EntitySystem]
        private static void Update(this ZBBattleRoom self)
        {
            // 战斗已结束，不再Tick
            if (self.Phase == ZBBattlePhase.KO || self.Phase == ZBBattlePhase.TimeUp)
            {
                return;
            }

            // 30Hz节流：只有真实时间到达下一帧时才推进
            long timeNow = TimeInfo.Instance.ServerFrameTime();
            int nextFrame = self.CurrentFrame + 1;
            if (timeNow < self.FixedTimeCounter.FrameTime(nextFrame))
            {
                return;
            }

            // 推进帧号
            self.CurrentFrame = nextFrame;

            // 根据阶段分发逻辑
            switch (self.Phase)
            {
                case ZBBattlePhase.Countdown:
                    self.TickCountdown();
                    break;
                case ZBBattlePhase.Fighting:
                    self.TickFighting();
                    break;
            }

            // 每帧广播状态快照
            self.BroadcastSnapshot();
        }

        // ============================================================
        // 倒计时阶段Tick
        // ============================================================
        private static void TickCountdown(this ZBBattleRoom self)
        {
            self.CountdownFrames--;
            if (self.CountdownFrames <= 0)
            {
                self.Phase = ZBBattlePhase.Fighting;
                Log.Info($"[ZBoxing] 战斗开始! BattleId={self.BattleId}, Frame={self.CurrentFrame}");
            }
        }

        // ============================================================
        // 战斗阶段Tick
        // ============================================================
        private static void TickFighting(this ZBBattleRoom self)
        {
            // 1. 消费双方输入缓冲区（取最早的一个输入）
            ZBInputFrame input1 = self.ConsumeInput(self.Player1);
            ZBInputFrame input2 = self.ConsumeInput(self.Player2);

            // 2. 应用输入 → 更新玩家状态
            //    （E.4角色状态机/E.5招式系统实现后替换此处占位逻辑）
            self.ApplyInput(self.Player1, input1);
            self.ApplyInput(self.Player2, input2);

            // 3. 递减剩余时间
            self.RemainingFrames--;

            // 4. 检查胜负条件
            //    4a. KO判定（E.7伤害计算实现后生效）
            if (self.Player1.Hp <= 0 || self.Player2.Hp <= 0)
            {
                self.EndBattle(ZBBattlePhase.KO);
                return;
            }

            //    4b. 时间到
            if (self.RemainingFrames <= 0)
            {
                self.EndBattle(ZBBattlePhase.TimeUp);
                return;
            }
        }

        // ============================================================
        // 消费玩家输入缓冲区（取出最早的一个）
        // ============================================================
        private static ZBInputFrame ConsumeInput(this ZBBattleRoom self, ZBBattlePlayer player)
        {
            if (player == null || player.InputBuffer.Count == 0)
            {
                return null;
            }

            ZBInputFrame input = player.InputBuffer[0];
            player.InputBuffer.RemoveAt(0);
            return input;
        }

        // ============================================================
        // 应用输入到玩家状态（基础版：仅处理移动）
        // E.4/E.5会替换为完整的状态机+招式系统
        // ============================================================
        private static void ApplyInput(this ZBBattleRoom self, ZBBattlePlayer player, ZBInputFrame input)
        {
            if (player == null)
            {
                return;
            }

            if (input == null)
            {
                // 无输入 → 保持Idle（如果不在攻击/受击等特殊状态中）
                if (player.AnimState == ZBAnimState.MoveForward || player.AnimState == ZBAnimState.MoveBackward)
                {
                    player.AnimState = ZBAnimState.Idle;
                }
                return;
            }

            // 处理移动方向
            if (input.MoveDir != 0)
            {
                float moveSpeed = 3.0f / ZBBattleConst.TickRate; // 3单位/秒 → 每帧移动量
                player.PosX += input.MoveDir * moveSpeed;

                // 限制在场地范围内 [-5, 5]
                if (player.PosX < -5f) player.PosX = -5f;
                if (player.PosX > 5f) player.PosX = 5f;

                // 更新移动动画状态
                bool movingForward = (player.FacingRight && input.MoveDir > 0)
                                     || (!player.FacingRight && input.MoveDir < 0);
                player.AnimState = movingForward ? ZBAnimState.MoveForward : ZBAnimState.MoveBackward;
            }
            else
            {
                // 无移动输入 → Idle
                if (player.AnimState == ZBAnimState.MoveForward || player.AnimState == ZBAnimState.MoveBackward)
                {
                    player.AnimState = ZBAnimState.Idle;
                }
            }

            // 更新朝向（始终面向对手）
            ZBBattlePlayer opponent = self.GetOpponent(player.PlayerId);
            if (opponent != null)
            {
                player.FacingRight = player.PosX < opponent.PosX;
            }

            // 动作输入（E.4/E.5实现后处理，当前仅记录日志）
            // input.Action != ZBInputAction.None 时由状态机处理
        }

        // ============================================================
        // 战斗结束处理
        // ============================================================
        private static void EndBattle(this ZBBattleRoom self, int endPhase)
        {
            self.Phase = endPhase;

            // 判定胜者
            long winnerId = 0;
            int reason = endPhase == ZBBattlePhase.KO ? 1 : 2; // 1=KO, 2=时间到

            if (self.Player1.Hp <= 0 && self.Player2.Hp > 0)
            {
                winnerId = self.Player2.PlayerId;
            }
            else if (self.Player2.Hp <= 0 && self.Player1.Hp > 0)
            {
                winnerId = self.Player1.PlayerId;
            }
            else if (endPhase == ZBBattlePhase.TimeUp)
            {
                // 时间到按剩余HP比较
                if (self.Player1.Hp > self.Player2.Hp)
                {
                    winnerId = self.Player1.PlayerId;
                }
                else if (self.Player2.Hp > self.Player1.Hp)
                {
                    winnerId = self.Player2.PlayerId;
                }
                // 相等则winnerId=0（平局）
            }

            // 广播战斗结束消息
            G2C_ZBBattleEnd endMsg = G2C_ZBBattleEnd.Create();
            endMsg.WinnerId = winnerId;
            endMsg.Reason = reason;
            endMsg.GoldReward = winnerId != 0 ? 100 : 50; // 胜者100金币，平局50
            endMsg.Player1Hp = self.Player1.Hp;
            endMsg.Player2Hp = self.Player2.Hp;
            self.Broadcast(endMsg);

            string winnerName = winnerId == 0 ? "平局" :
                winnerId == self.Player1.PlayerId ? self.Player1.Nickname : self.Player2.Nickname;
            Log.Info($"[ZBoxing] 战斗结束! BattleId={self.BattleId}, " +
                     $"Winner={winnerName}, Reason={reason}, " +
                     $"HP: {self.Player1.Hp} vs {self.Player2.Hp}, " +
                     $"Frame={self.CurrentFrame}");

            // 延迟销毁战斗（给客户端时间处理结算）
            // E.8完善时可能改为定时器延迟销毁
        }

        // ============================================================
        // 广播状态快照（每Tick调用一次）
        // ============================================================
        private static void BroadcastSnapshot(this ZBBattleRoom self)
        {
            G2C_ZBBattleSnapshot snapshot = G2C_ZBBattleSnapshot.Create();
            snapshot.ServerFrame = self.CurrentFrame;
            snapshot.RemainingTime = self.RemainingFrames;
            snapshot.BattlePhase = self.Phase;

            // 填充Player1状态
            snapshot.Player1 = self.BuildPlayerState(self.Player1);
            // 填充Player2状态
            snapshot.Player2 = self.BuildPlayerState(self.Player2);

            self.Broadcast(snapshot);
        }

        /// <summary>
        /// 构建单个玩家的状态快照
        /// </summary>
        private static ZBPlayerState BuildPlayerState(this ZBBattleRoom self, ZBBattlePlayer player)
        {
            ZBPlayerState state = ZBPlayerState.Create();
            if (player == null)
            {
                return state;
            }

            state.PlayerId = player.PlayerId;
            state.PosX = player.PosX;
            state.PosY = player.PosY;
            state.Hp = player.Hp;
            state.Stamina = player.Stamina;
            state.AnimState = player.AnimState;
            state.AnimFrame = player.AnimFrame;
            state.FacingRight = player.FacingRight;
            state.ComboCount = player.ComboCount;
            return state;
        }

        /// <summary>
        /// 获取指定玩家ID对应的ZBBattlePlayer
        /// </summary>
        public static ZBBattlePlayer GetPlayer(this ZBBattleRoom self, long playerId)
        {
            if (self.Player1 != null && self.Player1.PlayerId == playerId)
            {
                return self.Player1;
            }

            if (self.Player2 != null && self.Player2.PlayerId == playerId)
            {
                return self.Player2;
            }

            return null;
        }

        /// <summary>
        /// 获取指定玩家的对手
        /// </summary>
        public static ZBBattlePlayer GetOpponent(this ZBBattleRoom self, long playerId)
        {
            if (self.Player1 != null && self.Player1.PlayerId == playerId)
            {
                return self.Player2;
            }

            if (self.Player2 != null && self.Player2.PlayerId == playerId)
            {
                return self.Player1;
            }

            return null;
        }

        /// <summary>
        /// 当前是否接受玩家输入（仅战斗中阶段接受）
        /// </summary>
        public static bool IsAcceptingInput(this ZBBattleRoom self)
        {
            return self.Phase == ZBBattlePhase.Fighting;
        }

        /// <summary>
        /// 向战斗中的双方推送消息
        /// </summary>
        public static void Broadcast(this ZBBattleRoom self, MessageObject message)
        {
            if (self.Player1?.Session != null && !self.Player1.Session.IsDisposed)
            {
                self.Player1.Session.Send(message);
            }

            if (self.Player2?.Session != null && !self.Player2.Session.IsDisposed)
            {
                self.Player2.Session.Send(message);
            }
        }
    }
}
