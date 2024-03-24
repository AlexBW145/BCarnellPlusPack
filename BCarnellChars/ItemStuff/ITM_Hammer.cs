using BCarnellChars.Characters;
using MTM101BaldAPI;
using MTM101BaldAPI.Reflection;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BCarnellChars.ItemStuff
{
    public class ITM_Hammer : Item
    {
        private RaycastHit hit;

        public override bool Use(PlayerManager pm)
        {
            Destroy(gameObject);
            if (Physics.Raycast(pm.transform.position, Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).transform.forward, out hit, pm.pc.reach, LayerMask.GetMask("Default", "Windows")))
            {
                Window component = hit.transform.GetComponent<Window>();
                if (component != null && !(bool)component.ReflectionGetVariable("broken"))
                {
                    component.Break(true);
                    return true;
                }
            }
            if (pm.ec.Npcs.Find(x => x.Character == EnumExtensions.GetFromExtendedName<Character>("RPSGuy")))
            {
                RPSGuy component2 = pm.ec.Npcs.Find(x => x.Character == EnumExtensions.GetFromExtendedName<Character>("RPSGuy")).gameObject.GetComponent<RPSGuy>();
                if (component2 != null && component2.Playing)
                {
                    component2.fuckingDies();
                    return true;
                }
            }
            return false;
        }
    }
}
