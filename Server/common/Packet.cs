using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Packet
{
    interface ICopyAble<T>
    {
        T Copy();
    }

    interface ObjectIdentifier
    {
        // This interface identifies an object in the world by unique id.
        string id { get; set; }  // This is a unique identifier for objects.
    }

    interface PlayerIdentifier
    {
        // This interface identifies a player by unique username.
        string userName { get; set; }  // This is a unique identifier for players.
    }

    interface ITransform
    {
        float x { get; set; }
        float y { get; set; }
        float z { get; set; }
        float rot_x { get; set; }
        float rot_y { get; set; }
        float rot_z { get; set; }
        float rot_w { get; set; }
    }

    // UserInventory is only for Player.
    class UserInventory : ICopyAble<UserInventory>
    {
        public ItemSchema.ItemDBSchema[] items { get; set; }

        public UserInventory Copy()
        {
            return (UserInventory)this.MemberwiseClone();
        }
    }

    // DestroyObject is only for client.
    class DestroyObject : ObjectIdentifier, ICopyAble<DestroyObject>
    {
        public string id { get; set; }

        public DestroyObject Copy()
        {
            return (DestroyObject)this.MemberwiseClone();
        }
    }

    // StartMining is for both server and player.
    class StartMining : ObjectIdentifier, PlayerIdentifier, ICopyAble<StartMining>
    {
        public string id { get; set; }
        public string userName { get; set; }

        public StartMining Copy()
        {
            return (StartMining)this.MemberwiseClone();
        }
    }

    // MiningLockFailed is only for Player.
    class MiningLockFailed : ObjectIdentifier, PlayerIdentifier, ICopyAble<MiningLockFailed>
    {
        public string id { get; set; }
        public string userName { get; set; }

        public MiningLockFailed Copy()
        {
            return (MiningLockFailed)this.MemberwiseClone();
        }
    }

    // Abort minind is for server and player.
    // Abort mining is sent to server when a player interrupts mining.
    // It is also relayed from server to clients.
    class AbortMining : ObjectIdentifier, PlayerIdentifier, ICopyAble<AbortMining> 
    {
        public string id { get; set; }  // id of mineable object.
        public string userName { get; set; }  // userName of player who was mining.

        public AbortMining Copy()
        {
            return (AbortMining)this.MemberwiseClone();
        }
    }

    // EndMining is only for player.
    class EndMining : ObjectIdentifier, PlayerIdentifier, ICopyAble<EndMining>
    {
        public string id { get; set; }  // id of mineable object.
        public string userName { get; set; }  // userName of player who was mining.

        public EndMining Copy()
        {
            return (EndMining)this.MemberwiseClone();
        }
    }

    // Login is only for server.
    class Login : PlayerIdentifier
    {
        public string userName { get; set; }
    }

    // PlayerExited is only for client. (Server uses OnPeerDisconnected LiteNetLib event.)
    class PlayerExited : PlayerIdentifier
    {
        public string userName { get; set; }
    }

    // PlayerTransform is for both client and server.
    class PlayerTransform : ITransform, PlayerIdentifier, ICopyAble<PlayerTransform>
    {
        public string userName { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float rot_x { get; set; }
        public float rot_y { get; set; }
        public float rot_z { get; set; }
        public float rot_w { get; set; }

        public PlayerTransform Copy()
        {
            return (PlayerTransform)this.MemberwiseClone();
        }
    }

    // ObjectTransform is for both client and server.
    class ObjectTransform : ITransform, ObjectIdentifier, ICopyAble<ObjectTransform>
    {
        public string id { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float rot_x { get; set; }
        public float rot_y { get; set; }
        public float rot_z { get; set; }
        public float rot_w { get; set; }

        public ObjectTransform Copy()
        {
            return (ObjectTransform)this.MemberwiseClone();
        }
    }

    // PlaceMinableObject is only for client.
    class PlaceMinableObject : ICopyAble<PlaceMinableObject>
    {
        public ObjectSchema.Mineable mineable { get; set; }

        public PlaceMinableObject Copy()
        {
            return (PlaceMinableObject)this.MemberwiseClone();
        }
    }

    namespace Developer
    {
        class DeveloperPlaceMinableObject : ICopyAble<DeveloperPlaceMinableObject>
        {
            public ObjectSchema.Mineable mineable { get; set; }

            public DeveloperPlaceMinableObject Copy()
            {
                return (DeveloperPlaceMinableObject)this.MemberwiseClone();
            }
        }
    }
}
