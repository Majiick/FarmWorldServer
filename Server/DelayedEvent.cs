using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class DelayedEvent
    {
        public DelayedEvent(Action action, float executeAt)
        {
            Action = action;
            ExecuteAt = executeAt;
            Cancelled = false;
        }

        public DelayedEvent(Action action)
        {
            Action = action;
            ExecuteAt = 0;
            Cancelled = false;
        }

        public Action Action { get; }
        public float ExecuteAt { get; set; }
        public bool Cancelled { get; set; }
    }
}
