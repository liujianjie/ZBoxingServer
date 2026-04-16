using System.Collections.Generic;

namespace ET.Server
{
    // ============================================================
    // ZBBattleComponent 系统（战斗管理器生命周期）
    // ============================================================
    [EntitySystemOf(typeof(ZBBattleComponent))]
    [FriendOf(typeof(ZBBattleComponent))]
    [FriendOf(typeof(ZBBattleRoom))]
    [FriendOf(typeof(ZBRoomManagerComponent))]
    [FriendOf(typeof(ZBRoomComponent))]
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
        /// 管理器Update：扫描已标记为待清理的战斗，执行销毁+房间重置
        /// 在此处处理避免ZBBattleRoomSystem→ZBBattleComponentSystem的循环依赖
        /// </summary>
        [EntitySystem]
        private static void Update(this ZBBattleComponent self)
        {
            // 收集待清理的战斗ID（避免迭代中修改字典）
            List<long> toCleanup = null;
            foreach (var kv in self.BattleIdToInstanceId)
            {
                ZBBattleRoom battle = self.GetChild<ZBBattleRoom>(kv.Value);
                if (battle == null) continue;

                // CleanupCountdown == -1 表示已标记待清理
                if (battle.CleanupCountdown == -1)
                {
                    toCleanup ??= new List<long>();
                    toCleanup.Add(kv.Key);
                }
            }

            if (toCleanup == null) return;

            // 执行清理（内联房间重置逻辑，避免循环依赖ZBRoomManagerComponentSystem）
            Scene root = self.Root();
            ZBRoomManagerComponent roomManager = root?.GetComponent<ZBRoomManagerComponent>();

            foreach (long battleId in toCleanup)
            {
                ZBBattleRoom battle = self.GetBattle(battleId);
                if (battle != null)
                {
                    int roomId = battle.RoomId;

                    // 内联房间重置（不调用ZBRoomManagerComponentSystem扩展方法）
                    if (roomManager != null)
                    {
                        ResetRoomAfterBattle(roomManager, roomId, root);
                    }

                    Log.Info($"[ZBoxing] 执行战斗清理: BattleId={battleId}, RoomId={roomId}");
                }

                // 销毁战斗Entity
                self.DestroyBattle(battleId);
            }
        }

        /// <summary>
        /// 战斗结束后直接重置房间状态（内联，不调用ZBRoomManagerComponentSystem避免循环依赖）
        /// </summary>
        private static void ResetRoomAfterBattle(ZBRoomManagerComponent roomManager, int roomId, Scene root)
        {
            if (!roomManager.RoomIdToInstanceId.TryGetValue(roomId, out long instanceId))
            {
                return;
            }

            ZBRoomComponent room = roomManager.GetChild<ZBRoomComponent>(instanceId);
            if (room == null)
            {
                return;
            }

            // 重置房间状态和准备状态
            room.State = ZBRoomState.Full;
            if (room.Host != null) room.Host.IsReady = false;
            if (room.Guest != null) room.Guest.IsReady = false;

            Log.Info($"[ZBoxing] 房间战后重置: RoomId={roomId}, State=Full");
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

            // 注册映射（Bot PlayerId == -1 时跳过注册，避免字典冲突）
            self.BattleIdToInstanceId[battleRoom.BattleId] = battleRoom.InstanceId;
            self.PlayerToBattleId[host.PlayerId] = battleRoom.BattleId;
            if (guest.PlayerId != -1)
            {
                self.PlayerToBattleId[guest.PlayerId] = battleRoom.BattleId;
            }

            // 如果Player2是Bot（Session为null），创建Bot控制器子Entity
            if (battleRoom.Player2.Session == null)
            {
                battleRoom.AddChild<ZBBotController, long, int>(battleRoom.Player2.PlayerId, 2);
                Log.Info($"[ZBoxing] Bot控制器已创建: PlayerId={battleRoom.Player2.PlayerId}, BattleId={battleRoom.BattleId}");
            }

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
            // 战斗已结束，处理清理倒计时
            if (self.Phase == ZBBattlePhase.KO || self.Phase == ZBBattlePhase.TimeUp)
            {
                if (self.CleanupCountdown > 0)
                {
                    self.CleanupCountdown--;
                    if (self.CleanupCountdown <= 0)
                    {
                        self.ExecuteCleanup();
                    }
                }
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

        /// <summary>
        /// 标记战斗需要清理（由ZBBattleComponentSystem.Update统一处理，避免循环依赖）
        /// </summary>
        private static void ExecuteCleanup(this ZBBattleRoom self)
        {
            // 标记为需清理（CleanupCountdown设为-1表示待清理）
            self.CleanupCountdown = -1;
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

            // 2. 应用输入 → 更新玩家状态（角色状态机 E.4）
            self.ApplyInput(self.Player1, input1);
            self.ApplyInput(self.Player2, input2);

            // 3. 碰撞检测 — 双方互相检测攻击命中（E.6）
            self.CheckHitDetection(self.Player1, self.Player2);
            self.CheckHitDetection(self.Player2, self.Player1);

            // 4. 连击重置计时（E.7）
            TickComboReset(self.Player1);
            TickComboReset(self.Player2);

            // 5. 递减剩余时间
            self.RemainingFrames--;

            // 6. 检查胜负条件
            if (self.Player1.Hp <= 0 || self.Player2.Hp <= 0)
            {
                self.EndBattle(ZBBattlePhase.KO);
                return;
            }

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
        // 角色状态机（E.4）— 每帧更新单个玩家状态
        // 状态分类：
        //   锁定态: 攻击(Jab/Cross/Hook/Uppercut)、闪避(Dodge)、受击(HitStun)、KO
        //           → 帧计数驱动，到期自动回Idle，期间拒绝输入
        //   持续态: 格挡(Block) → 有Block输入维持，无则退出
        //   可取消态: Idle/MoveForward/MoveBackward → 随时接受新输入
        // ============================================================
        private static void ApplyInput(this ZBBattleRoom self, ZBBattlePlayer player, ZBInputFrame input)
        {
            if (player == null)
            {
                return;
            }

            // --- KO终态：不处理任何输入 ---
            if (player.AnimState == ZBAnimState.KO)
            {
                return;
            }

            // --- 推进动画帧计数 ---
            if (player.AnimFrame > 0)
            {
                player.AnimFrame++;
            }

            // --- 体力自然回复（非攻击/格挡/闪避状态时） ---
            if (!IsActionState(player.AnimState))
            {
                player.StaminaRegenDelay--;
                if (player.StaminaRegenDelay <= 0)
                {
                    player.StaminaRegenDelay = 0;
                    // 5点/秒 ÷ 30帧 ≈ 每6帧回复1点
                    if (self.CurrentFrame % 6 == 0 && player.Stamina < ZBBattleConst.InitStamina)
                    {
                        player.Stamina++;
                    }
                }
            }

            // --- 锁定状态处理（攻击/闪避/受击） ---
            if (IsLockedState(player.AnimState))
            {
                int totalFrames = GetAnimTotalFrames(player.AnimState);
                if (player.AnimFrame >= totalFrames)
                {
                    // 动作结束，回到Idle
                    player.AnimState = ZBAnimState.Idle;
                    player.AnimFrame = 0;
                }
                else
                {
                    // 动作未结束，忽略新输入（但仍更新朝向）
                    self.UpdateFacing(player);
                    return;
                }
            }

            // --- 格挡持续态处理 ---
            if (player.AnimState == ZBAnimState.Block)
            {
                // 持续格挡消耗体力: 3/秒 ÷ 30帧 ≈ 每10帧消耗1点
                if (self.CurrentFrame % 10 == 0)
                {
                    player.Stamina--;
                }

                // 体力耗尽或松开格挡键 → 退出格挡
                bool holdingBlock = input != null && input.Action == ZBInputAction.Block;
                if (player.Stamina <= 0 || !holdingBlock)
                {
                    player.AnimState = ZBAnimState.Idle;
                    player.AnimFrame = 0;
                    if (player.Stamina < 0) player.Stamina = 0;
                    // 格挡结束后有5帧后摇，设置回复延迟
                    player.StaminaRegenDelay = ZBBattleConst.BlockRecoveryFrames;
                }
                else
                {
                    // 继续格挡，不处理移动
                    self.UpdateFacing(player);
                    return;
                }
            }

            // --- 处理新动作输入（优先于移动） ---
            if (input != null && input.Action != ZBInputAction.None)
            {
                int staminaCost = GetActionStaminaCost(input.Action);

                // 体力检查
                if (player.Stamina >= staminaCost)
                {
                    int animState = ActionToAnimState(input.Action);
                    if (animState != ZBAnimState.Idle)
                    {
                        // 格挡进入持续态（不消耗前置体力）
                        if (animState == ZBAnimState.Block)
                        {
                            player.AnimState = ZBAnimState.Block;
                            player.AnimFrame = 0;
                        }
                        else
                        {
                            // 攻击/闪避: 扣除体力、进入锁定态
                            player.Stamina -= staminaCost;
                            player.AnimState = animState;
                            player.AnimFrame = 1;
                            // 动作后设置体力回复延迟
                            player.StaminaRegenDelay = ZBBattleConst.StaminaRegenDelayFrames;
                        }
                        self.UpdateFacing(player);
                        return;
                    }
                }
                // 体力不足时忽略动作，继续处理移动
            }

            // --- 处理移动方向 ---
            if (input != null && input.MoveDir != 0)
            {
                float moveSpeed = 3.0f / ZBBattleConst.TickRate; // 3单位/秒 → 每帧移动量
                player.PosX += input.MoveDir * moveSpeed;

                // 限制在场地范围内
                if (player.PosX < ZBBattleConst.ArenaMinX) player.PosX = ZBBattleConst.ArenaMinX;
                if (player.PosX > ZBBattleConst.ArenaMaxX) player.PosX = ZBBattleConst.ArenaMaxX;

                // 更新移动动画状态
                bool movingForward = (player.FacingRight && input.MoveDir > 0)
                                     || (!player.FacingRight && input.MoveDir < 0);
                player.AnimState = movingForward ? ZBAnimState.MoveForward : ZBAnimState.MoveBackward;
            }
            else
            {
                // 无移动输入 → 回Idle
                if (player.AnimState == ZBAnimState.MoveForward || player.AnimState == ZBAnimState.MoveBackward)
                {
                    player.AnimState = ZBAnimState.Idle;
                }
            }

            // --- 更新朝向 ---
            self.UpdateFacing(player);
        }

        /// <summary>
        /// 外部触发受击硬直（E.6碰撞检测调用）
        /// </summary>
        public static void ApplyHitStun(this ZBBattleRoom self, ZBBattlePlayer player)
        {
            if (player == null || player.AnimState == ZBAnimState.KO)
            {
                return;
            }

            player.AnimState = ZBAnimState.HitStun;
            player.AnimFrame = 1;
        }

        /// <summary>
        /// 触发KO状态（HP归零时调用）
        /// </summary>
        public static void ApplyKO(this ZBBattleRoom self, ZBBattlePlayer player)
        {
            if (player == null)
            {
                return;
            }

            player.AnimState = ZBAnimState.KO;
            player.AnimFrame = 0;
            player.Hp = 0;
        }

        /// <summary>
        /// 更新玩家朝向（始终面向对手）
        /// </summary>
        private static void UpdateFacing(this ZBBattleRoom self, ZBBattlePlayer player)
        {
            ZBBattlePlayer opponent = self.GetOpponent(player.PlayerId);
            if (opponent != null)
            {
                player.FacingRight = player.PosX < opponent.PosX;
            }
        }

        // ============================================================
        // 状态分类辅助方法
        // ============================================================

        /// <summary>
        /// 锁定状态：帧计数驱动，到期自动回Idle，期间拒绝输入
        /// </summary>
        private static bool IsLockedState(int animState)
        {
            return animState == ZBAnimState.Jab
                   || animState == ZBAnimState.Cross
                   || animState == ZBAnimState.Hook
                   || animState == ZBAnimState.Uppercut
                   || animState == ZBAnimState.Dodge
                   || animState == ZBAnimState.HitStun;
        }

        /// <summary>
        /// 动作状态（攻击/格挡/闪避）：体力回复暂停
        /// </summary>
        private static bool IsActionState(int animState)
        {
            return animState == ZBAnimState.Jab
                   || animState == ZBAnimState.Cross
                   || animState == ZBAnimState.Hook
                   || animState == ZBAnimState.Uppercut
                   || animState == ZBAnimState.Block
                   || animState == ZBAnimState.Dodge;
        }

        /// <summary>
        /// 输入动作 → 动画状态映射
        /// </summary>
        private static int ActionToAnimState(int action)
        {
            switch (action)
            {
                case ZBInputAction.Jab: return ZBAnimState.Jab;
                case ZBInputAction.Cross: return ZBAnimState.Cross;
                case ZBInputAction.Hook: return ZBAnimState.Hook;
                case ZBInputAction.Uppercut: return ZBAnimState.Uppercut;
                case ZBInputAction.Block: return ZBAnimState.Block;
                case ZBInputAction.Dodge: return ZBAnimState.Dodge;
                default: return ZBAnimState.Idle;
            }
        }

        /// <summary>
        /// 获取动作的体力消耗（从ZBMoveDatabase查找）
        /// </summary>
        private static int GetActionStaminaCost(int action)
        {
            if (action == ZBInputAction.Block) return 0; // 格挡持续消耗，不在此处扣除
            ZBMoveData data = ZBMoveDatabase.Get(action);
            return data?.StaminaCost ?? 0;
        }

        /// <summary>
        /// 获取动作的总帧数（从ZBMoveDatabase查找）
        /// </summary>
        private static int GetAnimTotalFrames(int animState)
        {
            return ZBMoveDatabase.GetTotalFrames(animState);
        }

        // ============================================================
        // E.6 碰撞检测：攻击者Hitbox vs 防御者Hurtbox
        // ============================================================

        /// <summary>
        /// 检测攻击者是否命中防御者
        /// </summary>
        private static void CheckHitDetection(this ZBBattleRoom self, ZBBattlePlayer attacker, ZBBattlePlayer defender)
        {
            if (attacker == null || defender == null) return;

            // 攻击者必须在攻击状态的活动帧阶段
            int phase = ZBMoveDatabase.GetCurrentPhase(attacker.AnimState, attacker.AnimFrame);
            if (phase != ZBMovePhase.Active) return;

            // 防止同一次攻击多次命中（只在活动帧第一帧判定）
            ZBMoveData moveData = ZBMoveDatabase.GetByAnimState(attacker.AnimState);
            if (moveData == null) return;
            int activeStartFrame = moveData.StartupFrames + 1;
            if (attacker.AnimFrame != activeStartFrame) return;

            // 防御者闪避无敌帧 → 不命中
            if (ZBMoveDatabase.IsInvincible(defender.AnimState, defender.AnimFrame))
            {
                // 广播闪避事件
                self.BroadcastBattleEvent(3, attacker.PlayerId, defender.PlayerId, 0, moveData.MoveType);
                return;
            }

            // AABB碰撞检测：攻击者Hitbox与防御者Hurtbox
            if (!IsHitboxOverlap(attacker, defender))
            {
                return;
            }

            // === 命中确认 ===

            // 基础伤害
            int damage = moveData.Damage;
            bool isBlocking = defender.AnimState == ZBAnimState.Block;

            if (isBlocking)
            {
                // 格挡减伤
                damage = damage * (100 - ZBBattleConst.BlockDamageReductionPct) / 100;
                // 格挡打断连击
                defender.ComboCount = 0;
                defender.ComboResetCounter = 0;

                // 广播格挡事件
                self.BroadcastBattleEvent(2, attacker.PlayerId, defender.PlayerId, damage, moveData.MoveType);
            }
            else
            {
                // 正面命中 — 连击加成
                int comboBonus = defender.ComboCount * ZBBattleConst.ComboBonusPct;
                if (comboBonus > ZBBattleConst.ComboMaxBonusPct)
                {
                    comboBonus = ZBBattleConst.ComboMaxBonusPct;
                }
                damage = damage * (100 + comboBonus) / 100;

                // 受击硬直
                self.ApplyHitStun(defender);

                // 连击计数+1，重置衰减计时器
                defender.ComboCount++;
                defender.ComboResetCounter = ZBBattleConst.ComboResetFrames;

                // 广播命中事件
                self.BroadcastBattleEvent(1, attacker.PlayerId, defender.PlayerId, damage, moveData.MoveType);
            }

            // 扣血
            defender.Hp -= damage;
            if (defender.Hp < 0) defender.Hp = 0;

            // 击退
            float knockback = ZBBattleConst.KnockbackBase * moveData.KnockbackMult;
            if (isBlocking) knockback *= 0.5f; // 格挡时击退减半
            float knockDir = attacker.FacingRight ? 1f : -1f;
            defender.PosX += knockDir * knockback;

            // 场地边界限制
            if (defender.PosX < ZBBattleConst.ArenaMinX) defender.PosX = ZBBattleConst.ArenaMinX;
            if (defender.PosX > ZBBattleConst.ArenaMaxX) defender.PosX = ZBBattleConst.ArenaMaxX;

            // KO检测
            if (defender.Hp <= 0)
            {
                self.ApplyKO(defender);
                self.BroadcastBattleEvent(4, attacker.PlayerId, defender.PlayerId, 0, moveData.MoveType);
            }
        }

        /// <summary>
        /// AABB碰撞检测：攻击者Hitbox vs 防御者Hurtbox
        /// Hitbox: 攻击者面前延伸的矩形区域
        /// Hurtbox: 防御者身体的矩形区域
        /// </summary>
        private static bool IsHitboxOverlap(ZBBattlePlayer attacker, ZBBattlePlayer defender)
        {
            // 计算Hitbox（在攻击者面前）
            float hitboxCenterX;
            if (attacker.FacingRight)
            {
                hitboxCenterX = attacker.PosX + ZBBattleConst.HitboxReach * 0.5f;
            }
            else
            {
                hitboxCenterX = attacker.PosX - ZBBattleConst.HitboxReach * 0.5f;
            }

            float hitboxMinX = hitboxCenterX - ZBBattleConst.HitboxHalfWidth;
            float hitboxMaxX = hitboxCenterX + ZBBattleConst.HitboxHalfWidth;

            // Hurtbox（防御者身体中心）
            float hurtboxMinX = defender.PosX - ZBBattleConst.HurtboxHalfWidth;
            float hurtboxMaxX = defender.PosX + ZBBattleConst.HurtboxHalfWidth;

            // 1D AABB重叠检测（2D侧视角，Y轴不需要）
            return hitboxMaxX >= hurtboxMinX && hitboxMinX <= hurtboxMaxX;
        }

        /// <summary>
        /// 连击重置计时：被命中后开始倒数，到期后连击数归零
        /// </summary>
        private static void TickComboReset(ZBBattlePlayer player)
        {
            if (player == null || player.ComboCount <= 0) return;

            if (player.ComboResetCounter > 0)
            {
                player.ComboResetCounter--;
            }
            else
            {
                // 计时器到期，重置连击
                player.ComboCount = 0;
            }
        }

        /// <summary>
        /// 广播战斗事件
        /// </summary>
        private static void BroadcastBattleEvent(this ZBBattleRoom self,
            int eventType, long attackerId, long defenderId, int damage, int moveType)
        {
            G2C_ZBBattleEvent evt = G2C_ZBBattleEvent.Create();
            evt.EventType = eventType;
            evt.AttackerId = attackerId;
            evt.DefenderId = defenderId;
            evt.Damage = damage;
            evt.MoveType = moveType;
            self.Broadcast(evt);
        }

        // ============================================================
        // E.8 战斗结束处理（胜负判定+广播+延迟清理）
        // ============================================================
        private static void EndBattle(this ZBBattleRoom self, int endPhase)
        {
            self.Phase = endPhase;

            // 判定胜者
            long winnerId = 0;
            int reason = endPhase == ZBBattlePhase.KO ? 1 : 2; // 1=KO, 2=时间到

            if (endPhase == ZBBattlePhase.KO)
            {
                bool p1Dead = self.Player1.Hp <= 0;
                bool p2Dead = self.Player2.Hp <= 0;

                if (p1Dead && p2Dead)
                {
                    // 双方同时KO → 平局
                    winnerId = 0;
                }
                else if (p1Dead)
                {
                    winnerId = self.Player2.PlayerId;
                }
                else if (p2Dead)
                {
                    winnerId = self.Player1.PlayerId;
                }
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

            // 启动清理倒计时（在Update中倒数后执行清理）
            self.CleanupCountdown = ZBBattleConst.BattleCleanupFrames;
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

            // 刷新双方Session活跃时间，防止战斗期间客户端零上行流量时
            // 被SessionIdleChecker误判超时并断开连接（与BUG-4等待阶段修复保持一致）
            long clientNow = TimeInfo.Instance.ClientNow();
            if (self.Player1?.Session != null && !self.Player1.Session.IsDisposed)
            {
                self.Player1.Session.LastRecvTime = clientNow;
            }
            if (self.Player2?.Session != null && !self.Player2.Session.IsDisposed)
            {
                self.Player2.Session.LastRecvTime = clientNow;
            }
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
        /// 检查客户端帧号是否在容忍范围内
        /// </summary>
        public static bool IsFrameInTolerance(this ZBBattleRoom self, int clientFrame)
        {
            int frameDiff = clientFrame - self.CurrentFrame;
            return frameDiff >= -ZBBattleConst.InputFrameTolerance
                   && frameDiff <= ZBBattleConst.InputFrameTolerance;
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
