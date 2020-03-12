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
        Dictionary<string, Player> _connectedPlayers = new Dictionary<string, Player>();
        NetPacketProcessor _netPacketProcessor = new NetPacketProcessor();
        List<DelayedEvent> _delayedEvents = new List<DelayedEvent>();
        public long TickStartTime { get; set; }  // Gets set by the Server at the start of the tick.

        public GameState()
        {
            SubscribeReusable<Packet.PlayerTransform, LiteNetLib.NetPeer>(OnPositionPacketReceived);
            SubscribeReusable<Packet.Login, LiteNetLib.NetPeer>(OnLoginReceived);
            SubscribeReusable<Packet.StartMining, LiteNetLib.NetPeer>(OnStartMiningPacketReceived);
        }

        public void Tick()
        {
            foreach (Player player in _connectedPlayers.Values)
            {
                SendToAllOtherPlayers(player, _netPacketProcessor.Write(player.lastTransform));
            }

            ExecuteDelayedEvents();
        }

        void AddDelayedEvent(Action action, float executionDelay)
        {
            _delayedEvents.Add(new DelayedEvent(action, executeAt: TickStartTime + executionDelay));
        }

        void ExecuteDelayedEvents()
        {
            List<DelayedEvent> notExecuted = new List<DelayedEvent>(_delayedEvents.Count);
            foreach (DelayedEvent e in _delayedEvents)
            {
                if (e.ExecuteAt >= TickStartTime)
                {
                    e.Action();
                } else
                {
                    notExecuted.Add(e);
                }
            }

            _delayedEvents = notExecuted;  // Filter out all of the executed events.
        }

        void SendToAllOtherPlayers(Player excluded, byte[] bytes)
        {
            foreach (Player player in _connectedPlayers.Values)
            {
                if (player == excluded) continue;
                player._netPeer.Send(bytes, DeliveryMethod.ReliableSequenced);
            }
        }

        void OnStartMiningPacketReceived(Packet.StartMining sm, NetPeer peer)
        {
            // TODO: Lock object
            // TODO: Send out notification to all other players.
            AddDelayedEvent(
                () => peer.Send(_netPacketProcessor.Write(new Packet.EndMining { id = sm.id }), DeliveryMethod.ReliableSequenced),
                3 * Time.SECOND);
            
        }

        void OnPositionPacketReceived(Packet.PlayerTransform transform, NetPeer peer)
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
            _connectedPlayers[login.userName] = new Player(login.userName, peer);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Player p;
            try
            {
                p = _connectedPlayers.Values.Single(p => p._netPeer == peer);
            } 
            catch (InvalidOperationException)
            {
                Console.WriteLine(String.Format("Peer {0} disconnected but was not found in connectedPlayers.", peer.EndPoint));
                return;
            }

            SendToAllOtherPlayers(p, _netPacketProcessor.Write(new Packet.PlayerExited { userName = p._userName }));
            Console.WriteLine("Player {0} logged out.", p._userName);
            _connectedPlayers.Remove(p._userName);
        }
    }
}
