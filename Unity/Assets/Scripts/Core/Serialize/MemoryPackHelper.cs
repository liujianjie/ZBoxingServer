// 序列化辅助类，内部实现从MemoryPack改为protobuf-net，保留类名避免修改调用方
using System;
using System.ComponentModel;
using System.IO;
using ProtoBuf;

namespace ET
{
    public static class MemoryPackHelper
    {
        public static byte[] Serialize(object message)
        {
            if (message is ISupportInitialize supportInitialize)
            {
                supportInitialize.BeginInit();
            }
            using var ms = new MemoryStream();
            Serializer.NonGeneric.Serialize(ms, message);
            return ms.ToArray();
        }

        public static void Serialize(object message, MemoryBuffer stream)
        {
            if (message is ISupportInitialize supportInitialize)
            {
                supportInitialize.BeginInit();
            }
            Serializer.NonGeneric.Serialize(stream, message);
        }

        public static object Deserialize(Type type, byte[] bytes, int index, int count)
        {
            using var ms = new MemoryStream(bytes, index, count);
            object o = Serializer.NonGeneric.Deserialize(type, ms);
            if (o is ISupportInitialize supportInitialize)
            {
                supportInitialize.EndInit();
            }
            return o;
        }

        // 使用Merge填充已有对象，支持ET的对象池机制
        public static object Deserialize(Type type, byte[] bytes, int index, int count, ref object o)
        {
            using var ms = new MemoryStream(bytes, index, count);
            o = Serializer.NonGeneric.Merge(ms, o);
            if (o is ISupportInitialize supportInitialize)
            {
                supportInitialize.EndInit();
            }
            return o;
        }

        public static object Deserialize(Type type, MemoryBuffer stream)
        {
            object o = Serializer.NonGeneric.Deserialize(type, stream);
            if (o is ISupportInitialize supportInitialize)
            {
                supportInitialize.EndInit();
            }
            return o;
        }

        // 使用Merge填充已有对象，支持ET的对象池机制
        public static object Deserialize(Type type, MemoryBuffer stream, ref object o)
        {
            o = Serializer.NonGeneric.Merge(stream, o);
            if (o is ISupportInitialize supportInitialize)
            {
                supportInitialize.EndInit();
            }
            return o;
        }
    }
}
