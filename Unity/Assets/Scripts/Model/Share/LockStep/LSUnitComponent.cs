using ProtoBuf;

namespace ET
{
	[ComponentOf(typeof(LSWorld))]
	[ProtoContract]
	public partial class LSUnitComponent: LSEntity, IAwake, ISerializeToEntity
	{
	}
}
