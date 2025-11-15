using UnityEngine;

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

        public override void OnStateTriggerStay(Collider other, bool isValid)
        {
            if (isValid)
                erbot.Jammed(other);
        }
    }
}
