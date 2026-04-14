namespace ET.Server
{
    /// <summary>
    /// 创建房间Handler
    /// 客户端发送房间名 → 服务端创建房间并返回房间信息
    /// </summary>
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_ZBCreateRoomHandler : MessageSessionHandler<C2G_ZBCreateRoom, G2C_ZBCreateRoom>
    {
        protected override async ETTask Run(Session session, C2G_ZBCreateRoom request, G2C_ZBCreateRoom response)
        {
            // 验证登录状态
            ZBSessionPlayerComponent sessionPlayer = session.GetComponent<ZBSessionPlayerComponent>();
            if (sessionPlayer == null)
            {
                response.ErrorCode = ZBErrorCode.NotLoggedIn;
                return;
            }

            Scene root = session.Root();

            // 获取房间管理器（懒初始化）
            ZBRoomManagerComponent roomManager = root.GetComponent<ZBRoomManagerComponent>();
            if (roomManager == null)
            {
                roomManager = root.AddComponent<ZBRoomManagerComponent>();
            }

            // 获取账户组件（用于生成PlayerBrief）
            ZBAccountComponent accountComponent = root.GetComponent<ZBAccountComponent>();
            if (accountComponent == null)
            {
                response.ErrorCode = ZBErrorCode.ServerError;
                return;
            }

            // 创建房间
            int errorCode = roomManager.CreateRoom(
                request.RoomName,
                sessionPlayer.GetPlayerId(),
                sessionPlayer.GetNickname(),
                session,
                out ZBRoomComponent room
            );

            response.ErrorCode = errorCode;

            if (errorCode == ZBErrorCode.Success && room != null)
            {
                response.Room = roomManager.ToRoomInfo(room, accountComponent);
            }

            await ETTask.CompletedTask;
        }
    }
}
