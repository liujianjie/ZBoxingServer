using System.Collections.Generic;

namespace ET.Server
{
    // ============================================================
    // 战斗阶段枚举（与proto BattlePhase字段对齐）
    // ============================================================
    public static class ZBBattlePhase
    {
        public const int Countdown = 0;    // 倒计时
        public const int Fighting = 1;     // 战斗中
        public const int KO = 2;           // KO结束
        public const int TimeUp = 3;       // 时间到
    }

    // ============================================================
    // 动画状态枚举（与proto AnimState字段对齐）
    // ============================================================
    public static class ZBAnimState
    {
        public const int Idle = 0;
        public const int MoveForward = 1;
        public const int MoveBackward = 2;
        public const int Jab = 10;
        public const int Cross = 11;
        public const int Hook = 12;
        public const int Uppercut = 13;
        public const int Block = 20;
        public const int Dodge = 21;
        public const int HitStun = 30;
        public const int KO = 31;
    }

    // ============================================================
    // 输入动作枚举（与proto Action字段对齐）
    // ============================================================
    public static class ZBInputAction
    {
        public const int None = 0;
        public const int Jab = 1;
        public const int Cross = 2;
        public const int Hook = 3;
        public const int Uppercut = 4;
        public const int Block = 5;
        public const int Dodge = 6;
    }

    // ============================================================
    // 战斗常量
    // ============================================================
    public static class ZBBattleConst
    {
        public const int TickRate = 30;                             // 逻辑帧率(Hz)
        public const int RoundTimeSec = 60;                         // 回合时间(秒)
        public const int RoundFrames = RoundTimeSec * TickRate;     // 回合总帧数 = 1800
        public const int CountdownSec = 3;                          // 开战倒计时(秒)
        public const int CountdownFrames = CountdownSec * TickRate; // 倒计时帧数 = 90
        public const int InitHp = 100;                              // 初始HP
        public const int InitStamina = 100;                         // 初始体力
        public const float Player1StartX = -2.0f;                   // Player1初始X位置
        public const float Player2StartX = 2.0f;                    // Player2初始X位置
        public const int TickIntervalMs = 1000 / TickRate;            // 每帧间隔(毫秒) = 33
        public const int InputBufferMax = 10;                       // 输入缓冲区上限
        public const int InputFrameTolerance = 15;                  // 帧号容忍偏差（±15帧=±0.5秒）
        public const int StaminaRegenDelayFrames = 30;              // 动作后体力回复延迟(帧) = 1秒
        public const int BlockRecoveryFrames = 5;                   // 格挡解除后硬直(帧)
        public const float ArenaMinX = -5f;                         // 场地左边界
        public const float ArenaMaxX = 5f;                          // 场地右边界
        public const float HurtboxHalfWidth = 0.4f;                // 受击判定半宽
        public const float HitboxReach = 1.2f;                     // 攻击判定前方延伸距离
        public const float HitboxHalfWidth = 0.3f;                 // 攻击判定半宽
        public const float KnockbackBase = 0.5f;                   // 基础击退距离
        public const int BlockDamageReductionPct = 70;              // 格挡减伤百分比
        public const int ComboBonusPct = 10;                        // 连击伤害加成(每次+10%)
        public const int ComboMaxBonusPct = 50;                     // 连击伤害上限加成(50%)
        public const int ComboResetFrames = 30;                     // 连击重置延迟(帧) = 1秒无命中后重置
        public const int BattleCleanupFrames = 90;                  // 战斗结束后清理延迟(帧) = 3秒
    }

    // ============================================================
    // 战斗输入帧（缓冲区元素）
    // ============================================================
    [EnableClass]
    public class ZBInputFrame
    {
        public int Frame;       // 客户端声称的帧号
        public int MoveDir;     // -1=左, 0=静止, 1=右
        public int Action;      // ZBInputAction枚举值
    }

    // ============================================================
    // 战斗中的玩家数据
    // ============================================================
    [EnableClass]
    public class ZBBattlePlayer
    {
        // --- 身份 ---
        public long PlayerId;
        public string Nickname;
        public Session Session;           // 推送消息用（断线时为null）

        // --- 战斗属性 ---
        public int Hp;                    // 当前HP
        public int Stamina;               // 当前体力
        public float PosX;                // X坐标
        public float PosY;                // Y坐标（地面=0）
        public bool FacingRight;          // 朝向

        // --- 动作状态 ---
        public int AnimState;             // ZBAnimState枚举
        public int AnimFrame;             // 当前动作已执行帧数
        public int ComboCount;            // 连击计数
        public int StaminaRegenDelay;     // 体力回复延迟剩余帧数（>0时不回复）
        public int ComboResetCounter;     // 连击重置计数器（每帧递减，归零则ComboCount清零）

        // --- 输入缓冲 ---
        public List<ZBInputFrame> InputBuffer = new();

        // --- 关联 ---
        public int RoomId;                // 来源房间ID
    }

    // ============================================================
    // 单场战斗Entity（实现IUpdate参与30Hz游戏循环）
    // ============================================================
    [ChildOf(typeof(ZBBattleComponent))]
    public class ZBBattleRoom : Entity, IAwake, IDestroy, IUpdate
    {
        // --- 战斗标识 ---
        public long BattleId;             // 唯一战斗ID（自增分配）
        public int RoomId;                // 关联的房间ID

        // --- 双方玩家 ---
        public ZBBattlePlayer Player1;    // Player1（房主方）
        public ZBBattlePlayer Player2;    // Player2（客人方）

        // --- 帧与时间 ---
        public int CurrentFrame;          // 当前服务端逻辑帧号（从0开始）
        public int RemainingFrames;       // 剩余帧数
        public long StartTimeMs;          // 战斗创建时间戳(毫秒)

        // --- 30Hz固定帧率控制 ---
        public FixedTimeCounter FixedTimeCounter;  // 帧时间计算器(33ms间隔)

        // --- 阶段 ---
        public int Phase;                 // ZBBattlePhase枚举值
        public int CountdownFrames;       // 倒计时剩余帧数
        public int CleanupCountdown;      // 战斗结束后清理倒计时（帧数,>0时等待）
    }

    // ============================================================
    // 战斗管理器组件（挂载在Gate Scene上）
    // ============================================================
    [ComponentOf(typeof(Scene))]
    public class ZBBattleComponent : Entity, IAwake, IDestroy, IUpdate
    {
        /// <summary>
        /// 战斗ID → 战斗Entity的InstanceId
        /// </summary>
        public Dictionary<long, long> BattleIdToInstanceId = new();

        /// <summary>
        /// 玩家ID → 战斗ID（快速查找玩家在哪场战斗）
        /// </summary>
        public Dictionary<long, long> PlayerToBattleId = new();

        /// <summary>
        /// 下一个可分配的战斗ID
        /// </summary>
        public long NextBattleId = 1;
    }

    // ============================================================
    // Bot控制器Entity（挂载在ZBBattleRoom下）
    // ============================================================
    /// <summary>
    /// Bot控制器 — 管理一个虚拟玩家的AI决策
    /// 挂载在 ZBBattleRoom 下作为子Entity
    /// </summary>
    [ChildOf(typeof(ZBBattleRoom))]
    public class ZBBotController : Entity, IAwake<long, int>, IUpdate, IDestroy
    {
        /// <summary>Bot的PlayerId（通常为-1）</summary>
        public long BotPlayerId;

        /// <summary>难度等级 1=简单 2=中等 3=困难</summary>
        public int Difficulty;

        /// <summary>上次决策的帧号（避免每帧重复决策）</summary>
        public int LastDecisionFrame;

        /// <summary>决策间隔帧数（简单版每5帧决策一次=6次/秒）</summary>
        public int DecisionInterval;

        /// <summary>随机数生成器（服务端AI决策，不参与帧同步）</summary>
        public System.Random Rng;
    }
}
