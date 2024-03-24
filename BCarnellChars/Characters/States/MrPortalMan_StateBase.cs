using System;
using System.Collections.Generic;
using System.Text;

namespace BCarnellChars.Characters.States
{
    public class MrPortalMan_StateBase : NpcState
    {
        protected MrPortalMan portalMan;

        public MrPortalMan_StateBase(MrPortalMan mrportal)
            : base(mrportal)
        {
            portalMan = mrportal;
        }
    }
}
