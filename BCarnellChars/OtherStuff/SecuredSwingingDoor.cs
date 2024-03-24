using MTM101BaldAPI.Registers;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BCarnellChars.OtherStuff
{
    public class SecuredSwingingDoor : MonoBehaviour, IItemAcceptor
    {
        private SwingDoor swingDoor;

        public Material[] originalOverlays = new Material[0];
        private bool lockOnStart = true;

        private void Start()
        {
            swingDoor = gameObject.GetComponent<SwingDoor>();
            if (swingDoor == null)
                return;
            if (swingDoor.ec != null)
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
            for (int i = 0; i < swingDoor.overlayLocked.Length; i++)
            {
                swingDoor.overlayLocked[i] = originalOverlays[i];
            }

            swingDoor.Unlock();
            //player.itm.AddItem(ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey")).value);
            Destroy(this);
        }

        public bool ItemFits(Items item)
        {
            return item == EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey");
        }
    }
}
