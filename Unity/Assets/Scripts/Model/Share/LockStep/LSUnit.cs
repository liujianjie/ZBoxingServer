using System;
using ProtoBuf;
using MongoDB.Bson.Serialization.Attributes;
using TrueSync;

namespace ET
{
    [ChildOf(typeof(LSUnitComponent))]
    [ProtoContract]
    public partial class LSUnit: LSEntity, IAwake, ISerializeToEntity
    {
        public TSVector Position
        {
            get;
            set;
        }

        [ProtoIgnore]
        [BsonIgnore]
        public TSVector Forward
        {
            get => this.Rotation * TSVector.forward;
            set => this.Rotation = TSQuaternion.LookRotation(value, TSVector.up);
        }

        public TSQuaternion Rotation
        {
            get;
            set;
        }
    }
}
