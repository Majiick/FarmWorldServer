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

            SubscribeReusable<Packet.PlayerTransform, LiteNetLib.NetPeer>(OnPlayerTransformReceived);
            SubscribeReusable<Packet.Login, LiteNetLib.NetPeer>(OnLoginReceived);
            SubscribeReusable<Packet.StartMining, LiteNetLib.NetPeer>(OnStartMiningPacketReceived);
            SubscribeReusable<Packet.AbortMining, LiteNetLib.NetPeer>(OnAbortMiningPacketReceived);
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

        void ExecuteDelayedEvents()
        {
            List<DelayedEvent> notExecuted = new List<DelayedEvent>(_delayedEvents.Count);
            foreach (DelayedEvent e in _delayedEvents)
            {
                if (TickStartTime >= e.ExecuteAt)
                {
                    e.Action();
                } else
                {
                    notExecuted.Add(e);
                }
            }

            _delayedEvents = notExecuted;  // Filter out all of the executed events.
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

        void OnPlaceMinableObjectPacketReceived(Packet.Developer.DeveloperPlaceMinableObject obj, NetPeer peer)
        {
            var id = _db.Write(obj.mineable);
            ObjectSchema.Mineable m = _db.Read<ObjectSchema.Mineable>(id);
            SendToAllPlayers(this.Write(new Packet.PlaceMinableObject { mineable = m }));
        }

        void OnStartMiningPacketReceived(Packet.StartMining sm, NetPeer peer)
        {
            Packet.StartMining smCopy = sm.Copy();
            if (!_db.Lock(smCopy.id, smCopy.userName)) // Lock object
            {
                Console.WriteLine(String.Format("Player {0} failed to lock object {1}", smCopy.userName, smCopy.id));
                peer.Send(this.Write(new Packet.MiningLockFailed { id = smCopy.id, userName = smCopy.userName}), DeliveryMethod.ReliableOrdered);
                return;
            }

            var p = _connectedPlayers[smCopy.userName];
            p.StartMining(smCopy.id);
            SendToAllOtherPlayers(smCopy.userName, this.Write<Packet.StartMining>(smCopy.Copy())); // Send notification to all other players.
            AddDelayedEvent(
                () => {
                    if (p.IsMining(smCopy.id))
                    {
                        p.EndMining();
                        SendToAllPlayers(this.Write(new Packet.EndMining { id = smCopy.id, userName = smCopy.userName })); // Send finish mining notification to everyone including player.
                        if (!_db.Unlock(smCopy.id))
                        {
                            throw new Exception(String.Format("Failed to unlock mining object id '{0}' started mining by '{1}', this should never happen. Did object get unlocked somewhere else?", smCopy.id, smCopy.userName));
                        }
                    }
                },
                3 * Time.SECOND);
        }

        public void OnAbortMiningPacketReceived(Packet.AbortMining am, NetPeer peer) 
        {
            var amCopy = am.Copy();
            var p = _connectedPlayers[amCopy.userName];
            if (!p.IsMining(amCopy.id))
            {
                Console.WriteLine(String.Format("Abort packet was issued from {0} to abort mining {1} but the server state says the player is not mining that mineable.", amCopy.userName, amCopy.id));
                return;
            }

            p.AbortMining();
            SendToAllOtherPlayers(p, this.Write<Packet.AbortMining>(amCopy));
        }

        void OnPlayerTransformReceived(Packet.PlayerTransform transform, NetPeer peer)
        {
            _connectedPlayers[transform.userName].lastTransform = transform.Copy();
        }

        void OnLoginReceived(Packet.Login login, NetPeer peer)
        {
            if (_connectedPlayers.ContainsKey(login.userName))
            {
                Console.WriteLine("Player {0} is already logged in.", login.userName);
                return;
            }

            Console.WriteLine("Player {0} logged in.", login.userName);
            _connectedPlayers[login.userName] = new PlayerState(login.userName, peer);

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
    }
}
