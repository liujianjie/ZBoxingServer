namespace ET.Server
{
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_ZBRegisterHandler : MessageSessionHandler<C2G_ZBRegister, G2C_ZBRegister>
    {
        protected override async ETTask Run(Session session, C2G_ZBRegister request, G2C_ZBRegister response)
        {
            Scene root = session.Root();

            // 懒初始化账户管理组件
            ZBAccountComponent accountComponent = root.GetComponent<ZBAccountComponent>();
            if (accountComponent == null)
            {
                accountComponent = root.AddComponent<ZBAccountComponent>();
            }

            // 注册
            int errorCode = accountComponent.Register(request.Username, request.Password, request.Nickname, out ZBAccountInfo account);
            if (errorCode != ZBErrorCode.Success)
            {
                response.ErrorCode = errorCode;
                return;
            }

            // 注册成功后自动登录：在Session上挂载玩家组件
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

            Log.Info($"[ZBoxing] 注册成功: {account.Nickname} (ID={account.PlayerId})");

            await ETTask.CompletedTask;
        }
    }
}
