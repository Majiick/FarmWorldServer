using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Server
{
    class GameState : NetPacketProcessor
    {
        Dictionary<string, PlayerState> _connectedPlayers = new Dictionary<string, PlayerState>();
        List<DelayedEvent> _delayedEvents = new List<DelayedEvent>();
        Database _db;

        public GameState(Database db)
        {
            _db = db;

            RegisterNestedType<ObjectSchema.Mineable>();
            RegisterNestedType<ObjectSchema.Plantable>();
            RegisterNestedType<ItemSchema.ItemDBSchema>();

            SubscribeReusable<Packet.PlayerTransform, LiteNetLib.NetPeer>(OnPlayerTransformReceived);
            SubscribeReusable<Packet.Login, LiteNetLib.NetPeer>(OnLoginReceived);
            SubscribeReusable<Packet.MinedQuantity, LiteNetLib.NetPeer>(OnMinedQuantityReceived);
            SubscribeReusable<Packet.StartMining, LiteNetLib.NetPeer>(OnStartMiningPacketReceived);
            SubscribeReusable<Packet.EndMining, LiteNetLib.NetPeer>(OnEndMiningPacketReceived);
            SubscribeReusable<Packet.AbortMining, LiteNetLib.NetPeer>(OnAbortMiningPacketReceived);
            SubscribeReusable<Packet.FishThrowBobbler, LiteNetLib.NetPeer>(OnFishThrowBobblerPacketReceived);
            SubscribeReusable<Packet.FishBobblerInWater, LiteNetLib.NetPeer>(OnFishBobblerInWaterPacketReceived);
            SubscribeReusable<Packet.FishCaught, LiteNetLib.NetPeer>(OnFishCaughtPacketReceived);
            SubscribeReusable<Packet.AbortFishing, LiteNetLib.NetPeer>(OnAbortFishingPacketReceived);
            SubscribeReusable<Packet.Developer.DeveloperPlaceMinableObject, LiteNetLib.NetPeer>(OnPlaceMinableObjectPacketReceived);
            SubscribeReusable<Packet.PlacePlantableObject, LiteNetLib.NetPeer>(OnPlacePlantableObjectPacketReceived);
        }

        public void Tick()
        {
            foreach (PlayerState player in _connectedPlayers.Values)
            {
                if (player.lastTransform != null)
                {
                    SendToAllOtherPlayers(player, this.Write(player.lastTransform));
                }
            }

            ExecuteDelayedEvents();
        }

        void AddDelayedEvent(Action action, long executionDelay)
        {
            _delayedEvents.Add(new DelayedEvent(action, executeAt: GameTime.Instance().TickStartTime() + executionDelay));
        }

        void AddDelayedEvent(DelayedEvent e, long executionDelay)
        {
            e.ExecuteAt = GameTime.Instance().TickStartTime() + executionDelay;
            _delayedEvents.Add(e);
        }

        void ExecuteDelayedEvents()
        {
            List<DelayedEvent> notExecuted = new List<DelayedEvent>(_delayedEvents.Count);
            foreach (DelayedEvent e in _delayedEvents)
            {
                if (GameTime.Instance().TickStartTime() >= e.ExecuteAt && !e.Cancelled)
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
            //Console.WriteLine("Seding pos");
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

        void AddXPToPlayerAndSendReceivedXP(PlayerState p, int xp) {
            p.AddXP(xp);
            _db.AddXP(p._userName, xp);
            SendToAllPlayers(this.Write(new Packet.ReceivedXP { userName = p._userName, xpGained = xp, xpTotal = p.totalXp}));
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
            AddDelayedEvent(fishBitingEvent, waitSeconds * GameTime.SECOND);
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
            var objCopy = obj.Copy();
            try
            {
                ValidatePacket(objCopy);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(String.Format("Failed to validate packet in OnPlaceMinableObjectPacketReceived from {0}: {1}", peer.EndPoint, ex.ToString()));
                return;
            }
            
            var id = _db.Write(objCopy.mineable);
            ObjectSchema.Mineable m = _db.Read<ObjectSchema.Mineable>(id);
            SendToAllPlayers(this.Write(new Packet.PlaceMinableObject { mineable = m }));
        }

        /*
        void OnStartHarvestReceived(Packet.StartHarvest startHarvestPacket, NetPeer peer) {
            ObjectSchema.Plantable readPlant = _db.Read<ObjectSchema.Plantable>(startHarvestPacket.id);

            //Check if object is locked first if locked return
            // SendToAllPlayers(this.Write(new Packet.AbortHarvest { userName = startHarvestPacket.userName, message = "This object is locked by another player" }));

            //Allow the player to harvest
            if (!PlantManager.CheckHarvestable(readPlant)) {
                SendToAllPlayers(this.Write(new Packet.AbortHarvest { userName = startHarvestPacket.userName, message = "This plant is not yet harvestable" }));
                return;
            }

            DelayedEvent finishHarvesting = new DelayedEvent(() =>  // This delayed event is cancelled if AbortMining is called.
            {
                SendToAllPlayers();
            });

            p.StartHarvesting(smCopy.id, finishHarvesting);
            // Send notification to all other players. that player has started harvesting
            AddDelayedEvent(finishHarvesting, 2 * GameTime.SECOND);
        }
        */

        void OnPlacePlantableObjectPacketReceived(Packet.PlacePlantableObject placePlantableObject, NetPeer peer) 
        {
            Packet.PlacePlantableObject ppCopy = placePlantableObject.Copy();
            ObjectSchema.Plantable toWritePlant = ppCopy.plantable;
            toWritePlant.growthTime = PlantManager.CalculateGrowthTime(toWritePlant.plantableType);
            toWritePlant.timePlanted = GameTime.Instance().TickStartTime();
            var id = _db.Write(toWritePlant);
      
            ObjectSchema.Plantable readPlant = _db.Read<ObjectSchema.Plantable>(id);
            SendToAllPlayers(this.Write(new Packet.StartPlanting { userName = ppCopy.userName }));
            SendToAllPlayers(this.Write(new Packet.PlacePlantableObject { plantable = readPlant}));
        }

        void OnEndMiningPacketReceived(Packet.EndMining em, NetPeer peer)
        {
            Packet.EndMining emCopy = em.Copy();
            try
            {
                ValidatePacket(emCopy);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(String.Format("Failed to validate packet in OnEndMiningPacketReceived from {0}: {1}", peer.EndPoint, ex.ToString()));
                return;
            }

            var p = _connectedPlayers[emCopy.userName];

            // Add item to user inventory
            var item = new ItemSchema.ItemDBSchema();
            if (p.miningState.minableSubType == ObjectSchema.ObjectTypes.IMineableSubMineableType.OAK.Value)
            {
                item.uniqueName = ItemSchema.ItemNames.Wood.Value;
            }
            else if (p.miningState.minableSubType == ObjectSchema.ObjectTypes.IMineableSubMineableType.IRON.Value)
            {
                item.uniqueName = ItemSchema.ItemNames.Ore.Value;
            }
            else if (p.miningState.minableSubType == ObjectSchema.ObjectTypes.IMineableSubMineableType.STONE.Value)
            {
                item.uniqueName = ItemSchema.ItemNames.Ore.Value;
            }
            else
            {
                throw new ArgumentException(String.Format("smCopy.minableType type {0} not recognized.", p.miningState.minableSubType));
            }

            item.userName = p._userName;
            item.quantity = p.miningState.minedInSession;
            _db.AddToUserInventory(item);
            SendInventoryToPlayer(p); // Send inventory to player.
            if (!_db.Unlock(emCopy.id)) // Unlock object in database.
            {
                throw new Exception(String.Format("Failed to unlock mining object id '{0}' started mining by '{1}', this should never happen. Did object get unlocked somewhere else?", emCopy.id, emCopy.userName));
            }

            // TODO: Update object quantity in db.
            // TODO: Validate that player isn't cheating.
            AddXPToPlayerAndSendReceivedXP(p, GameStatistics.GameStatistics.XpForMining(p.miningState.minableSubType, "MEDIUM"));  // TODO: Add size.
            p.EndMining(emCopy.id);
            SendToAllOtherPlayers(em.userName, Write<Packet.EndMining>(em));
        }

        void OnStartMiningPacketReceived(Packet.StartMining sm, NetPeer peer)
        {
            Packet.StartMining smCopy = sm.Copy();
            try
            {
                ValidatePacket(smCopy);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(String.Format("Failed to validate packet in OnStartMiningPacketReceived from {0}: {1}", peer.EndPoint, ex.ToString()));
                return;
            }
            
            var p = _connectedPlayers[smCopy.userName];
            if (p.IsMining())
            {
                Console.WriteLine(
                    String.Format("Player {0} sent StartMining but was already mining {1}. Resetting player state.",
                                  smCopy.userName, p.miningState.miningId));
                _db.Unlock(p.miningState.miningId);
                p.ResetState();
            }

            if (!_db.Lock(smCopy.id, smCopy.userName)) // Lock object
            {
                Console.WriteLine(String.Format("Player {0} failed to lock object {1}", smCopy.userName, smCopy.id));
                peer.Send(this.Write(new Packet.MiningLockFailed { id = smCopy.id, userName = smCopy.userName}), DeliveryMethod.ReliableOrdered);
                return;
            }

            p.StartMining(smCopy.id, smCopy.minableType, smCopy.minableSubType);
            SendToAllOtherPlayers(smCopy.userName, this.Write<Packet.StartMining>(smCopy.Copy())); // Send notification to all other players.
        }

        public void OnMinedQuantityReceived(Packet.MinedQuantity mq, NetPeer peer)
        {
            var mqCopy = mq.Copy();
            try
            {
                ValidatePacket(mqCopy);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(String.Format("Failed to validate packet in OnAbortMiningPacketReceived from {0}: {1}", peer.EndPoint, ex.ToString()));
                return;
            }

            // TODO: Validate that player isn't cheating. 
            var p = _connectedPlayers[mqCopy.userName];
            p.MineQuantity(mqCopy.item.quantity, mqCopy.id);
            SendToAllOtherPlayers(p, this.Write<Packet.MinedQuantity>(mqCopy));
        }

        public void OnAbortMiningPacketReceived(Packet.AbortMining am, NetPeer peer) 
        {
            var amCopy = am.Copy();
            try
            {
                ValidatePacket(amCopy);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(String.Format("Failed to validate packet in OnAbortMiningPacketReceived from {0}: {1}", peer.EndPoint, ex.ToString()));
                return;
            }
            
            var p = _connectedPlayers[amCopy.userName];
            if (!p.IsMining(amCopy.id))
            {
                Console.WriteLine(String.Format("Abort packet was issued from {0} to abort mining {1} but the server state says the player is not mining that mineable.", amCopy.userName, amCopy.id));
                return;
            }

            _db.Unlock(am.id);
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
            //Console.WriteLine(transform.x);
            //Console.WriteLine(transform.z);
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
            ObjectSchema.Player player = _db.GetPlayer(login.userName);

            // Send player's initial state.
            peer.Send(Write(new Packet.LoginInitialPlayerState { userName = player.userName, xp = player.xp }), DeliveryMethod.ReliableOrdered);

            // Send player's inventory to them.
            SendInventoryToPlayer(_connectedPlayers[login.userName]);

            //Send Current time to player
            peer.Send(this.Write(new Packet.CurrentServerTime { time = GameTime.Instance().TickStartTime() }), DeliveryMethod.ReliableSequenced);

            // Send all mineable objects to player.
            List< ObjectSchema.Mineable> allMinables = _db.ReadAllObjects<ObjectSchema.Mineable>(ObjectSchema.ObjectTypes.IObjectType.MINEABLE);
            foreach (var mineable in allMinables)
            {
                peer.Send(this.Write(new Packet.PlaceMinableObject { mineable = mineable }), DeliveryMethod.ReliableSequenced);
            }

            List<ObjectSchema.Plantable> allPlantables = _db.ReadAllObjects<ObjectSchema.Plantable>(ObjectSchema.ObjectTypes.IObjectType.PLANTABLE);
            foreach (var plantable in allPlantables)
            {
                peer.Send(this.Write(new Packet.PlacePlantableObject { plantable = plantable }), DeliveryMethod.ReliableSequenced);
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            PlayerState p;
            try
            {
                p = _connectedPlayers.Values.Single(pack => pack._netPeer == peer);
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

            if (packet is Packet.PlaceMinableObject po) {

                if (String.IsNullOrEmpty(po.mineable.mineableType)) {
                    throw new ArgumentException("Placeable minable type in PlaceMinableObject packet is emtpy.");
                }
            }

            if (packet is Packet.Developer.DeveloperPlaceMinableObject pmo)
            {
                if (pmo.mineable.x == 0 && pmo.mineable.y == 0 && pmo.mineable.z == 0 && pmo.mineable.rot_x == 0 && pmo.mineable.rot_y == 0 && pmo.mineable.rot_z == 0 && pmo.mineable.rot_w == 0)
                {
                    throw new ArgumentException("DeveloperPlaceMinableObject transform is empty.");
                }

                if (pmo.mineable.remainingQuantity == 0)
                {
                    throw new ArgumentException("DeveloperPlaceMinableObjectmineable.remainingQuantity is empty.");
                }

                if (String.IsNullOrEmpty(pmo.mineable.type))
                {
                    throw new ArgumentException("DeveloperPlaceMinableObject.mineable.type is empty.");
                }

                if (String.IsNullOrEmpty(pmo.mineable.size))
                {
                    throw new ArgumentException("DeveloperPlaceMinableObject.mineable.size is empty.");
                }

                if (String.IsNullOrEmpty(pmo.mineable.mineableType))
                {
                    throw new ArgumentException("DeveloperPlaceMinableObject.mineable.mineableType is empty.");
                }

                if (String.IsNullOrEmpty(pmo.mineable.subMineableType))
                {
                    throw new ArgumentException("DeveloperPlaceMinableObject.mineable.subMineableType is empty.");
                }
            }

            if (packet is Packet.IItem itp)
            {
                if (String.IsNullOrEmpty(itp.item.uniqueName))
                {
                    throw new ArgumentException("IItem packet item.uniqueName is empty.");
                }

                if (itp.item.quantity == 0)
                {
                    throw new ArgumentException("IItem packet item.quantity is empty.");
                }

                if (String.IsNullOrEmpty(itp.item.userName))
                {
                    throw new ArgumentException("IItem packet item.userName is empty.");
                }
            }

            if (packet is Packet.MinedQuantity mq)
            {
                if (mq.fadeScale == 0)
                {
                    throw new ArgumentException("MinedQuantity packet fadeScale is empty.");
                }

                if (mq.notificationScale == 0)
                {
                    throw new ArgumentException("MinedQuantity packet notificationScale is empty.");
                }
            }
        }
    }
}
