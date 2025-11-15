using BCarnellChars.GeneratorStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BCarnellChars.ItemStuff;

public class ITM_AnyportalGuy : Item
{
    [SerializeField] internal SoundObject wrongPlacement, destroySnd;
    public override bool Use(PlayerManager pm)
    {
        bool found = false;
        if (FindObjectsOfType<ITM_AnyportalOutput>().Count(x => x.Ready && x.Owner == pm) > 0)
        {
            List<ITM_AnyportalOutput> placedOutputs = FindObjectsOfType<ITM_AnyportalOutput>().ToList().FindAll(x => x.Ready && x.Owner == pm);
            int index = Mathf.RoundToInt(UnityEngine.Random.Range(0f, placedOutputs.Count - 1));
            pm.Teleport(pm.ec.CellFromPosition(placedOutputs[index].transform.position).CenterWorldPosition);
            pm.transform.rotation = placedOutputs[index].transform.rotation;
            placedOutputs[index].Break();
            found = true;
        }
        else if (FindObjectsOfType<CryingPortal>().Count() > 0)
        {
            List<CryingPortal> existingOutputs = FindObjectsOfType<CryingPortal>().ToList();
            int index = Mathf.RoundToInt(UnityEngine.Random.Range(0f, existingOutputs.Count - 1));
            pm.Teleport(existingOutputs[index].CellPos);
            pm.transform.rotation = existingOutputs[index].PortalRotat;
            found = true;
        }
        CoreGameManager.Instance.audMan.PlaySingle(found ? destroySnd : wrongPlacement);
        return found;
    }
}
