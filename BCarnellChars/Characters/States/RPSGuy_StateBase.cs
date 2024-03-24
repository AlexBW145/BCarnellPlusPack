using System;
using System.Collections.Generic;
using System.Text;

namespace BCarnellChars.Characters.States
{
    public class RPSGuy_StateBase : NpcState
    {
        protected RPSGuy rpsGuy;

        public RPSGuy_StateBase(NPC npc, RPSGuy rpsguy)
            : base(npc)
        {
            this.rpsGuy = rpsguy;
        }
    }
}
