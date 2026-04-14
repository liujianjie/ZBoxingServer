namespace ET.Server
{
    /// <summary>
    /// 房间列表Handler
    /// 返回当前所有等待中/已满的房间列表
    /// </summary>
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_ZBRoomListHandler : MessageSessionHandler<C2G_ZBRoomList, G2C_ZBRoomList>
    {
        protected override async ETTask Run(Session session, C2G_ZBRoomList request, G2C_ZBRoomList response)
        {
            ZBSessionPlayerComponent sessionPlayer = session.GetComponent<ZBSessionPlayerComponent>();
            if (sessionPlayer == null)
            {
                // 未登录，返回空列表
                return;
            }

            Scene root = session.Root();

            ZBRoomManagerComponent roomManager = root.GetComponent<ZBRoomManagerComponent>();
            if (roomManager == null)
            {
                // 还没人创建过房间，返回空列表
                return;
            }

            ZBAccountComponent accountComponent = root.GetComponent<ZBAccountComponent>();
            if (accountComponent == null)
            {
                return;
            }

            // 获取房间列表并转换为协议消息
            var rooms = roomManager.GetRoomList();
            foreach (var room in rooms)
            {
                response.Rooms.Add(roomManager.ToRoomInfo(room, accountComponent));
            }

            await ETTask.CompletedTask;
        }
    }
}
