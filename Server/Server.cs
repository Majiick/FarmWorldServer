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

        public void BlockingStart()
        {
            GameState gameState = new GameState();
            Listener listener = new Listener();
            _server = new NetManager(listener);
            listener.Setup(gameState);
            _server.Start(port);

            Stopwatch stopWatch = new Stopwatch();
            while (!Console.KeyAvailable)
            {
                stopWatch.Start();
                _server.PollEvents();
                gameState.Tick();
                while (stopWatch.ElapsedMilliseconds < 50)
                {
                    Thread.Sleep(1);
                }
                stopWatch.Reset();
            }
            _server.Stop();
        }
    }
}
