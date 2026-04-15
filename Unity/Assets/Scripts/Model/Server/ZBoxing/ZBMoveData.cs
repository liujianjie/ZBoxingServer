using System.Collections.Generic;

namespace ET.Server
{
    // ============================================================
    // 招式帧阶段枚举
    // ============================================================
    public static class ZBMovePhase
    {
        public const int None = 0;       // 非攻击状态
        public const int Startup = 1;    // 前摇（可被打断）
        public const int Active = 2;     // 活动帧（碰撞判定生效）
        public const int Recovery = 3;   // 后摇（硬直，不可取消）
    }

    // ============================================================
    // 招式帧数据定义（来自设计文档 @30fps）
    // ============================================================
    [EnableClass]
    public class ZBMoveData
    {
        public int MoveType;           // ZBInputAction枚举值
        public string Name;            // 招式名称
        public int Damage;             // 伤害
        public int StaminaCost;        // 体力消耗
        public int StartupFrames;      // 前摇帧数
        public int ActiveFrames;       // 活动帧数（碰撞判定）
        public int RecoveryFrames;     // 后摇帧数
        public float KnockbackMult;    // 击退距离倍率（基础0.5单位）

        /// <summary>
        /// 总帧数 = 前摇 + 活动 + 后摇
        /// </summary>
        public int TotalFrames => StartupFrames + ActiveFrames + RecoveryFrames;
    }

    // ============================================================
    // 招式数据库（静态查找表）
    // 后续可改为从Luban配表加载
    // ============================================================
    public static class ZBMoveDatabase
    {
        [StaticField]
        private static readonly Dictionary<int, ZBMoveData> Moves = new()
        {
            {
                ZBInputAction.Jab, new ZBMoveData
                {
                    MoveType = ZBInputAction.Jab,
                    Name = "刺拳",
                    Damage = 8,
                    StaminaCost = 10,
                    StartupFrames = 3,
                    ActiveFrames = 2,
                    RecoveryFrames = 4,
                    KnockbackMult = 0.5f,
                }
            },
            {
                ZBInputAction.Cross, new ZBMoveData
                {
                    MoveType = ZBInputAction.Cross,
                    Name = "直拳",
                    Damage = 15,
                    StaminaCost = 15,
                    StartupFrames = 5,
                    ActiveFrames = 3,
                    RecoveryFrames = 6,
                    KnockbackMult = 1.0f,
                }
            },
            {
                ZBInputAction.Hook, new ZBMoveData
                {
                    MoveType = ZBInputAction.Hook,
                    Name = "勾拳",
                    Damage = 22,
                    StaminaCost = 20,
                    StartupFrames = 7,
                    ActiveFrames = 3,
                    RecoveryFrames = 8,
                    KnockbackMult = 1.5f,
                }
            },
            {
                ZBInputAction.Uppercut, new ZBMoveData
                {
                    MoveType = ZBInputAction.Uppercut,
                    Name = "上勾拳",
                    Damage = 30,
                    StaminaCost = 30,
                    StartupFrames = 9,
                    ActiveFrames = 2,
                    RecoveryFrames = 10,
                    KnockbackMult = 2.0f,
                }
            },
            {
                ZBInputAction.Dodge, new ZBMoveData
                {
                    MoveType = ZBInputAction.Dodge,
                    Name = "闪避",
                    Damage = 0,
                    StaminaCost = 20,
                    StartupFrames = 2,
                    ActiveFrames = 6,   // 无敌帧
                    RecoveryFrames = 3,
                    KnockbackMult = 0f,
                }
            },
        };

        /// <summary>
        /// 受击硬直帧数
        /// </summary>
        public const int HitStunFrames = 8;

        /// <summary>
        /// 获取招式数据，不存在返回null
        /// </summary>
        public static ZBMoveData Get(int moveType)
        {
            Moves.TryGetValue(moveType, out ZBMoveData data);
            return data;
        }

        /// <summary>
        /// 根据AnimState获取对应的招式数据
        /// </summary>
        public static ZBMoveData GetByAnimState(int animState)
        {
            int action = AnimStateToAction(animState);
            if (action == ZBInputAction.None) return null;
            return Get(action);
        }

        /// <summary>
        /// 获取指定动画状态的总帧数
        /// </summary>
        public static int GetTotalFrames(int animState)
        {
            ZBMoveData data = GetByAnimState(animState);
            if (data != null) return data.TotalFrames;
            if (animState == ZBAnimState.HitStun) return HitStunFrames;
            return 0;
        }

        /// <summary>
        /// 判断当前帧处于招式的哪个阶段
        /// </summary>
        /// <param name="animState">动画状态</param>
        /// <param name="animFrame">当前动作帧号(从1开始)</param>
        /// <returns>ZBMovePhase枚举</returns>
        public static int GetCurrentPhase(int animState, int animFrame)
        {
            ZBMoveData data = GetByAnimState(animState);
            if (data == null)
            {
                if (animState == ZBAnimState.HitStun) return ZBMovePhase.Recovery; // 受击视为后摇
                return ZBMovePhase.None;
            }

            if (animFrame <= data.StartupFrames)
            {
                return ZBMovePhase.Startup;
            }
            else if (animFrame <= data.StartupFrames + data.ActiveFrames)
            {
                return ZBMovePhase.Active;
            }
            else
            {
                return ZBMovePhase.Recovery;
            }
        }

        /// <summary>
        /// 判断当前帧是否在闪避无敌帧中
        /// </summary>
        public static bool IsInvincible(int animState, int animFrame)
        {
            return animState == ZBAnimState.Dodge
                   && GetCurrentPhase(animState, animFrame) == ZBMovePhase.Active;
        }

        /// <summary>
        /// AnimState → InputAction 反向映射
        /// </summary>
        private static int AnimStateToAction(int animState)
        {
            switch (animState)
            {
                case ZBAnimState.Jab: return ZBInputAction.Jab;
                case ZBAnimState.Cross: return ZBInputAction.Cross;
                case ZBAnimState.Hook: return ZBInputAction.Hook;
                case ZBAnimState.Uppercut: return ZBInputAction.Uppercut;
                case ZBAnimState.Dodge: return ZBInputAction.Dodge;
                default: return ZBInputAction.None;
            }
        }
    }
}
