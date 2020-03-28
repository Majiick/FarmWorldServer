using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Server
{
    class GameTime
    {
        // All time constans in terms of milliseconds.
        public const long SECOND = 1000;
        public const long MINUTE = SECOND * 60;
        public const long HOUR = MINUTE * 60;

        private static GameTime _instance;
        private long _offset;
        private long _tickStartTime; // Gets set by the Server at the start of the tick.

        private readonly long saveInterval = 5000;
        private long lastSaveTime = 0;

        public static GameTime Instance()
        {
            if (_instance == null)
            {
                _instance = new GameTime();
            }

            return _instance;
        }

        private GameTime()
        {
            _offset = GameConfig.Instance().config.startTime;
            lastSaveTime = _offset;
        }

        public long TickStartTime()  // in milliseconds.
        {
            return _offset + _tickStartTime;
        }

        public void UpdateTickStartTime(long tickStartTime)
        {
            _tickStartTime = tickStartTime;

            if (_tickStartTime > lastSaveTime + saveInterval)
            {
                GameConfig.Instance().config.startTime = _tickStartTime;
                GameConfig.Instance().WriteConfig();
                lastSaveTime = tickStartTime;
            }
        }
    }
}
