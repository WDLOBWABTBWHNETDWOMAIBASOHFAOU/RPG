﻿using System;
using System.Runtime.Serialization;

namespace Wink
{
    [Serializable]
    class EndTurnEvent : Event
    {
        private Player player;

        public EndTurnEvent(Player player)
        {
            this.player = player;
        }

        #region Serialization
        public EndTurnEvent(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            player = context.GetVars().Local.GetGameObjectByGUID(Guid.Parse(info.GetString("playerGUID"))) as Player;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("playerGUID", player.GUID.ToString());
            base.GetObjectData(info, context);
        }
        #endregion

        public override bool GUIDSerialization
        {
            get { return false; }
        }

        public override bool OnClientReceive(LocalClient client)
        {
            throw new NotImplementedException();
        }

        public override bool OnServerReceive(LocalServer server)
        {
            server.EndTurn(player);
            return true;
        }

        public override bool Validate(Level level)
        {
            //If player's action points are already 0, manually ending turn is not necessary.
            return player.ActionPoints > 0;
        }
    }
}
