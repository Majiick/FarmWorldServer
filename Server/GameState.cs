using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Server
{
    class GameState : NetPacketProcessor
    {
        Dictionary<string, Player> connectedPlayers = new Dictionary<string, Player>();

        public GameState()
        {
            SubscribeReusable<Packet.Position, LiteNetLib.NetPeer>(OnPositionPacketReceived);
            SubscribeReusable<Packet.Login, LiteNetLib.NetPeer>(OnLoginReceived);
        }

        void OnPositionPacketReceived(Packet.Position position, NetPeer peer)
        {
            connectedPlayers[position.userName].lastX = position.x;
            connectedPlayers[position.userName].lastY = position.y;
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
            Player p = connectedPlayers.Values.Single(p => p._netPeer == peer);
            if (p == null)
            {
                Console.WriteLine("IP {0} was not logged in but tried to log out.", peer.EndPoint);
                return;
            }

            Console.WriteLine("Player {0} logged out.", p._userName);
            connectedPlayers.Remove(p._userName);
        }

        public void Tick()
        {
            NetPacketProcessor _netPacketProcessor = new NetPacketProcessor();
            foreach (Player player in connectedPlayers.Values)
            {
                foreach (Player otherPlayer in connectedPlayers.Values)
                {
                    if (player == otherPlayer) continue;
                    player._netPeer.Send(_netPacketProcessor.Write(
                        new Packet.Position { x = otherPlayer.lastX, y = otherPlayer.lastY, userName = otherPlayer._userName }), 
                        DeliveryMethod.ReliableSequenced);
                }
            }
        }
    }
}
