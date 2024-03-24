﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BCarnellChars.Characters.States
{
    public class ERRORBOT_StateBase : NpcState
    {
        protected ERRORBOT erbot;

        public ERRORBOT_StateBase(NPC npc, ERRORBOT errbot)
            : base(npc)
        {
            erbot = errbot;
        }
    }
}
