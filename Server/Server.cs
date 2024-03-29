﻿using LiteNetLib;
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
        NetManager _server;
        Database _db;

        void SetupDB()
        {
            _db = new Database();
            List<ObjectSchema.IObject> lockedObjects = _db.GetAllLockedObjects();
            foreach (var obj in lockedObjects)
            {
                Console.WriteLine(String.Format("Object '{0}' '{1}' was locked on startup.", obj.id, obj.type));
            }
            _db.UnlockAllObjects();
        }

        public void BlockingStart()
        {
            SetupDB();
            GameState gameState = new GameState(_db);
            Listener listener = new Listener();
            _server = new NetManager(listener);
            listener.Setup(gameState);
            _server.Start(GameConfig.Instance().config.port);
            Console.WriteLine("Server started.");

            Stopwatch _precisionTime = new Stopwatch();
            _precisionTime.Start();
            while (true)
            {
                long unadjustedTickStartTime = _precisionTime.ElapsedMilliseconds;
                GameTime.Instance().UpdateTickStartTime(_precisionTime.ElapsedMilliseconds); // Update the tick start time.
                _server.PollEvents();
                gameState.Tick();
                if (_precisionTime.ElapsedMilliseconds - unadjustedTickStartTime > 1000 / GameConfig.Instance().config.ticksPerSecond) {
                    Console.WriteLine(String.Format("Tick took {0} milliseconds which is past the tick rate.", _precisionTime.ElapsedMilliseconds - unadjustedTickStartTime));
                }
                // Wait until time for next tick.
                while (_precisionTime.ElapsedMilliseconds - unadjustedTickStartTime < 1000 / GameConfig.Instance().config.ticksPerSecond)
                {
                    // This while loop should actually just be empty. We should probably be completely hot-looping this thread for maximum precision.
                    // Question though: Do we need better than 1-5ms resolution?
                    Thread.Sleep(1);
                }
            }

            _server.Stop();
        }
    }
}
