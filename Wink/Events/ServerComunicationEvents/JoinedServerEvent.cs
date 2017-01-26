﻿using System;
using System.Runtime.Serialization;

namespace Wink
{
    [Serializable]
    class JoinedServerEvent : LevelUpdatedEvent
    {
        public JoinedServerEvent(Level level) : base(level)
        { }

        public JoinedServerEvent(SerializationInfo info, StreamingContext context) : base(info, context)
        { }

        public override bool OnClientReceive(LocalClient client)
        {
            base.OnClientReceive(client);
            client.Camera.CenterOn(client.Player);
            GameEnvironment.GameStateManager.SwitchTo("playingState");
            return true;
        }
    }
}
