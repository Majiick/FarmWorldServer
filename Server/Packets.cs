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

    class DestroyObject : Transform, ObjectIdentifier, ICopyAble<DestroyObject>
    {
        public string id { get; set; }

        public DestroyObject Copy()
        {
            return (DestroyObject)this.MemberwiseClone();
        }
    }

    abstract class Transform
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float rot_x { get; set; }
        public float rot_y { get; set; }
        public float rot_z { get; set; }
        public float rot_w { get; set; }
    }

    // StartMining is only for server.
    class StartMining : ObjectIdentifier, ICopyAble<DestroyObject>
    {
        public string id { get; set; }

        public DestroyObject Copy()
        {
            return (DestroyObject)this.MemberwiseClone();
        }
    }

    // EndMining is only for player.
    class EndMining : ObjectIdentifier, ICopyAble<DestroyObject>
    {
        public string id { get; set; }

        public DestroyObject Copy()
        {
            return (DestroyObject)this.MemberwiseClone();
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
    class PlayerTransform : Transform, PlayerIdentifier, ICopyAble<PlayerTransform>
    {
        public string userName { get; set; }

        public PlayerTransform Copy()
        {
            return (PlayerTransform)this.MemberwiseClone();
        }
    }

    // ObjectTransform is for both client and server.
    class ObjectTransform : Transform, ObjectIdentifier, ICopyAble<ObjectTransform>
    {
        public string id { get; set; }

        public ObjectTransform Copy()
        {
            return (ObjectTransform)this.MemberwiseClone();
        }
    }
}
