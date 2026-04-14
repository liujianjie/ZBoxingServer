namespace ET.Server
{
    /// <summary>
    /// ZBoxing Session玩家组件，挂载在Session上
    /// 记录该Session对应的已登录玩家信息
    /// </summary>
    [ComponentOf(typeof(Session))]
    public class ZBSessionPlayerComponent : Entity, IAwake, IDestroy
    {
        public long PlayerId;
        public string Username;
        public string Nickname;
    }
}
