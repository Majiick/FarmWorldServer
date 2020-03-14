using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectSchema
{
    /*
        * This namespace defines all of the in-game objects and their structure.
        * The way any class is defined here is the way it is represented in the database in a JSON structure.
        * 
        * Warning: It's important that all of the objects here are structs so shallow copy works on them.
        * Big Warning: Any changes to this file should be really carefully made. All Serialize/Deserialize have to be updated
        *              so that the Packets are coded/decoded correctly but also so that the database is forward compatible.
        * Another Warning: LiteNetLib only supports nested types that are only 1 level deep. Code: https://github.com/RevenantX/LiteNetLib/blob/master/LiteNetLib/Utils/NetSerializer.cs#L83
    */
#if !UNITY_STANDALONE
    interface IFromJson<T>
    {
        // ref T obj is the object passed by reference this 'this' keyword is passed by value on structs.
        void FromJson(Newtonsoft.Json.Linq.JObject json, ref T obj);
    }
#endif

    interface IObject
    {
        string id { get; set; }
        string type { get; set; }
    }

    interface ILockable
    {
        bool locked { get; set; }
        string lockedBy { get; set; }
        long lockStartTime { get; set; }
    }

    interface IMineable
    {
        string mineableType { get; set; }
        string size { get; set; }
    }

    public struct Mineable : Packet.ITransform, IObject, IMineable, ILockable, INetSerializable
#if !UNITY_STANDALONE
        , IFromJson<Mineable>
#endif
    {
        //IObject
        public string id { get; set; }
        public string type { get; set; }

        //Mineable
        public string mineableType { get; set; }
        public string subMineableType { get; set; }
        public string size { get; set; }

        // Transform
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float rot_x { get; set; }
        public float rot_y { get; set; }
        public float rot_z { get; set; }
        public float rot_w { get; set; }

        // Lock
        public bool locked { get; set; }
        public string lockedBy { get; set; }
        public long lockStartTime { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            id = reader.GetString();
            type = reader.GetString();
            mineableType = reader.GetString();
            subMineableType = reader.GetString();
            size = reader.GetString();

            x = reader.GetFloat();
            y = reader.GetFloat();
            z = reader.GetFloat();
            rot_x = reader.GetFloat();
            rot_y = reader.GetFloat();
            rot_z = reader.GetFloat();
            rot_w = reader.GetFloat();

            locked = reader.GetBool();
            lockedBy = reader.GetString();
            lockStartTime = reader.GetLong();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(id);
            writer.Put(type);
            writer.Put(mineableType);
            writer.Put(subMineableType);
            writer.Put(size);

            writer.Put(x);
            writer.Put(y);
            writer.Put(z);
            writer.Put(rot_x);
            writer.Put(rot_y);
            writer.Put(rot_z);
            writer.Put(rot_w);

            writer.Put(locked);
            writer.Put(lockedBy);
            writer.Put(lockStartTime);
        }

#if !UNITY_STANDALONE
        public void FromJson(Newtonsoft.Json.Linq.JObject json, ref Mineable obj)
        {
            obj.id = json.Value<string>("id");
            obj.type = json.Value<string>("type");
            obj.mineableType = json.Value<string>("mineableType");
            obj.subMineableType = json.Value<string>("subMineableType");
            obj.size = json.Value<string>("size");
            obj.x = json.Value<float>("x");
            obj.y = json.Value<float>("y");
            obj.z = json.Value<float>("z");
            obj.rot_x = json.Value<float>("rot_x");
            obj.rot_y = json.Value<float>("rot_y");
            obj.rot_z = json.Value<float>("rot_z");
            obj.rot_w = json.Value<float>("rot_w");
        }
#endif
    }

    class ObjectTypes
    {
        //IObject
        public class IObjectType
        {
            private IObjectType(string value) { Value = value; }
            public string Value { get; set; }
            public static IObjectType MINEABLE { get { return new IObjectType("MINEABLE"); } }
        }

        //IMineable
        public class IMineableMineableType
        {
            private IMineableMineableType(string value) { Value = value; }
            public string Value { get; set; }
            public static IMineableMineableType ROCK { get { return new IMineableMineableType("ROCK"); } }
            public static IMineableMineableType TREE { get { return new IMineableMineableType("TREE"); } }
        }
        public class IMineableSubMineableType
        {
            private IMineableSubMineableType(string value) { Value = value; }
            public string Value { get; set; }
            public static IMineableSubMineableType STONE { get { return new IMineableSubMineableType("STONE"); } }
            public static IMineableSubMineableType IRON { get { return new IMineableSubMineableType("IRON"); } }
        }
        public class IMineableSize
        {
            private IMineableSize(string value) { Value = value; }
            public string Value { get; set; }
            public static IMineableSize SMALL { get { return new IMineableSize("SMALL"); } }
            public static IMineableSize MEDIUM { get { return new IMineableSize("MEDIUM"); } }
            public static IMineableSize LARGE { get { return new IMineableSize("LARGE"); } }
        }

        //Mineable
        public static readonly string MineableBaseID = "MINEABLE";
    }
}
