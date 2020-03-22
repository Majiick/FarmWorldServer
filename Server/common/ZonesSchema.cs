using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZonesSchema
{
    public class Zone
    {
        private Zone(string value) { Value = value; }
        public string Value { get; set; }
        public static Zone StartingArea { get { return new Zone("StartingArea"); } }
    }

    public class ZoneHelper
    {
        private static readonly ZoneHelper instance = new ZoneHelper();
        private static Dictionary<string, Zone> zoneMap = new Dictionary<string, Zone>();
        static ZoneHelper()
        {
            zoneMap[Zone.StartingArea.Value] = Zone.StartingArea;
        }
        private ZoneHelper() { }

        public static Zone GetZone(string zoneName)
        {
            return zoneMap[zoneName];
        }

        public static ZoneHelper Instance { get { return instance; } }
    }
}
