using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server
{
    public class ServerConfig
    {
        // These are the defaults but are overwritten by the server_config.json.
        public long startTime = 0;
        public int port = 9050;
        public int ticksPerSecond = 10;
    }

    class GameConfig
    {
        private static GameConfig _instance;
        public ServerConfig config;

        public static GameConfig Instance()
        {
            if (_instance == null)
            {
                _instance = new GameConfig();
            }

            return _instance;
        }

        private GameConfig()
        {
            LoadConfig();
        }

        void LoadConfig()
        {
            try
            {
                using (StreamReader r = new StreamReader("./server_config.json"))
                {
                    string json = r.ReadToEnd();
                    config = JsonConvert.DeserializeObject<ServerConfig>(json);
                    Console.WriteLine("Loaded server_config.json.");
                }
            } catch (System.IO.FileNotFoundException e)
            {
                Console.WriteLine("Writing server_config.json with defaults.");
                config = new ServerConfig();
                WriteConfig();
            }
        }

        public void WriteConfig()
        {
            System.IO.File.WriteAllText("./server_config.json", JsonConvert.SerializeObject(config));
        }
    }
}
