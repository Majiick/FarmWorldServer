using LiteNetLib;
using LiteNetLib.Utils;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace Server
{
    class Listener : INetEventListener
    {
        GameState _gameState;

        public void Setup(GameState gameState)
        {
            _gameState = gameState;
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey("SomeConnectionKey");
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            try
            {
                _gameState.ReadAllPackets(reader, peer);
            }
            catch (ParseException p)
            {
                Console.WriteLine("ParseException from client {0}: " + p.ToString(), peer.EndPoint);
            }
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine("Client connected: {0}", peer.EndPoint);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            _gameState.OnPeerDisconnected(peer, disconnectInfo);
            Console.WriteLine("Client disconnected: {0}", peer.EndPoint);
        }
    }

    class Server
    {
        public readonly int port = 9050;
        public readonly int maxClients = 10;
        NetManager _server;
        Database _db;

        public void BlockingStart()
        {
            _db = new Database();
            GameState gameState = new GameState();
            Listener listener = new Listener();
            _server = new NetManager(listener);
            listener.Setup(gameState);
            _server.Start(port);

            Stopwatch _precisionTime = new Stopwatch();
            _precisionTime.Start();
            while (!Console.KeyAvailable)
            {
                gameState.TickStartTime = _precisionTime.ElapsedMilliseconds;
                _server.PollEvents();
                gameState.Tick();

                // Wait until time for next tick.
                while (_precisionTime.ElapsedMilliseconds < gameState.TickStartTime + 50)
                {
                    Thread.Sleep(1);
                }
            }

            _server.Stop();
        }
    }
}
