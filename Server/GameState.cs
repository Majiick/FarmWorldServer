using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;

namespace Server
{
    class GameState : NetPacketProcessor
    {
        Dictionary<string, Player> connectedPlayers = new Dictionary<string, Player>();
        NetPacketProcessor _netPacketProcessor = new NetPacketProcessor();

        public GameState()
        {
            SubscribeReusable<Packet.PlayerTransform, LiteNetLib.NetPeer>(OnPositionPacketReceived);
            SubscribeReusable<Packet.Login, LiteNetLib.NetPeer>(OnLoginReceived);
            SubscribeReusable<Packet.StartMining, LiteNetLib.NetPeer>(OnStartMiningPacketReceived);
        }

        void OnStartMiningPacketReceived(Packet.StartMining sm, NetPeer peer)
        {
            // Lock object
            // Send out notification to all other players.
            // Send out EndMining to player 1 sec later.
            Thread.Sleep(1000);
            peer.Send(_netPacketProcessor.Write(new Packet.EndMining { id = sm.id }), DeliveryMethod.ReliableSequenced);
        }

        void OnPositionPacketReceived(Packet.PlayerTransform transform, NetPeer peer)
        {
            connectedPlayers[transform.userName].lastTransform = transform.Copy();
        }

        void OnLoginReceived(Packet.Login login, NetPeer peer)
        {
            if (connectedPlayers.ContainsKey(login.userName))
            {
                Console.WriteLine("Player {0} is already logged in.", login.userName);
                return;
            }

            Console.WriteLine("Player {0} logged in.", login.userName);
            connectedPlayers[login.userName] = new Player(login.userName, peer);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Player p;
            try
            {
                p = connectedPlayers.Values.Single(p => p._netPeer == peer);
            } 
            catch (InvalidOperationException)
            {
                Console.WriteLine(String.Format("Peer {0} disconnected but was not found in connectedPlayers.", peer.EndPoint));
                return;
            }
            
            if (p == null)
            {
                Console.WriteLine("IP {0} was not logged in but tried to log out.", peer.EndPoint);
                return;
            }

            SendToAllOtherPlayers(p, _netPacketProcessor.Write(new Packet.PlayerExited { userName = p._userName }));
            Console.WriteLine("Player {0} logged out.", p._userName);
            connectedPlayers.Remove(p._userName);
        }

        public void Tick()
        {
            foreach (Player player in connectedPlayers.Values)
            {
                SendToAllOtherPlayers(player, _netPacketProcessor.Write(player.lastTransform));
            }
        }

        void SendToAllOtherPlayers(Player excluded, byte[] bytes)
        {
            foreach (Player player in connectedPlayers.Values)
            {
                if (player == excluded) continue;
                player._netPeer.Send(bytes, DeliveryMethod.ReliableSequenced);
            }
        }
    }
}
