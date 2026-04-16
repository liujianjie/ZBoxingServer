using System;
using System.Collections.Generic;

namespace ET.Server
{
    // ============================================================
    // ZBBotController 系统 — Bot AI决策逻辑
    // Bot以5帧为间隔进行决策，不参与帧同步，直接写入InputBuffer
    // ============================================================
    [EntitySystemOf(typeof(ZBBotController))]
    [FriendOf(typeof(ZBBotController))]
    [FriendOf(typeof(ZBBattleRoom))]
    [FriendOf(typeof(ZBBattlePlayer))]
    public static partial class ZBBotControllerSystem
    {
        [EntitySystem]
        private static void Awake(this ZBBotController self, long botPlayerId, int difficulty)
        {
            self.BotPlayerId = botPlayerId;
            self.Difficulty = difficulty;
            self.LastDecisionFrame = 0;
            self.DecisionInterval = 5; // 每5帧决策一次，约6次/秒
            self.Rng = new System.Random();
            Log.Info($"[ZBoxing] Bot控制器初始化: PlayerId={botPlayerId}, Difficulty={difficulty}");
        }

        [EntitySystem]
        private static void Destroy(this ZBBotController self)
        {
            Log.Info($"[ZBoxing] Bot控制器销毁: PlayerId={self.BotPlayerId}");
        }

        [EntitySystem]
        private static void Update(this ZBBotController self)
        {
            // 获取父Entity ZBBattleRoom
            ZBBattleRoom battleRoom = self.Parent as ZBBattleRoom;
            if (battleRoom == null)
            {
                return;
            }

            // 只在战斗进行中阶段决策
            if (battleRoom.Phase != ZBBattlePhase.Fighting)
            {
                return;
            }

            // 限制决策频率：距上次决策帧数不足则跳过
            if (battleRoom.CurrentFrame - self.LastDecisionFrame < self.DecisionInterval)
            {
                return;
            }

            // 找到Bot对应的ZBBattlePlayer
            ZBBattlePlayer botPlayer = battleRoom.GetPlayer(self.BotPlayerId);
            if (botPlayer == null)
            {
                return;
            }

            // Bot处于锁定状态（攻击/受击/闪避中）→ 不操作
            if (IsBotLocked(botPlayer.AnimState))
            {
                self.LastDecisionFrame = battleRoom.CurrentFrame;
                return;
            }

            // 获取对手
            ZBBattlePlayer opponent = battleRoom.GetOpponent(self.BotPlayerId);
            if (opponent == null)
            {
                return;
            }

            // 执行AI决策，得到 (moveDir, action)
            DecideAction(self, botPlayer, opponent, battleRoom, out int moveDir, out int action);

            // 只有有实际输入时才写入缓冲区
            if (moveDir != 0 || action != ZBInputAction.None)
            {
                // 限制输入缓冲区容量，避免堆积
                if (botPlayer.InputBuffer.Count < ZBBattleConst.InputBufferMax)
                {
                    botPlayer.InputBuffer.Add(new ZBInputFrame
                    {
                        Frame = battleRoom.CurrentFrame,
                        MoveDir = moveDir,
                        Action = action,
                    });
                }
            }

            self.LastDecisionFrame = battleRoom.CurrentFrame;
        }

        /// <summary>
        /// Bot AI决策 — 根据当前局面决定输入
        /// </summary>
        private static void DecideAction(
            ZBBotController self,
            ZBBattlePlayer botPlayer,
            ZBBattlePlayer opponent,
            ZBBattleRoom battleRoom,
            out int moveDir,
            out int action)
        {
            moveDir = 0;
            action = ZBInputAction.None;

            // 计算水平距离
            float dx = Math.Abs(botPlayer.PosX - opponent.PosX);

            // 朝向对手的移动方向（+1=右, -1=左）
            int towardOpponent = botPlayer.PosX < opponent.PosX ? 1 : -1;
            int awayFromOpponent = -towardOpponent;

            // 对手血量低时增加攻击欲望
            bool opponentLowHp = opponent.Hp < 30;

            // 体力不足 → 靠近/远离等待恢复，不出手
            if (botPlayer.Stamina < 20)
            {
                moveDir = dx > 1.5f ? towardOpponent : awayFromOpponent;
                action = ZBInputAction.None;
                return;
            }

            // 距离较远 → 移动靠近
            if (dx > 2.5f)
            {
                moveDir = towardOpponent;
                action = ZBInputAction.None;
                return;
            }

            // 近距离决策
            int roll = self.Rng.Next(100);

            // 对手血量低：额外激进（roll偏移）
            if (opponentLowHp)
            {
                roll = Math.Max(0, roll - 20);
            }

            // 对手在攻击中（AnimState 10~13） → 防御反应
            if (opponent.AnimState >= ZBAnimState.Jab && opponent.AnimState <= ZBAnimState.Uppercut)
            {
                if (roll < 30)
                {
                    // 30% 格挡
                    moveDir = 0;
                    action = ZBInputAction.Block;
                }
                else if (roll < 50)
                {
                    // 20% 闪避后撤
                    moveDir = awayFromOpponent;
                    action = ZBInputAction.Dodge;
                }
                else
                {
                    // 50% 等待
                    moveDir = 0;
                    action = ZBInputAction.None;
                }
                return;
            }

            // 对手受击硬直 → 进攻追击机会
            if (opponent.AnimState == ZBAnimState.HitStun)
            {
                if (roll < 80)
                {
                    // 80% 追击：随机选Jab/Cross/Hook
                    moveDir = 0;
                    action = PickAttack(self.Rng);
                }
                else
                {
                    moveDir = 0;
                    action = ZBInputAction.None;
                }
                return;
            }

            // 对手Idle/移动 → 常规决策
            if (roll < 40)
            {
                // 40% 攻击
                moveDir = 0;
                action = PickAttack(self.Rng);
            }
            else if (roll < 60)
            {
                // 20% 向对手移动
                moveDir = towardOpponent;
                action = ZBInputAction.None;
            }
            else if (roll < 75)
            {
                // 15% 格挡
                moveDir = 0;
                action = ZBInputAction.Block;
            }
            else
            {
                // 25% 等待
                moveDir = 0;
                action = ZBInputAction.None;
            }
        }

        /// <summary>
        /// 随机选择一个攻击动作（Jab/Cross/Hook）
        /// </summary>
        private static int PickAttack(System.Random rng)
        {
            int r = rng.Next(3);
            switch (r)
            {
                case 0: return ZBInputAction.Jab;
                case 1: return ZBInputAction.Cross;
                default: return ZBInputAction.Hook;
            }
        }

        /// <summary>
        /// 判断Bot是否处于锁定状态（攻击/受击/闪避中无法操作）
        /// </summary>
        private static bool IsBotLocked(int animState)
        {
            return animState == ZBAnimState.Jab
                   || animState == ZBAnimState.Cross
                   || animState == ZBAnimState.Hook
                   || animState == ZBAnimState.Uppercut
                   || animState == ZBAnimState.Dodge
                   || animState == ZBAnimState.HitStun
                   || animState == ZBAnimState.KO;
        }
    }
}
