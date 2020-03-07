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
            Server server = new Server();
            server.BlockingStart();
        }
    }
}
