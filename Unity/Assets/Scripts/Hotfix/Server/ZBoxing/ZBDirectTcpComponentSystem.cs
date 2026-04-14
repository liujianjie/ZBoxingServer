// 直连TCP组件系统 — 处理Gate场景TCP直连端口的生命周期、连接管理和消息路由
using System;
using System.Net;

namespace ET.Server
{
    [EntitySystemOf(typeof(ZBDirectTcpComponent))]
    [FriendOf(typeof(ZBDirectTcpComponent))]
    public static partial class ZBDirectTcpComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ZBDirectTcpComponent self, IPEndPoint endPoint)
        {
            self.TcpService = new TService(endPoint, ServiceType.Outer);

            // 新连接回调: 为每个客户端创建Session子实体
            self.TcpService.AcceptCallback = self.OnAccept;
            // 消息读取回调: 反序列化后走标准Gate handler路由
            self.TcpService.ReadCallback = self.OnRead;
            // 错误/断开回调: 清理Session实体
            self.TcpService.ErrorCallback = self.OnError;

            Log.Info($"[ZBDirectTcp] TCP直连端口已启动: {endPoint}");
        }

        [EntitySystem]
        private static void Update(this ZBDirectTcpComponent self)
        {
            self.TcpService?.Update();
        }

        [EntitySystem]
        private static void Destroy(this ZBDirectTcpComponent self)
        {
            self.TcpService?.Dispose();
            self.TcpService = null;
        }

        private static void OnAccept(this ZBDirectTcpComponent self, long channelId, IPEndPoint ipEndPoint)
        {
            Session session = self.AddChildWithId<Session, AService>(channelId, self.TcpService);
            session.RemoteAddress = ipEndPoint;
            session.AddComponent<SessionAcceptTimeoutComponent>();
            session.AddComponent<SessionIdleCheckerComponent>();
            Log.Info($"[ZBDirectTcp] 客户端连接: {ipEndPoint}, ChannelId={channelId}");
        }

        private static void OnRead(this ZBDirectTcpComponent self, long channelId, MemoryBuffer memoryBuffer)
        {
            Session session = self.GetChild<Session>(channelId);
            if (session == null)
            {
                self.TcpService.Recycle(memoryBuffer);
                return;
            }

            try
            {
                // 反序列化: [2字节opcode LE][protobuf载荷]
                (ActorId _, object message) = MessageSerializeHelper.ToMessage(self.TcpService, memoryBuffer);
                self.TcpService.Recycle(memoryBuffer);

                if (message == null)
                {
                    Log.Error($"[ZBDirectTcp] 消息反序列化失败: ChannelId={channelId}");
                    return;
                }

                // 路由到Gate场景的handler（与Router转发路径完全一致）
                EventSystem.Instance.Invoke(
                    (long)self.IScene.SceneType,
                    new NetComponentOnRead() { Session = session, Message = message }
                );
            }
            catch (Exception e)
            {
                Log.Error($"[ZBDirectTcp] 消息处理异常: {e}");
                self.TcpService.Recycle(memoryBuffer);
            }
        }

        private static void OnError(this ZBDirectTcpComponent self, long channelId, int error)
        {
            Session session = self.GetChild<Session>(channelId);
            if (session == null)
            {
                return;
            }

            Log.Info($"[ZBDirectTcp] 客户端断开: {session.RemoteAddress}, Error={error}");
            session.Error = error;
            session.Dispose();
        }
    }
}
