﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wink
{
    class EndTurnEvent : Event
    {
        public EndTurnEvent(Sender sender) : base(sender)
        {

        }

        public override void OnClientReceive(LocalClient client)
        {
            throw new NotImplementedException();
        }

        public override void OnServerReceive(LocalServer server)
        {
            throw new NotImplementedException();
        }

        public override bool Validate(Level level)
        {
            throw new NotImplementedException();
        }
    }
}