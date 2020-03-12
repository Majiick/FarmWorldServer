using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public struct DelayedEvent
    {
        public DelayedEvent(Action action, float executeAt)
        {
            Action = action;
            ExecuteAt = executeAt;
        }

        public Action Action { get; }
        public float ExecuteAt { get; }
    }
}
