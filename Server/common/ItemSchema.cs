using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace ItemSchema
{
    public interface IItem
    {
        string uniqueName { get; set; }
        string description { get; set; }
    }

    public struct ItemDBSchema : INetSerializable  // How the object looks like in the database.
    {
        public string id { get; set; }
        public string uniqueName { get; set; }
        public string userName { get; set; }
        public int quantity { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            id = reader.GetString();
            uniqueName = reader.GetString();
            userName = reader.GetString();
            quantity = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(id);
            writer.Put(uniqueName);
            writer.Put(userName);
            writer.Put(quantity);
        }
    }

    public class Ore : IItem
    {
        public string uniqueName { get; set; }
        public string description { get; set; }

        public Ore()
        {
            uniqueName = ItemNames.Ore.Value;
            description = "A shiny piece of Ore.";
        }
    }

    public class Geode : IItem
    {
        public string uniqueName { get; set; }
        public string description { get; set; }

        public Geode()
        {
            uniqueName = ItemNames.Geode.Value;
            description = "A hollow rock. You can hear something rattling inside.";
        }
    }

    public class Wood : IItem
    {
        public string uniqueName { get; set; }
        public string description { get; set; }

        public Wood()
        {
            uniqueName = ItemNames.Wood.Value;
            description = "It's an ordinary piece of wood.";
        }
    }

    public class Herring : IItem
    {
        public string uniqueName { get; set; }
        public string description { get; set; }

        public Herring()
        {
            uniqueName = ItemNames.Herring.Value;
            description = "A herring fish. Food for the commoners.";
        }
    }

    public class ItemSchema
    {
        private static readonly ItemSchema instance = new ItemSchema();
        private static Dictionary<string, IItem> itemMap = new Dictionary<string, IItem>();
        static ItemSchema()
        {
            itemMap[ItemNames.Ore.Value] = new Ore();
        }
        private ItemSchema() { }

        public static IItem GetItem(string uniqueName)
        {
            return itemMap[uniqueName];
        }

        public static ItemSchema Instance { get { return instance; } }
    }

    public class ItemNames
    {
        private ItemNames(string value) { Value = value; }
        public string Value { get; set; }
        public static ItemNames Ore { get { return new ItemNames("Ore"); } }
        public static ItemNames Geode { get { return new ItemNames("Geode"); } }
        public static ItemNames Herring { get { return new ItemNames("Herring"); } }
        public static ItemNames Wood { get { return new ItemNames("Wood"); } }
    }
}
