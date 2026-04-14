namespace ET.Server
{
    /// <summary>
    /// 加入房间Handler
    /// 客户端发送房间ID → 服务端将玩家加入房间 → 广播房间更新
    /// </summary>
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_ZBJoinRoomHandler : MessageSessionHandler<C2G_ZBJoinRoom, G2C_ZBJoinRoom>
    {
        protected override async ETTask Run(Session session, C2G_ZBJoinRoom request, G2C_ZBJoinRoom response)
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
                response.ErrorCode = ZBErrorCode.RoomNotFound;
                return;
            }

            ZBAccountComponent accountComponent = root.GetComponent<ZBAccountComponent>();
            if (accountComponent == null)
            {
                response.ErrorCode = ZBErrorCode.ServerError;
                return;
            }

            // 加入房间
            int errorCode = roomManager.JoinRoom(
                request.RoomId,
                sessionPlayer.GetPlayerId(),
                sessionPlayer.GetNickname(),
                session,
                out ZBRoomComponent room
            );

            response.ErrorCode = errorCode;

            if (errorCode == ZBErrorCode.Success && room != null)
            {
                response.Room = roomManager.ToRoomInfo(room, accountComponent);

                // 向房间内所有玩家广播更新（包括房主）
                roomManager.BroadcastRoomUpdate(room, accountComponent);
            }

            await ETTask.CompletedTask;
        }
    }
}
