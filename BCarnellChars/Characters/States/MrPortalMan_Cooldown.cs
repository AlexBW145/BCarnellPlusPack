using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BCarnellChars.Characters.States
{
    public class MrPortalMan_Cooldown : MrPortalMan_StateBase
    {
        public MrPortalMan_Cooldown(MrPortalMan mrportal, float cool)
            : base(mrportal)
        {
            cooldown = cool;
        }

        private float cooldown;

        public override void Update()
        {
            base.Update();
            if (cooldown > 0)
                cooldown -= Time.deltaTime * npc.TimeScale;
            else
                portalMan.StartToDevour();

        }
    }
}
