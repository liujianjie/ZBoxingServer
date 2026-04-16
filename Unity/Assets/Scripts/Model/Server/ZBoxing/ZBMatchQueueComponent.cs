using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 匹配队列中的玩家信息
    /// </summary>
    [EnableClass]
    public class ZBMatchPlayer
    {
        public long PlayerId;
        public string Nickname;
        public Session Session;
        public long EnqueueTime; // 入队时间戳（毫秒）
    }

    /// <summary>
    /// 匹配队列组件，挂载在Gate Scene上
    /// 维护等待匹配的玩家队列，凑齐2人后自动创建房间
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ZBMatchQueueComponent : Entity, IAwake, IDestroy, IUpdate
    {
        /// <summary>
        /// 等待匹配的玩家队列（先进先出）
        /// </summary>
        public List<ZBMatchPlayer> Queue = new();

        /// <summary>
        /// 玩家ID → 队列索引快速查找（用于取消匹配）
        /// </summary>
        public Dictionary<long, ZBMatchPlayer> PlayerMap = new();
    }
}
