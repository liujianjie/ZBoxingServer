// 直连TCP组件定义 — Gate场景对外暴露TCP端口，供ZFrameWork客户端直接连接（跳过Router）
using System.Net;

namespace ET.Server
{
    /// <summary>
    /// 直连TCP组件 — 在Gate场景上提供外部TCP端口，让ZFrameWork客户端直接连接（跳过Router）
    /// wire format: [2字节长度LE][2字节opcode LE][protobuf载荷]
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ZBDirectTcpComponent : Entity, IAwake<IPEndPoint>, IUpdate, IDestroy
    {
        public TService TcpService;
    }
}
