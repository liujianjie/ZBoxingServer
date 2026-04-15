namespace ET.Server
{
    /// <summary>
    /// 战斗输入Handler
    /// 客户端每帧发送输入 → 服务端验证后存入该玩家的输入缓冲区
    /// 单向消息（ISessionMessage），无需响应
    /// </summary>
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_ZBBattleInputHandler : MessageSessionHandler<C2G_ZBBattleInput>
    {
        protected override async ETTask Run(Session session, C2G_ZBBattleInput message)
        {
            // 1. 获取Session上的玩家信息
            ZBSessionPlayerComponent sessionPlayer = session.GetComponent<ZBSessionPlayerComponent>();
            if (sessionPlayer == null)
            {
                return;
            }

            long playerId = sessionPlayer.GetPlayerId();

            // 2. 获取战斗管理器
            Scene root = session.Root();
            ZBBattleComponent battleComponent = root.GetComponent<ZBBattleComponent>();
            if (battleComponent == null)
            {
                return;
            }

            // 3. 查找玩家所在战斗
            ZBBattleRoom battle = battleComponent.GetBattleByPlayerId(playerId);
            if (battle == null)
            {
                return;
            }

            // 4. 只在战斗阶段接受输入（倒计时和已结束时忽略）
            if (!battle.IsAcceptingInput())
            {
                return;
            }

            // 5. 查找该玩家在战斗中的数据
            ZBBattlePlayer battlePlayer = battle.GetPlayer(playerId);
            if (battlePlayer == null)
            {
                return;
            }

            // ============================================================
            // 输入合法性验证
            // ============================================================

            // 6. MoveDir 范围验证：只接受 -1, 0, 1
            int moveDir = message.MoveDir;
            if (moveDir < -1 || moveDir > 1)
            {
                moveDir = 0;
            }

            // 7. Action 范围验证：只接受 0~6（ZBInputAction枚举范围）
            int action = message.Action;
            if (action < ZBInputAction.None || action > ZBInputAction.Dodge)
            {
                action = ZBInputAction.None;
            }

            // 8. 帧号合理性验证
            //    客户端帧号不能超过服务端太多（防止预测作弊）
            //    也不能落后太多（过期输入没意义）
            if (!battle.IsFrameInTolerance(message.Frame))
            {
                return;
            }

            // 9. 缓冲区上限防DoS
            if (battlePlayer.InputBuffer.Count >= ZBBattleConst.InputBufferMax)
            {
                return;
            }

            // 10. 将验证后的输入存入缓冲区（游戏循环消费）
            battlePlayer.InputBuffer.Add(new ZBInputFrame
            {
                Frame = message.Frame,
                MoveDir = moveDir,
                Action = action,
            });

            await ETTask.CompletedTask;
        }
    }
}
