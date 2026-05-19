using System.Linq;
using UnityEngine;

namespace BCarnellChars.Characters.States;

public class MrPortalMan_StateBase(MrPortalMan mrportal) : NpcState(mrportal)
{
    protected MrPortalMan portalMan = mrportal;
}

public class MrPortalMan_Cooldown(MrPortalMan mrportal, float cool) : MrPortalMan_StateBase(mrportal)
{
    private float cooldown = cool;

    public override void Update()
    {
        base.Update();
        if (cooldown > 0)
            cooldown -= Time.deltaTime * npc.TimeScale;
        else
            portalMan.StartToDevour();

    }
}

public class MrPortalMan_Wandering(MrPortalMan mrportal) : MrPortalMan_StateBase(mrportal)
{
    private Entity currentTarget;
    private int numberOfFoodNeed, currentAteFood = 0;

    public override void Initialize()
    {
        base.Initialize();
        var npcs = portalMan.ec.Npcs.Where(npc => npc != null && npc.Character != Character.Null).ToList();
        numberOfFoodNeed = Mathf.RoundToInt(npcs.Count - (npcs.Count / 1.5f));
    }

    public override void Update()
    {
        if (currentAteFood >= numberOfFoodNeed)
            portalMan.Rest();

        foreach (NPC npc in portalMan.ec.Npcs.Where(npc => npc != null && npc.Character != Character.Null && npc is not MrPortalMan && npc.spriteBase.activeSelf && npc.spriteRenderer.Any(s => s.gameObject.activeSelf) && npc.spriteRenderer.Any(s => s.enabled) && npc.baseTrigger.Count() > 0 && npc.Navigator.enabled))
        {
            portalMan.looker.Raycast(npc.transform, Mathf.Min((portalMan.transform.position - npc.transform.position).magnitude + npc.Navigator.Velocity.magnitude, portalMan.looker.distance, portalMan.ec.MaxRaycast), out bool _sighted);
            if (_sighted && currentTarget == null)
            {
                currentTarget = npc.Navigator.Entity;
                ChangeNavigationState(new NavigationState_TargetPosition(portalMan, 63, currentTarget.transform.position));
            }
        }
        if (currentTarget != null)
        {
            var npc = currentTarget?.GetComponent<NPC>();
            if (npc != null)
            {
                portalMan.looker.Raycast(currentTarget.transform, Mathf.Min((portalMan.transform.position - currentTarget.transform.position).magnitude + npc.Navigator.Velocity.magnitude, portalMan.looker.distance, portalMan.ec.MaxRaycast), out bool _sighted);
                if (!npc.spriteBase.activeSelf || !npc.spriteRenderer.Any(s => s.gameObject.activeSelf)
                    || !npc.spriteRenderer.Any(s => s.enabled) || npc.baseTrigger.Count() <= 0 || !npc.Navigator.enabled)
                    currentTarget = null;
                else if (_sighted && portalMan.Navigator.HasDestination)
                    currentNavigationState.UpdatePosition(currentTarget.transform.position);
            }
        }
        else if (!portalMan.Navigator.HasDestination)
            ChangeNavigationState(new NavigationState_WanderRandom(portalMan, 0));

        if (!portalMan.Navigator.HasDestination && currentTarget == null)
            portalMan.Questioning();
    }

    public override void PlayerSighted(PlayerManager player)
    {
        if (currentTarget == null)
            currentTarget = player.plm.Entity;
    }

    public override void DestinationEmpty()
    {
        if (currentTarget != null)
        {
            portalMan.looker.Raycast(currentTarget.transform, Mathf.Min((portalMan.transform.position - currentTarget.transform.position).magnitude, portalMan.looker.distance, portalMan.ec.MaxRaycast), out bool _sighted);
            if (!_sighted)
                currentTarget = null;
        }
    }
    public override void OnStateTriggerStay(Entity entity, Collider other, bool isValid)
    {
        if (other.CompareTag("NPC") || other.CompareTag("Player"))
        {
            portalMan.TeleportAnIdiot(entity);
            if (entity == currentTarget)
            {
                currentTarget = null;
                portalMan.Navigator.ClearDestination();
            }
            if (other.GetComponent<NPC>()?.Character != Character.Null)
                currentAteFood++;
        }
        else if (entity != null)
            portalMan.TeleportAnIdiot(entity);
    }
}