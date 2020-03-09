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

    class Login : Identifier
    {

    }

    class PlayerTransform : Identifier
    {
        public float x { get; set; }
        public float y { get; set; }
        public float rot_x { get; set; }
        public float rot_y { get; set; }
        public float rot_z { get; set; }
    }
}
