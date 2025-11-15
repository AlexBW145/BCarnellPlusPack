using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BCarnellChars.ItemStuff
{
    public class ITM_TaserPulse : Item
    {
        private LineRenderer chain;
        private int maxChain = 3;
        private Ray ray;
        [SerializeField] internal LayerMask mask;
        [SerializeField] internal Material mat;
        [SerializeField] internal SoundObject use;
        private MovementModifier mod = new MovementModifier(-Vector3.one, 0.4f);

        public override bool Use(PlayerManager pm)
        {
            mod.movementAddend = pm.transform.forward;
            CoreGameManager.Instance.audMan.PlaySingle(use);
            chain = gameObject.AddComponent<LineRenderer>();
            chain.material = mat;
            chain.textureMode = LineTextureMode.Tile;
            chain.loop = false;
            chain.useWorldSpace = true;
            chain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            chain.SetPosition(0, pm.transform.position + Vector3.down + pm.transform.right);
            ray.origin = pm.transform.position;
            ray.direction = CoreGameManager.Instance.GetCamera(pm.playerNumber).transform.forward;
            List<GameObject> hitem = new List<GameObject>();
            float furthestPos = 0f;
            int flag = 0;
            while (flag < maxChain) // Not a very fast BSODA.
            {
                Physics.Raycast(ray, out var hit, float.PositiveInfinity, (LayerMask)LayerMask.GetMask("Default", "Ignore Raycast", "Ignore Raycast B", "Windows"), QueryTriggerInteraction.Ignore);
                RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Min(hit.distance, float.PositiveInfinity), mask, QueryTriggerInteraction.Ignore);
                furthestPos = hit.distance;
                for (int i = 0; i < hits.Count(); i++)
                {
                    if (hits[i].transform.gameObject.CompareTag("Wall") && hits[i].transform.gameObject.activeSelf)
                    {
                        Physics.Raycast(ray, out hit, (hits[i].transform.position - ray.origin).magnitude, (LayerMask)LayerMask.GetMask("Default", "Ignore Raycast", "Ignore Raycast B", "Windows"), QueryTriggerInteraction.Ignore);
                        if (hit.distance > furthestPos)
                            furthestPos = hit.distance;
                    }
                }
                var cast = Physics.CapsuleCastAll(chain.GetPosition(chain.positionCount - 2), ray.GetPoint(furthestPos), 4f, ray.direction, furthestPos, LayerMask.GetMask("NPCs"), QueryTriggerInteraction.Collide);
                bool found = false;
                for (int j = 0; j < cast.Count(); j++)
                {
                    if (cast[j].transform.gameObject.CompareTag("NPC") && !hitem.Contains(cast[j].transform.gameObject))
                    {
                        found = true;
                        hitem.Add(cast[j].transform.gameObject);
                        //Debug.Log("FOUND: " + cast[j].transform.gameObject.name);
                        chain.SetPosition(chain.positionCount - 1, cast[j].transform.transform.position);
                        ray.origin = cast[j].transform.position;
                        //ray.direction = -cast[j].transform.right + cast[j].transform.right;
                        chain.positionCount++;
                        furthestPos = 0f;
                        flag++;
                        cast[j].transform.gameObject.GetComponent<AudioManager>()?.PlaySingle(BasePlugin.bcppAssets.Get<SoundObject>("Items/TASERPulse/Effect"));
                        cast[j].transform.gameObject.GetComponent<ActivityModifier>()?.moveMods.Add(mod);
                        var npc = cast[j].transform.gameObject.GetComponent<NPC>();
                        npc.StartCoroutine(MoveModTime(npc));
                        pm.RuleBreak("Drinking", 9f);
                        break;
                    }
                }
                if (!found)
                    flag = maxChain;
            }
            var lastpos = ray.GetPoint(furthestPos);
            if (chain.positionCount - 1 < maxChain)
                chain.SetPosition(chain.positionCount - 1, new Vector3(lastpos.x, 5f, lastpos.z));
            Destroy(gameObject, 0.1f);
            return true;
        }

        IEnumerator MoveModTime(NPC npc)
        {
            float time = 17f;
            while (time > 0)
            {
                if (npc == null)
                    yield break;
                time -= 1f * (Time.deltaTime * npc.ec.EnvironmentTimeScale);
                yield return null;
            }
            if (npc == null)
                yield break;
            if (npc.Navigator.Entity.ExternalActivity.moveMods.Contains(mod))
                npc.Navigator.Entity.ExternalActivity.moveMods.Remove(mod);
            yield break;
        }
    }
}
