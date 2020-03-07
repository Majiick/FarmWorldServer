using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    class Player
    {
        public NetPeer _netPeer;
        public string _userName;
        public float lastX { get; set; }
        public float lastY { get; set; }
        
        public Player(string userName, NetPeer netPeer)
        {
            _userName = userName;
            _netPeer = netPeer;
        }
    }
}
