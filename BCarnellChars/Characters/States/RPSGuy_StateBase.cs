using UnityEngine;

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

        public override void OnStateTriggerEnter(Collider other, bool isValid)
        {
            base.OnStateTriggerEnter(other, isValid);
            if (other.CompareTag("NPC") && isValid)
            {
                switch (other.GetComponent<NPC>().Character)
                {
                    case Character.Prize:
                        FirstPrize prize = other.GetComponent<FirstPrize>();
                        if (!rpsGuy.Dead && prize.Navigator.speed >= prize.slamSpeed)
                            rpsGuy.fuckingDies();
                        break;
                }
            }
        }
    }
}
