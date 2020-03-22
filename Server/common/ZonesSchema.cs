using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZonesSchema
{
    public class Zones
    {
        private Zones(string value) { Value = value; }
        public string Value { get; set; }
        public static Zones StartingArea { get { return new Zones("StartingArea"); } }
    }
}
