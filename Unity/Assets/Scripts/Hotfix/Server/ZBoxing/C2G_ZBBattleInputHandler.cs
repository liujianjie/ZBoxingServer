namespace ET.Server
{
    /// <summary>
    /// 战斗输入Handler
    /// 客户端每帧发送输入 → 服务端存入该玩家的输入缓冲区
    /// 单向消息（ISessionMessage），无需响应
    /// </summary>
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_ZBBattleInputHandler : MessageSessionHandler<C2G_ZBBattleInput>
    {
        protected override async ETTask Run(Session session, C2G_ZBBattleInput message)
        {
            // 获取Session上的玩家信息
            ZBSessionPlayerComponent sessionPlayer = session.GetComponent<ZBSessionPlayerComponent>();
            if (sessionPlayer == null)
            {
                return;
            }

            long playerId = sessionPlayer.GetPlayerId();

            // 获取战斗管理器
            Scene root = session.Root();
            ZBBattleComponent battleComponent = root.GetComponent<ZBBattleComponent>();
            if (battleComponent == null)
            {
                return;
            }

            // 查找玩家所在战斗
            ZBBattleRoom battle = battleComponent.GetBattleByPlayerId(playerId);
            if (battle == null)
            {
                return;
            }

            // 只在战斗阶段接受输入（倒计时和已结束时忽略）
            if (!battle.IsAcceptingInput())
            {
                return;
            }

            // 查找该玩家在战斗中的数据
            ZBBattlePlayer battlePlayer = battle.GetPlayer(playerId);
            if (battlePlayer == null)
            {
                return;
            }

            // 限制缓冲区大小防止恶意客户端灌满内存
            if (battlePlayer.InputBuffer.Count >= ZBBattleConst.InputBufferMax)
            {
                return;
            }

            // 将输入存入缓冲区（游戏循环E.2会消费）
            battlePlayer.InputBuffer.Add(new ZBInputFrame
            {
                Frame = message.Frame,
                MoveDir = message.MoveDir,
                Action = message.Action,
            });

            await ETTask.CompletedTask;
        }
    }
}
