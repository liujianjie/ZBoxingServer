namespace ET.Server
{
    /// <summary>
    /// 离开房间Handler
    /// 玩家主动离开 → 如果房主离开则客人晋升/解散 → 广播更新
    /// </summary>
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_ZBLeaveRoomHandler : MessageSessionHandler<C2G_ZBLeaveRoom, G2C_ZBLeaveRoom>
    {
        protected override async ETTask Run(Session session, C2G_ZBLeaveRoom request, G2C_ZBLeaveRoom response)
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

            // 离开房间
            int errorCode = roomManager.LeaveRoom(
                sessionPlayer.GetPlayerId(),
                out ZBRoomComponent affectedRoom,
                out bool roomDissolved
            );

            response.ErrorCode = errorCode;

            // 如果房间还在且有剩余玩家，广播更新
            if (errorCode == ZBErrorCode.Success && !roomDissolved && affectedRoom != null && accountComponent != null)
            {
                roomManager.BroadcastRoomUpdate(affectedRoom, accountComponent);
            }

            await ETTask.CompletedTask;
        }
    }
}
