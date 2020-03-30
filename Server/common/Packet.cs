using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Packet {
    interface ICopyAble<T> {
        T Copy();
    }

    interface ObjectIdentifier {
        // This interface identifies an object in the world by unique id.
        string id { get; set; }  // This is a unique identifier for objects.
    }

    interface PlayerIdentifier {
        // This interface identifies a player by unique username.
        string userName { get; set; }  // This is a unique identifier for players.
    }

    interface ZoneIdentifier {
        string zone { get; set; }  // This is a unique identifier for zones.
    }

    interface ITransform {
        float x { get; set; }
        float y { get; set; }
        float z { get; set; }
        float rot_x { get; set; }
        float rot_y { get; set; }
        float rot_z { get; set; }
        float rot_w { get; set; }
    }

    interface IItem
    {
        ItemSchema.ItemDBSchema item { get; set; }
    }

    // UserInventory is only for Player.
    public class UserInventory : ICopyAble<UserInventory> {
        public ItemSchema.ItemDBSchema[] items { get; set; }

        public UserInventory Copy() {
            return (UserInventory)this.MemberwiseClone();
        }

        public override string ToString() {
            string str = "";
            foreach (var i in items) {
                str += i.uniqueName + " " + i.quantity.ToString() + "\n";
            }

            return str;
        }
    }

    // DestroyObject is only for client.
    class DestroyObject : ObjectIdentifier, ICopyAble<DestroyObject> {
        public string id { get; set; }

        public DestroyObject Copy() {
            return (DestroyObject)this.MemberwiseClone();
        }
    }

    // Start Planting is used server -> player
    class StartPlanting : ObjectIdentifier, PlayerIdentifier, ICopyAble<StartPlanting>
    {
        public string id { get; set; }
        public string userName { get; set; }

        public StartPlanting Copy()
        {
            return (StartPlanting)this.MemberwiseClone();
        }
    }

    // Mining workflow:
    // User sends StartMining, server sends to everyone else.
    // The server locks the object. If lock failed, then the server sends abort mining to the user.
    // The player sends MinedQuantity to the server, server sends to everyone else.
    // The played sends EndMining, server sends to everyone else.

    // StartMining is for both server and player.
    class StartMining : ObjectIdentifier, PlayerIdentifier, ICopyAble<StartMining> {
        public string id { get; set; }
        public string userName { get; set; }
        public string minableType { get; set; }
        public string minableSubType { get; set; }

        public StartMining Copy() {
            return (StartMining)this.MemberwiseClone();
        }
    }

    // MiningLockFailed is only for Player.
    class MiningLockFailed : ObjectIdentifier, PlayerIdentifier, ICopyAble<MiningLockFailed> {
        public string id { get; set; }
        public string userName { get; set; }

        public MiningLockFailed Copy() {
            return (MiningLockFailed)this.MemberwiseClone();
        }
    }

    // MinedQuantity is for both server and player.
    class MinedQuantity : ObjectIdentifier, PlayerIdentifier, ICopyAble<MinedQuantity>, IItem
    {
        public string id { get; set; }  // id of object being mined
        public string userName { get; set; }  // userName of user mining the object

        // What item the user received.
        // The server verifies if this value is plausible.
        public ItemSchema.ItemDBSchema item { get; set; }

        public MinedQuantity Copy()
        {
            return (MinedQuantity)this.MemberwiseClone();
        }
    }

    // EndMining is only for player.
    class EndMining : ObjectIdentifier, PlayerIdentifier, ICopyAble<EndMining>, IItem
    {
        public string id { get; set; }  // id of mineable object.
        public string userName { get; set; }  // userName of player who was mining.

        // How much bonus material the user gets when successfully mines an object. 
        // Server verifies this value as well.
        public ItemSchema.ItemDBSchema item { get; set; }

        public EndMining Copy() {
            return (EndMining)this.MemberwiseClone();
        }
    }

    // Abort mining is for server and player.
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

    // Login is only for server.
    class Login : PlayerIdentifier {
        public string userName { get; set; }
    }

    // PlayerExited is only for client. (Server uses OnPeerDisconnected LiteNetLib event.)
    class PlayerExited : PlayerIdentifier {
        public string userName { get; set; }
    }

    // PlayerTransform is for both client and server.
    class PlayerTransform : ITransform, PlayerIdentifier, ICopyAble<PlayerTransform> {
        public string userName { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float rot_x { get; set; }
        public float rot_y { get; set; }
        public float rot_z { get; set; }
        public float rot_w { get; set; }

        public PlayerTransform Copy() {
            return (PlayerTransform)this.MemberwiseClone();
        }
    }

    // ObjectTransform is for both client and server.
    class ObjectTransform : ITransform, ObjectIdentifier, ICopyAble<ObjectTransform> {
        public string id { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float rot_x { get; set; }
        public float rot_y { get; set; }
        public float rot_z { get; set; }
        public float rot_w { get; set; }

        public ObjectTransform Copy() {
            return (ObjectTransform)this.MemberwiseClone();
        }
    }

    // PlaceMinableObject is only for client.
    class CurrentServerTime : ICopyAble<CurrentServerTime> {
        public long time { get; set; }

        public CurrentServerTime Copy() {
            return (CurrentServerTime)this.MemberwiseClone();
        }
    }


    // PlaceMinableObject is only for client.
    class PlaceMinableObject : ICopyAble<PlaceMinableObject> {
        public ObjectSchema.Mineable mineable { get; set; }

        public PlaceMinableObject Copy() {
            return (PlaceMinableObject)this.MemberwiseClone();
        }
    }

    // PlacePlantableObject is only for client
    class PlacePlantableObject : PlayerIdentifier, ICopyAble<PlacePlantableObject> {
        public string userName { get; set; }
        public ObjectSchema.Plantable plantable { get; set; }

        public PlacePlantableObject Copy() {
            return (PlacePlantableObject)this.MemberwiseClone();
        }
    }

    /*
     * Fishing sequence:
     *    FishThrowBobbler -> Player to Server and server relays to all other players (this is just for animation).
     *    FishBobblerInWater -> Player to Server.
     *    FishBiting -> Server to all players.
     *    FishCaught -> Player to Server indicating whether they caught successfully and then Server to all Player (Server fills in what kind of fish was caught).
     *    If fish was caught: UserInventory -> Server to Player.
     */

    // FishThrowBobbler is for both server and players.
    class FishThrowBobbler : PlayerIdentifier, ITransform, ICopyAble<FishThrowBobbler> {
        public string userName { get; set; }

        // Bobbler position
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float rot_x { get; set; }
        public float rot_y { get; set; }
        public float rot_z { get; set; }
        public float rot_w { get; set; }

        public FishThrowBobbler Copy() {
            return (FishThrowBobbler)this.MemberwiseClone();
        }
    }

    // BobblerInWater is only for server.
    class FishBobblerInWater : ZoneIdentifier, PlayerIdentifier, ICopyAble<FishBobblerInWater> {
        public string zone { get; set; }
        public string userName { get; set; }

        public FishBobblerInWater Copy() {
            return (FishBobblerInWater)this.MemberwiseClone();
        }
    }

    class FishBiting : PlayerIdentifier, ICopyAble<FishBiting> {
        public string userName { get; set; }

        public FishBiting Copy() {
            return (FishBiting)this.MemberwiseClone();
        }
    }

    public class FishCaught : PlayerIdentifier, ICopyAble<FishCaught> {
        public string userName { get; set; }

        public bool success { get; set; }
        public ItemSchema.ItemDBSchema fishItem { get; set; }

        public FishCaught Copy() {
            return (FishCaught)this.MemberwiseClone();
        }
    }

    class AbortFishing : PlayerIdentifier, ICopyAble<AbortFishing> {
        public string userName { get; set; }
        public AbortFishing Copy() {
            return (AbortFishing)this.MemberwiseClone();
        }
    }

    namespace Developer {
        class DeveloperPlaceMinableObject : ICopyAble<DeveloperPlaceMinableObject> {
            public ObjectSchema.Mineable mineable { get; set; }

            public DeveloperPlaceMinableObject Copy() {
                return (DeveloperPlaceMinableObject)this.MemberwiseClone();
            }
        }
    }
}
