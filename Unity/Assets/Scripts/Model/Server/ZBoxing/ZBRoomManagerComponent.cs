using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// ZBoxing房间管理器，挂载在Gate Scene上
    /// 管理所有活跃房间，提供创建/加入/离开/查询功能
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ZBRoomManagerComponent : Entity, IAwake, IDestroy, IUpdate
    {
        /// <summary>
        /// 房间ID → 房间Entity的InstanceId
        /// </summary>
        public Dictionary<int, long> RoomIdToInstanceId = new();

        /// <summary>
        /// 玩家ID → 所在房间ID（快速查找玩家在哪个房间）
        /// </summary>
        public Dictionary<long, int> PlayerToRoomId = new();

        /// <summary>
        /// 下一个可分配的房间ID
        /// </summary>
        public int NextRoomId = 1;
    }
}
