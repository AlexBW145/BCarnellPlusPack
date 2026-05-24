using BCarnellChars.OtherStuff;
using MTM101BaldAPI;
using System.Linq;
using UnityEngine;

namespace BCarnellChars.OtherStuff
{
    public class SecuredSwingingDoor : MonoBehaviour, IItemAcceptor
    {
        [SerializeField] internal SwingDoor swingDoor;

        [SerializeField] internal Material[] originalOverlays = new Material[0];
        private int attemptsofBreak = 0, maximumAttempts = 5;
        public void Initialize()
        {
            if (originalOverlays.Length == 0)
            {
                originalOverlays = new Material[swingDoor.overlayLocked.Length];
                for (int i = 0; i < swingDoor.overlayLocked.Length; i++)
                {
                    originalOverlays[i] = swingDoor.overlayLocked[i];
                    swingDoor.overlayLocked[i] = BCPPPlugin.securedYellowSwingingDoor;
                }
            }
            swingDoor.Lock(true);
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

        internal void UnlockFully()
        {
            for (int i = 0; i < swingDoor.overlayLocked.Length; i++)
                swingDoor.overlayLocked[i] = originalOverlays[i];

            swingDoor.Unlock();
            //player.itm.AddItem(ItemMetaStorage.Instance.FindByEnum(EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey")).value);
            Destroy(this);
        }

        private static readonly string[] hammers = new string[] { "BHammer".ToLower(), "Hammer".ToLower() };
        public bool ItemFits(Items item)
        {
            if (hammers.Contains(item.ToStringExtended().ToLower()))
                AttemptBreak();
            else if (item == EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey") || item.ToStringExtended().ToLower() == "IOU".ToLower())
                UnlockFully();
            return item == EnumExtensions.GetFromExtendedName<Items>("UnsecuredYellowKey") || item.ToStringExtended().ToLower() == "IOU".ToLower() || hammers.Contains(item.ToStringExtended().ToLower());
        }
    }
}

public class SwingDoor_Secured : SwingDoor
{
    [SerializeField]
    internal SecuredSwingingDoor securedDoor;

    public override void Initialize()
    {
        base.Initialize();
        securedDoor.Initialize();
        /*if (ec.TrapCheck(aTile, true, out _) || ec.TrapCheck(bTile, true, out _))
            securedDoor.UnlockFully();*/
    }
}