﻿using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectSchema {
    /*
        * This namespace defines all of the in-game objects and their structure.
        * The way any class is defined here is the way it is represented in the database in a JSON structure.
        * 
        * Warning: It's important that all of the objects here are structs so shallow copy works on them.
        * Big Warning: Any changes to this file should be really carefully made. All Serialize/Deserialize have to be updated
        *              so that the Packets are coded/decoded correctly but also so that the database is forward compatible.
        * Another Warning: LiteNetLib only supports nested types that are only 1 level deep. Code: https://github.com/RevenantX/LiteNetLib/blob/master/LiteNetLib/Utils/NetSerializer.cs#L83
        * 
        * To add another struct to this file:
        *   1. Make the struct, make sure IFromJson and IObject is interfaced correctly (check type template).
        *   2. Make sure to update Deserialize and Deserialize.
        *   3. Make sure to update ConstructObject.
        *   4. Make sure to update FromJson.
    */
#if !UNITY_STANDALONE
    interface IFromJson<T> {
        // ref T obj is the object passed by reference this 'this' keyword is passed by value on structs.
        void FromJson(Newtonsoft.Json.Linq.JObject json, ref T obj);
    }
#endif

    public interface IObject {
        string id { get; set; }
        string type { get; set; }
    }

    interface ILockable {
        bool locked { get; set; }
        string lockedBy { get; set; }
        long lockStartTime { get; set; }
    }

    interface IMineable {
        string mineableType { get; set; }
        string size { get; set; }
        int remainingQuantity { get; set; }
    }

    public struct Player : Packet.ITransform, IObject, INetSerializable
#if !UNITY_STANDALONE
        , IFromJson<Player>
#endif
    {
        public string userName { get; set; }
        public int xp { get; set; }

        //IObject
        public string id { get; set; }
        public string type { get; set; }

        // Transform
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float rot_x { get; set; }
        public float rot_y { get; set; }
        public float rot_z { get; set; }
        public float rot_w { get; set; }

        public void Deserialize(NetDataReader reader) {
            userName = reader.GetString();
            xp = reader.GetInt();

            id = reader.GetString();
            type = reader.GetString();

            x = reader.GetFloat();
            y = reader.GetFloat();
            z = reader.GetFloat();
            rot_x = reader.GetFloat();
            rot_y = reader.GetFloat();
            rot_z = reader.GetFloat();
            rot_w = reader.GetFloat();
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(userName);
            writer.Put(xp);

            writer.Put(id);
            writer.Put(type);

            writer.Put(x);
            writer.Put(y);
            writer.Put(z);
            writer.Put(rot_x);
            writer.Put(rot_y);
            writer.Put(rot_z);
            writer.Put(rot_w);
        }

#if !UNITY_STANDALONE
        public void FromJson(Newtonsoft.Json.Linq.JObject json, ref Player obj) {
            obj.userName = json.Value<string>("userName");
            obj.xp = json.Value<int>("xp");
            obj.id = json.Value<string>("id");
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
        public int remainingQuantity { get; set; }

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

        public void Deserialize(NetDataReader reader) {
            id = reader.GetString();
            type = reader.GetString();
            mineableType = reader.GetString();
            subMineableType = reader.GetString();
            size = reader.GetString();
            remainingQuantity = reader.GetInt();

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

        public void Serialize(NetDataWriter writer) {
            writer.Put(id);
            writer.Put(type);
            writer.Put(mineableType);
            writer.Put(subMineableType);
            writer.Put(size);
            writer.Put(remainingQuantity);

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
        public void FromJson(Newtonsoft.Json.Linq.JObject json, ref Mineable obj) {
            obj.id = json.Value<string>("id");
            obj.type = json.Value<string>("type");
            obj.mineableType = json.Value<string>("mineableType");
            obj.subMineableType = json.Value<string>("subMineableType");
            obj.size = json.Value<string>("size");
            obj.remainingQuantity = json.Value<int>("remainingQuantity");
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

    interface IPlantable {
        long growthTime { get; set; }
        string plantableType { get; set; }
        long timePlanted { get; set; }
    }

    public struct Plantable : Packet.ITransform, IObject, IPlantable, ILockable, INetSerializable
#if !UNITY_STANDALONE
        , IFromJson<Plantable>
#endif
    {
        //IObject
        public string id { get; set; }
        public string type { get; set; }
        public long growthTime { get; set; }
        public string plantableType { get; set; }
        public long timePlanted { get; set; }

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

        public void Deserialize(NetDataReader reader) {
            id = reader.GetString();
            type = reader.GetString();
            growthTime = reader.GetLong();
            plantableType = reader.GetString();
            timePlanted = reader.GetLong();
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

        public void Serialize(NetDataWriter writer) {
            writer.Put(id);
            writer.Put(type);
            writer.Put(growthTime);
            writer.Put(plantableType);
            writer.Put(timePlanted);
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
        public void FromJson(Newtonsoft.Json.Linq.JObject json, ref Plantable obj) {
            obj.id = json.Value<string>("id");
            obj.type = json.Value<string>("type");
            obj.plantableType = json.Value<string>("plantableType");
            obj.growthTime = json.Value<long>("growthTime");
            obj.timePlanted = json.Value<long>("timePlanted");
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

    //The increment of time in ms between the different states of the plant state
    public class ObjectLifeTimes {
        public class IPlantLifeTime {
            private IPlantLifeTime(long value) { Value = value; }
            public long Value { get; set; }
            public static IPlantLifeTime WHEAT { get { return new IPlantLifeTime(10000); } }
            public static IPlantLifeTime TREE { get { return new IPlantLifeTime(40000); } }
        }
    }

    public class ObjectTypes {
        //IObject
        public class IObjectType {
            private IObjectType(string value) { Value = value; }
            public string Value { get; set; }
            public static IObjectType MINEABLE { get { return new IObjectType("MINEABLE"); } }
            public static IObjectType FISHABLE { get { return new IObjectType("FISHABLE"); } }
            public static IObjectType PLANTABLE { get { return new IObjectType("PLANTABLE"); } }
            public static IObjectType PLAYER { get { return new IObjectType("PLAYER"); } }
        }

        //Plantable
        public class IPlantableType {
            private IPlantableType(string value) { Value = value; }
            public string Value { get; set; }
            public static IPlantableType WHEAT { get { return new IPlantableType("WHEAT"); } }
            public static IPlantableType TREE { get { return new IPlantableType("TREE"); } }
        }

        //IMineable
        public class IMineableMineableType {
            private IMineableMineableType(string value) { Value = value; }
            public string Value { get; set; }
            public static IMineableMineableType ROCK { get { return new IMineableMineableType("ROCK"); } }
            public static IMineableMineableType TREE { get { return new IMineableMineableType("TREE"); } }
        }
        public class IMineableSubMineableType {
            private IMineableSubMineableType(string value) { Value = value; }
            public string Value { get; set; }
            public static IMineableSubMineableType STONE { get { return new IMineableSubMineableType("STONE"); } }
            public static IMineableSubMineableType IRON { get { return new IMineableSubMineableType("IRON"); } }
            public static IMineableSubMineableType OAK { get { return new IMineableSubMineableType("OAK"); } }
        }
        public class IMineableSize {
            private IMineableSize(string value) { Value = value; }
            public string Value { get; set; }
            public static IMineableSize SMALL { get { return new IMineableSize("SMALL"); } }
            public static IMineableSize MEDIUM { get { return new IMineableSize("MEDIUM"); } }
            public static IMineableSize LARGE { get { return new IMineableSize("LARGE"); } }
        }


#if !UNITY_STANDALONE
        // Helpers
        // Constructs the correct object from json and sets the id.
        public static IObject ConstructObject(string id, Newtonsoft.Json.Linq.JObject json) {
            string iObjectType = json.Value<string>("type");
            if (iObjectType == IObjectType.MINEABLE.Value) {
                Mineable m = new Mineable();
                m.FromJson(json, ref m);
                m.id = id;
                return m;
            } else {
                throw new ArgumentException(String.Format("Object of type {0} cannot be decoded id '{1}'.", iObjectType, id));
            }
        }
#endif
    }
}
