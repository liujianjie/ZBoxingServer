namespace ET.Server
{
    /// <summary>
    /// 准备状态Handler
    /// 客户端发送准备/取消准备 → 服务端更新状态 → 广播房间更新
    /// 双方都准备后 → 推送G2C_ZBBattleStart开战通知
    /// </summary>
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_ZBReadyHandler : MessageSessionHandler<C2G_ZBReady, G2C_ZBReady>
    {
        protected override async ETTask Run(Session session, C2G_ZBReady request, G2C_ZBReady response)
        {
            ZBSessionPlayerComponent sessionPlayer = session.GetComponent<ZBSessionPlayerComponent>();
            if (sessionPlayer == null)
            {
                response.ErrorCode = ZBErrorCode.NotLoggedIn;
                return;
            }

            Scene root = session.Root();

            ZBRoomManagerComponent roomManager = root.GetComponent<ZBRoomManagerComponent>();
            if (roomManager == null)
            {
                response.ErrorCode = ZBErrorCode.NotInRoom;
                return;
            }

            ZBAccountComponent accountComponent = root.GetComponent<ZBAccountComponent>();
            if (accountComponent == null)
            {
                response.ErrorCode = ZBErrorCode.ServerError;
                return;
            }

            // 设置准备状态
            int errorCode = roomManager.SetReady(
                sessionPlayer.GetPlayerId(),
                request.Ready,
                out ZBRoomComponent room
            );

            response.ErrorCode = errorCode;

            if (errorCode != ZBErrorCode.Success || room == null)
            {
                return;
            }

            // 广播房间状态更新（让双方看到准备状态变化）
            roomManager.BroadcastRoomUpdate(room, accountComponent);

            // 检查是否满足开战条件
            roomManager.TryStartBattle(room);

            await ETTask.CompletedTask;
        }
    }
}
