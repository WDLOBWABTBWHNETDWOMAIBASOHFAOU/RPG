﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Wink
{
    [Serializable]
    class StatIncreaseEvent : Event
    {
        
        private Player player;
        private Stat stat;
        
        public StatIncreaseEvent(Player player, Stat stat ) : base()
        {
            this.player = player;
            this.stat = stat;
        }

        #region Serialization
        public StatIncreaseEvent(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            player = context.GetVars().Local.GetGameObjectByGUID(Guid.Parse(info.GetString("playerGUID"))) as Player;
            stat = (Stat)info.GetValue("stat", typeof(Stat));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("playerGUID", player.GUID.ToString());
            info.AddValue("stat", stat);
        }
        #endregion

        public override bool GUIDSerialization
        {
            get
            {
                return false;
            }
        }

        public override bool OnClientReceive(LocalClient client)
        {
            throw new NotImplementedException();
        }

        public override bool OnServerReceive(LocalServer server)
        {
            player.AddStatPoint(stat);
            server.ChangedObjects.Add(player);
            return true;
        }

        public override bool Validate(Level level)
        {
            if(player.freeStatPoints > 0)
            {
                return true;
            }
            return false;
        }
    }

}
