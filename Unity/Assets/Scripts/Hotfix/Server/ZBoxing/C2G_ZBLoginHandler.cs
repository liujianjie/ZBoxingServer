namespace ET.Server
{
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_ZBLoginHandler : MessageSessionHandler<C2G_ZBLogin, G2C_ZBLogin>
    {
        protected override async ETTask Run(Session session, C2G_ZBLogin request, G2C_ZBLogin response)
        {
            Scene root = session.Root();

            // 懒初始化账户管理组件
            ZBAccountComponent accountComponent = root.GetComponent<ZBAccountComponent>();
            if (accountComponent == null)
            {
                accountComponent = root.AddComponent<ZBAccountComponent>();
            }

            // 验证登录
            int errorCode = accountComponent.Login(request.Username, request.Password, out ZBAccountInfo account);
            if (errorCode != ZBErrorCode.Success)
            {
                response.ErrorCode = errorCode;
                return;
            }

            // 检查是否已在其他Session登录，踢掉旧Session
            if (accountComponent.IsOnline(account.PlayerId))
            {
                long oldSessionId = accountComponent.GetOnlineSessionId(account.PlayerId);
                // 通知旧Session被踢
                // 注: 暂不实现踢人推送，后续Phase补充
                accountComponent.SetOffline(account.PlayerId);
                Log.Warning($"[ZBoxing] 重复登录，踢掉旧Session: PlayerId={account.PlayerId}");
            }

            // 在Session上挂载玩家组件
            ZBSessionPlayerComponent sessionPlayer = session.GetComponent<ZBSessionPlayerComponent>();
            if (sessionPlayer == null)
            {
                sessionPlayer = session.AddComponent<ZBSessionPlayerComponent>();
            }
            sessionPlayer.SetPlayer(account.PlayerId, account.Username, account.Nickname);

            // 标记在线
            accountComponent.SetOnline(account.PlayerId, session.InstanceId);

            // 填充响应
            response.ErrorCode = ZBErrorCode.Success;
            response.Player = accountComponent.ToPlayerBrief(account);

            Log.Info($"[ZBoxing] 登录成功: {account.Nickname} (ID={account.PlayerId})");

            await ETTask.CompletedTask;
        }
    }
}
