using System;
using System.Collections.Generic;
using System.Text;

namespace Packet
{
    class Identifier
    {
        /* This class tells our server which player the packet is coming from. 
           This packet is included in every request.
        */
        public string userName { get; set; }  // This is a unique identifier.
    }

    // Login is only for server.
    class Login : Identifier
    {

    }

    // PlayerExited is only for client. (Server uses OnPeerDisconnected LiteNetLib event.)
    class PlayerExited : Identifier
    {

    }

    // PlayerTransform is for both server and players.
    class PlayerTransform : Identifier
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float rot_x { get; set; }
        public float rot_y { get; set; }
        public float rot_z { get; set; }
        public float rot_w { get; set; }

        public PlayerTransform Clone()
        {
            return (PlayerTransform)this.MemberwiseClone();
        }
    }
}
