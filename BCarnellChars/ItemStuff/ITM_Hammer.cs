using BCarnellChars.OtherStuff;
using System.Linq;
using UnityEngine;

namespace BCarnellChars.ItemStuff;

public class ITM_Hammer : Item
{
    private RaycastHit hit;
    [SerializeField, Range(0,3)] internal int duration = 3;

    public override bool Use(PlayerManager pm)
    {
        bool accepted = false;
        bool instaDestroy = false;
        //Debug.Log(duration);
        if (FindObjectOfType<RockPaperScissors>(false) != null) {
            var rps = FindObjectsOfType<RockPaperScissors>().ToList();
            while (rps.Count > 0)
            {
                rps[0].death();
                rps.RemoveAt(0);
            }
            accepted = true;
        }
        bool raycasted = Physics.Raycast(pm.transform.position, CoreGameManager.Instance.GetCamera(pm.playerNumber).transform.forward, out hit, Mathf.Min(pm.pc.reach, pm.ec.MaxRaycast), LayerMask.GetMask("Default", "Block Raycast", "NPCs", "Windows"), QueryTriggerInteraction.Collide);
        if (raycasted && !accepted)
        {
            if (hit.transform.gameObject.GetComponent<IItemAcceptor>() != null)
            {
                IItemAcceptor[] components = hit.transform.GetComponents<IItemAcceptor>();
                foreach (IItemAcceptor itemAcceptor in components)
                {
                    if (itemAcceptor.ItemFits(BCPPPlugin.bcppAssets.Get<ItemObject>("Items/BHammer").itemType) && !accepted)
                    {
                        itemAcceptor.InsertItem(pm, pm.ec);
                        accepted = true;
                        if (hit.transform.GetComponent<SecuredSwingingDoor>() != null)
                            instaDestroy = true;
                    }
                }
            }
            if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Windows"))
            {
                Window component = hit.transform.GetComponent<Window>();
                if (component != null && !component.IsOpen)
                {
                    component.Break(true);
                    if (component.IsOpen)
                        accepted = true;
                }
            }
            else if (hit.transform.CompareTag("StandardDoor"))
            {
                Door component = hit.transform.GetComponent<Door>();
                if (component != null && component.locked)
                {
                    component.Unlock();
                    component.OpenTimed(10f, true);
                    accepted = true;
                    component.transform.GetComponentInChildren<AudioManager>()?.PlaySingle(BCPPPlugin.bcppAssets.Get<SoundObject>("Items/UnsafeHammer/DestroyDoor"));
                }
            }
            else if (hit.transform.gameObject.GetComponent<CoinDoor>() != null)
            {
                hit.transform.gameObject.GetComponent<CoinDoor>().InsertItem(pm, pm.ec);
                accepted = true;
                instaDestroy = true;
            }
        }
        Destroy(gameObject);
        if (accepted)
        {
            if (duration > 1 && duration <= 3 && !instaDestroy)
                pm.itm.SetItem(duration == 2 ? BCPPPlugin.bcppAssets.Get<ItemObject>("Items/BHammerLow") : BCPPPlugin.bcppAssets.Get<ItemObject>("Items/BHammerMid"), pm.itm.selectedItem);
            else if (duration <= 1 || instaDestroy)
                CoreGameManager.Instance.audMan.PlaySingle(BCPPPlugin.bcppAssets.Get<SoundObject>("Items/UnsafeHammer/DurationBreak")); 
            return (duration <= 1 || instaDestroy);
        }
        return false;
    }
}
