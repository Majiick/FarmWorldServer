using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Diagnostics;

namespace Server
{
    class GameState : NetPacketProcessor
    {
        Dictionary<string, PlayerState> _connectedPlayers = new Dictionary<string, PlayerState>();
        List<DelayedEvent> _delayedEvents = new List<DelayedEvent>();
        public long TickStartTime { get; set; }  // Gets set by the Server at the start of the tick.
        Database _db;

        public GameState(Database db)
        {
            _db = db;

            RegisterNestedType<ObjectSchema.Mineable>();
            RegisterNestedType<ItemSchema.ItemDBSchema>();

            SubscribeReusable<Packet.PlayerTransform, LiteNetLib.NetPeer>(OnPlayerTransformReceived);
            SubscribeReusable<Packet.Login, LiteNetLib.NetPeer>(OnLoginReceived);
            SubscribeReusable<Packet.StartMining, LiteNetLib.NetPeer>(OnStartMiningPacketReceived);
            SubscribeReusable<Packet.AbortMining, LiteNetLib.NetPeer>(OnAbortMiningPacketReceived);
            SubscribeReusable<Packet.FishThrowBobbler, LiteNetLib.NetPeer>(OnFishThrowBobblerPacketReceived);
            SubscribeReusable<Packet.FishBobblerInWater, LiteNetLib.NetPeer>(OnFishBobblerInWaterPacketReceived);
            SubscribeReusable<Packet.FishCaught, LiteNetLib.NetPeer>(OnFishCaughtPacketReceived);
            SubscribeReusable<Packet.AbortFishing, LiteNetLib.NetPeer>(OnAbortFishingPacketReceived);
            SubscribeReusable<Packet.Developer.DeveloperPlaceMinableObject, LiteNetLib.NetPeer>(OnPlaceMinableObjectPacketReceived);
        }

        public void Tick()
        {
            foreach (PlayerState player in _connectedPlayers.Values)
            {
                SendToAllOtherPlayers(player, this.Write(player.lastTransform));
            }

            ExecuteDelayedEvents();
        }

        void AddDelayedEvent(Action action, long executionDelay)
        {
            _delayedEvents.Add(new DelayedEvent(action, executeAt: TickStartTime + executionDelay));
        }

        void AddDelayedEvent(DelayedEvent e, long executionDelay)
        {
            e.ExecuteAt = TickStartTime + executionDelay;
            _delayedEvents.Add(e);
        }

        void ExecuteDelayedEvents()
        {
            List<DelayedEvent> notExecuted = new List<DelayedEvent>(_delayedEvents.Count);
            foreach (DelayedEvent e in _delayedEvents)
            {
                if (TickStartTime >= e.ExecuteAt && !e.Cancelled)
                {
                    e.Action();
                } else
                {
                    if (!e.Cancelled)
                    {
                        notExecuted.Add(e);
                    }
                }
            }

            _delayedEvents = notExecuted;  // Filter out all of the executed events and cancelled events.
        }

        void SendToAllOtherPlayers(PlayerState excluded, byte[] bytes)
        {
            foreach (PlayerState player in _connectedPlayers.Values)
            {
                if (player == excluded) continue;
                player._netPeer.Send(bytes, DeliveryMethod.ReliableSequenced);
            }
        }

        void SendToAllOtherPlayers(string excludedUsername, byte[] bytes)
        {
            PlayerState p;
            if(!_connectedPlayers.TryGetValue(excludedUsername, out p))
            {
                throw new ArgumentException(String.Format("SendToAllOtherPlayers tried to exclude userName {0} but it is not connected.", excludedUsername));
            }
            SendToAllOtherPlayers(p, bytes);
        }

        void SendToAllPlayers(byte[] bytes)
        {
            foreach (PlayerState player in _connectedPlayers.Values)
            {
                player._netPeer.Send(bytes, DeliveryMethod.ReliableSequenced);
            }
        }

        void SendInventoryToPlayer(PlayerState p)
        {
            List<ItemSchema.ItemDBSchema> inventory = _db.GetUserInventory(p._userName);
            Packet.UserInventory packet = new Packet.UserInventory();
            packet.items = inventory.ToArray();
            p._netPeer.Send(this.Write(packet), DeliveryMethod.ReliableOrdered);
        }

        void OnFishThrowBobblerPacketReceived(Packet.FishThrowBobbler fw, NetPeer peer)
        {
            var fwCopy = fw.Copy();
            try
            {
                ValidatePacket(fwCopy);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(String.Format("Failed to validate packet in OnFishThrowBobblerPacketReceived from {0}: {1}", peer.EndPoint, ex.ToString()));
                return;
            }
            var p = _connectedPlayers[fwCopy.userName];
            p.ThrowBobbler();

            SendToAllOtherPlayers(fwCopy.userName, this.Write<Packet.FishThrowBobbler>(fwCopy));
        }

        void OnFishBobblerInWaterPacketReceived(Packet.FishBobblerInWater fw, NetPeer peer)
        {
            var fwCopy = fw.Copy();
            try
            {
                ValidatePacket(fwCopy);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(String.Format("Failed to validate packet in OnFishBobblerInWaterPacketReceived from {0}: {1}", peer.EndPoint, ex.ToString()));
                return;
            }
            var p = _connectedPlayers[fwCopy.userName];

            DelayedEvent fishBitingEvent = new DelayedEvent(() =>  // This delayed event is cancelled if AbortFishing is called.
            {
                p.FishBiting();
                SendToAllPlayers(this.Write(new Packet.FishBiting { userName = p._userName}));
            });

            p.BobblerInWater(fishBitingEvent, ZonesSchema.ZoneHelper.GetZone(fwCopy.zone));
            Random rnd = new Random();
            int waitSeconds = rnd.Next(3, 15);
            AddDelayedEvent(fishBitingEvent, waitSeconds * Time.SECOND);
        } 

        void OnFishCaughtPacketReceived(Packet.FishCaught fw, NetPeer peer)
        {
            var fwCopy = fw.Copy();
            try
            {
                ValidatePacket(fwCopy);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(String.Format("Failed to validate packet in OnFishSuccessfulCatchPacketReceived from {0}: {1}", peer.EndPoint, ex.ToString()));
                return;
            }
            var p = _connectedPlayers[fwCopy.userName];
            if (!fwCopy.success)
            {
                p.FishUnsuccessfulCatch();
                SendToAllPlayers(this.Write(new Packet.FishCaught { userName = p._userName, success = false }));
                return;
            }

            p.FishSuccessfulCatch();
            var item = new ItemSchema.ItemDBSchema();
            item.uniqueName = ItemSchema.ItemNames.Herring.Value;
            item.userName = p._userName;
            item.quantity = 1;
            _db.AddToUserInventory(item);
            SendToAllPlayers(this.Write(new Packet.FishCaught { userName = p._userName, success = false, fishItem = item }));
            SendInventoryToPlayer(p);
        }

        void OnAbortFishingPacketReceived(Packet.AbortFishing fw, NetPeer peer)
        {
            var fwCopy = fw.Copy();
            try
            {
                ValidatePacket(fwCopy);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(String.Format("Failed to validate packet in OnFishSuccessfulCatchPacketReceived from {0}: {1}", peer.EndPoint, ex.ToString()));
                return;
            }
            var p = _connectedPlayers[fwCopy.userName];
            p.AbortFishing();
            SendToAllOtherPlayers(p, this.Write(fwCopy));
        }

        void OnPlaceMinableObjectPacketReceived(Packet.Developer.DeveloperPlaceMinableObject obj, NetPeer peer)
        {
            var id = _db.Write(obj.mineable);
            ObjectSchema.Mineable m = _db.Read<ObjectSchema.Mineable>(id);
            SendToAllPlayers(this.Write(new Packet.PlaceMinableObject { mineable = m }));
        }

        void OnStartMiningPacketReceived(Packet.StartMining sm, NetPeer peer)
        {
            try
            {
                ValidatePacket(sm);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(String.Format("Failed to validate packet in OnStartMiningPacketReceived from {0}: {1}", peer.EndPoint, ex.ToString()));
                return;
            }

            Packet.StartMining smCopy = sm.Copy();
            var p = _connectedPlayers[smCopy.userName];
            if (p.IsMining())
            {
                Console.WriteLine(
                    String.Format("Player {0} sent StartMining but was already mining {1}. Resetting player state.",
                                  smCopy.userName, p.miningState.miningId));
                _db.Unlock(p.miningState.miningId);
                p.miningState.miningEndEventRef.Cancelled = true;
                p.ResetState();
            }

            if (!_db.Lock(smCopy.id, smCopy.userName)) // Lock object
            {
                Console.WriteLine(String.Format("Player {0} failed to lock object {1}", smCopy.userName, smCopy.id));
                peer.Send(this.Write(new Packet.MiningLockFailed { id = smCopy.id, userName = smCopy.userName}), DeliveryMethod.ReliableOrdered);
                return;
            }

            DelayedEvent finishMiningEvent = new DelayedEvent(() =>  // This delayed event is cancelled if AbortMining is called.
            {
                if (!p.IsMining(smCopy.id))
                {
                    Console.WriteLine(String.Format("Player {0}, mineable {1} finishMiningEvent was executed but the player was not mining. " +
                        "This should not happen as the event should have been cancelled.", smCopy.userName, smCopy.id));
                    return;
                }
                p.EndMining(smCopy.id);
                SendToAllPlayers(this.Write(new Packet.EndMining { id = smCopy.id, userName = smCopy.userName })); // Send finish mining notification to everyone including player.

                // Add ore to user inventory
                var item = new ItemSchema.ItemDBSchema();
                item.uniqueName = ItemSchema.ItemNames.Ore.Value;
                item.userName = p._userName;
                item.quantity = 1;
                _db.AddToUserInventory(item);
                SendInventoryToPlayer(p); // Send inventory to player.
                if (!_db.Unlock(smCopy.id)) // Unlock object in database.
                {
                    throw new Exception(String.Format("Failed to unlock mining object id '{0}' started mining by '{1}', this should never happen. Did object get unlocked somewhere else?", smCopy.id, smCopy.userName));
                }
            });
            p.StartMining(smCopy.id, finishMiningEvent);
            SendToAllOtherPlayers(smCopy.userName, this.Write<Packet.StartMining>(smCopy.Copy())); // Send notification to all other players.
            AddDelayedEvent(finishMiningEvent, 3 * Time.SECOND);
        }

        public void OnAbortMiningPacketReceived(Packet.AbortMining am, NetPeer peer) 
        {
            try
            {
                ValidatePacket(am);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(String.Format("Failed to validate packet in OnAbortMiningPacketReceived from {0}: {1}", peer.EndPoint, ex.ToString()));
                return;
            }

            var amCopy = am.Copy();
            var p = _connectedPlayers[amCopy.userName];
            if (!p.IsMining(amCopy.id))
            {
                Console.WriteLine(String.Format("Abort packet was issued from {0} to abort mining {1} but the server state says the player is not mining that mineable.", amCopy.userName, amCopy.id));
                return;
            }

            _db.Unlock(am.id);
            p.miningState.miningEndEventRef.Cancelled = true;
            p.AbortMining(amCopy.id);
            SendToAllOtherPlayers(p, this.Write<Packet.AbortMining>(amCopy));
        }

        void OnPlayerTransformReceived(Packet.PlayerTransform transform, NetPeer peer)
        {
            try
            {
                ValidatePacket(transform);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(String.Format("Failed to validate packet in OnPlayerTransformReceived from {0}: {1}", peer.EndPoint, ex.ToString()));
                return;
            }

            _connectedPlayers[transform.userName].lastTransform = transform.Copy();
        }

        void OnLoginReceived(Packet.Login login, NetPeer peer)
        {
            try
            {
                ValidatePacket(login);
            } catch (ArgumentException ex)
            {
                Console.WriteLine(String.Format("Failed to validate packet in OnLoginReceived from {0}: {1}", peer.EndPoint, ex.ToString()));
                return;
            }

            if (_connectedPlayers.ContainsKey(login.userName))
            {
                Console.WriteLine("Player {0} is already logged in.", login.userName);
                return;
            }

            Console.WriteLine("Player {0} logged in.", login.userName);
            _connectedPlayers[login.userName] = new PlayerState(login.userName, peer);

            // Send player's inventory to them.
            SendInventoryToPlayer(_connectedPlayers[login.userName]);

            // Send all mineable objects to player.
            List< ObjectSchema.Mineable> allMinables = _db.ReadAllObjects<ObjectSchema.Mineable>(ObjectSchema.ObjectTypes.IObjectType.MINEABLE);
            foreach (var mineable in allMinables)
            {
                peer.Send(this.Write(new Packet.PlaceMinableObject { mineable = mineable }), DeliveryMethod.ReliableUnordered);
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            PlayerState p;
            try
            {
                p = _connectedPlayers.Values.Single(p => p._netPeer == peer);
            } 
            catch (InvalidOperationException)
            {
                Console.WriteLine(String.Format("Peer {0} disconnected but was not found in connectedPlayers.", peer.EndPoint));
                return;
            }

            SendToAllOtherPlayers(p, this.Write(new Packet.PlayerExited { userName = p._userName }));
            Console.WriteLine("Player {0} logged out.", p._userName);
            _connectedPlayers.Remove(p._userName);
        }

        private void ValidatePacket(object packet)
        {
            if (packet is Packet.ObjectIdentifier oi) { 
                if (String.IsNullOrEmpty(oi.id))
                {
                    throw new ArgumentException("Object id in ObjectIdentifier packet is emtpy.");
                }
            }

            if (packet is Packet.PlayerIdentifier pi)
            {
                if (String.IsNullOrEmpty(pi.userName))
                {
                    throw new ArgumentException("Player username in PlayerIdentifier packet is emtpy.");
                }
            }

            if (packet is Packet.ITransform pt)
            {
                if (pt.x == 0 && pt.y == 0 && pt.z == 0 && pt.rot_x == 0 && pt.rot_y == 0 && pt.rot_z == 0 && pt.rot_w == 0)
                {
                    throw new ArgumentException("All of the values inside the ITransform packet are 0.");
                }
            }
        }
    }
}
