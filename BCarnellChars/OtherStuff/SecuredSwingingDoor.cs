using MTM101BaldAPI.Registers;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;

namespace BCarnellChars.OtherStuff
{
    public class SecuredSwingingDoor : MonoBehaviour, IItemAcceptor
    {
        private SwingDoor swingDoor;

        public Material[] originalOverlays = new Material[0];
        private bool lockOnStart = true;
        private int attemptsofBreak = 0, maximumAttempts = 5;

        private void Start()
        {
            swingDoor = gameObject.GetComponent<SwingDoor>();
            if (swingDoor == null || swingDoor.ec == null)
                return;
                
            gameObject.GetComponent<AudioManager>().enabled = true;

            if (originalOverlays.Length == 0)
            {
                originalOverlays = new Material[swingDoor.overlayLocked.Length];
                for (int i = 0; i < swingDoor.overlayLocked.Length; i++)
                {
                    originalOverlays[i] = swingDoor.overlayLocked[i];
                    swingDoor.overlayLocked[i] = BasePlugin.securedYellowSwingingDoor;
                }
            }

            if (lockOnStart)
            {
                swingDoor.Lock(true);
                lockOnStart = false;
            }
        }

        public void InsertItem(PlayerManager player, EnvironmentController ec)
        {
        }
        
        private void AttemptBreak()
        {
            attemptsofBreak++;
            if (attemptsofBreak > UnityEngine.Random.Range(attemptsofBreak, maximumAttempts+2))
                UnlockFully();
        }

        private void UnlockFully()
        {
            for (int i = 0; i < swingDoor.overlayLocked.Length; i++)
                swingDoor.overlayLocked[i] = originalOverlays[i];

            swingDoor.Unlock();
            //player.itm.AddItem(ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey")).value);
            Destroy(this);
        }

        public bool ItemFits(Items item)
        {
            if (new string[] { "BHammer".ToLower(), "Hammer".ToLower() }.Contains(item.ToStringExtended().ToLower()))
                AttemptBreak();
            else if (item == EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey") || item.ToStringExtended().ToLower() == "IOU".ToLower())
                UnlockFully();
            return item == EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey") || item.ToStringExtended().ToLower() == "IOU".ToLower() || new string[] { "BHammer".ToLower(), "Hammer".ToLower() }.Contains(item.ToStringExtended().ToLower());
        }
    }
}
