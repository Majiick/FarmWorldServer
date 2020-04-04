using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Server.load_test
{
    class LoadTest
    {
        List<Thread> threads = new List<Thread>();
        bool started = false;

        public LoadTest()
        {
            
        }

        public void Start(int clients_amount)
        {
            if (started) return;

            for (int i = 0; i < clients_amount; i++)
            {
                Thread t = new Thread(LoginAndWalk);
                threads.Add(t);
                t.Start();
            }

            started = true;
        }

        public void Stop()
        {
            foreach (var t in threads)
            {
                t.Abort();
            }
        }

        private void LoginAndWalk()
        {
            Random rnd = new Random();
            float x = 0;
            float y = 0.13f;
            float z = 0;
            NetManager _client;
            NetPacketProcessor _netPacketProcessor = new NetPacketProcessor();
            EventBasedNetListener listener = new EventBasedNetListener();
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
            _client = new NetManager(listener);
            _client.Start();
            _client.Connect("localhost", 9050, "SomeConnectionKey");

            string userName = Thread.CurrentThread.ManagedThreadId.ToString();
            Console.WriteLine(userName);


            _client.SendToAll(_netPacketProcessor.Write(new Packet.Login { userName = userName }), DeliveryMethod.ReliableOrdered);
            while (true)
            {
                Thread.Sleep(100);
                var bytes = _netPacketProcessor.Write(new Packet.PlayerTransform
                {
                    x = x,
                    y = y,
                    z = z,
                    rot_x = 0,
                    rot_y = 0,
                    rot_z = 0,
                    rot_w = 0,
                    userName = userName
                });
                x += (float)rnd.NextDouble() + -(float)rnd.NextDouble();
                y += 0;
                z += (float)rnd.NextDouble() + -(float)rnd.NextDouble();

                _client.SendToAll(bytes, DeliveryMethod.ReliableOrdered);
            }
        }

        private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            reader.Recycle();
            reader.Clear();
            // Do nothing.
        }
    }
}
