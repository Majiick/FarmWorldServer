using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Threading;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            NetPacketProcessor _netPacketProcessor = new NetPacketProcessor();
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager server = new NetManager(listener);
            server.Start(9050 /* port */);

            listener.ConnectionRequestEvent += request =>
            {
                if (server.PeersCount < 10 /* max connections */)
                    request.AcceptIfKey("SomeConnectionKey");
                else
                    request.Reject();
            };

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("We got connection: {0}", peer.EndPoint); // Show peer ip
                NetDataWriter writer = new NetDataWriter();                 // Create writer class
                writer.Put("Hello client!");                                // Put some string
                peer.Send(writer, DeliveryMethod.ReliableOrdered);             // Send with reliability
            };

            listener.NetworkReceiveEvent += (peer, reader, deliveryMethod) =>
            {
                _netPacketProcessor.ReadAllPackets(reader, peer);
            };

            _netPacketProcessor.SubscribeReusable<Packet.Position, NetPeer>(OnPositionPacketReceived);

            while (!Console.KeyAvailable)
            {
                server.PollEvents();
                Thread.Sleep(15);
            }
            server.Stop();
        }

        public static void OnPositionPacketReceived(Packet.Position position, NetPeer peer)
        {
            Console.WriteLine(position.ToString());
            Console.WriteLine(position.x);
            Console.WriteLine(position.y);
        }
    }
}
