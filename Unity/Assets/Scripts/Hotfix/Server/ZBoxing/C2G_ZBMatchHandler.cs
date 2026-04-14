namespace ET.Server
{
    /// <summary>
    /// 匹配Handler
    /// 客户端发送匹配请求（Cancel=false开始匹配, Cancel=true取消匹配）
    /// 加入队列后立即尝试撮合
    /// </summary>
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_ZBMatchHandler : MessageSessionHandler<C2G_ZBMatch, G2C_ZBMatch>
    {
        protected override async ETTask Run(Session session, C2G_ZBMatch request, G2C_ZBMatch response)
        {
            ZBSessionPlayerComponent sessionPlayer = session.GetComponent<ZBSessionPlayerComponent>();
            if (sessionPlayer == null)
            {
                response.ErrorCode = ZBErrorCode.NotLoggedIn;
                return;
            }

            Scene root = session.Root();

            // 获取或创建匹配队列组件
            ZBMatchQueueComponent matchQueue = root.GetComponent<ZBMatchQueueComponent>();
            if (matchQueue == null)
            {
                matchQueue = root.AddComponent<ZBMatchQueueComponent>();
            }

            long playerId = sessionPlayer.GetPlayerId();
            string nickname = sessionPlayer.GetNickname();

            if (request.Cancel)
            {
                // 取消匹配
                int err = matchQueue.Dequeue(playerId);
                response.ErrorCode = err;
                response.InQueue = false;
            }
            else
            {
                // 开始匹配
                int err = matchQueue.Enqueue(playerId, nickname, session);
                response.ErrorCode = err;
                response.InQueue = err == ZBErrorCode.Success;

                // 尝试撮合
                if (err == ZBErrorCode.Success)
                {
                    matchQueue.TryMatch();
                }
            }

            await ETTask.CompletedTask;
        }
    }
}
