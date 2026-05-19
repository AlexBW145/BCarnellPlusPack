using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace BCarnellChars.Characters.States;

public class BCarnellBoss_StateBase(BCarnellBoss carnell) : NpcState(carnell)
{
    protected BCarnellBoss bcarnell = carnell;

    public override void Hear(GameObject source, Vector3 position, int value)
    {
        bcarnell.Hear(source, position, value, true);
    }

    public override void PlayerInSight(PlayerManager player)
    {
        if (!IsActuallyHidden(player))
        {
            bcarnell.ClearSoundLocations();
            bcarnell.Hear(null, player.transform.position, 127, false);
        }
    }

    public override void DestinationEmpty()
    {
        if (bcarnell.CurrentDestinationInteraction != null && bcarnell.CurrentDestinationInteraction.Check(bcarnell))
        {
            bcarnell.CurrentDestinationInteraction.Trigger(bcarnell);
            bcarnell.ClearDestinationInteraction();
        }
        if (!bcarnell.Attacking)
            bcarnell.UpdateSoundTarget();
    }

    protected static readonly FieldInfo
        _hiddenByLight = AccessTools.DeclaredField(typeof(PlayerManager), "hiddenByLight"),
        _hides = AccessTools.DeclaredField(typeof(Entity), "hides");
    protected bool IsActuallyHidden(PlayerManager player)
    {
        if ((bool)_hiddenByLight.GetValue(player) && (int)_hides.GetValue(player.plm.Entity) > 2)
            return true;
        return !(bool)_hiddenByLight.GetValue(player) && player.Invisible;
    }

    protected virtual void ActivateSlapAnimation()
    {
        bcarnell.SlapNormal();
    }

    public override void DoorHit(StandardDoor door)
    {
        if (door.locked)
        {
            door.Unlock();
            door.OpenTimed(5f, false);
        }
        else
        {
            base.DoorHit(door);
        }
    }
}

public class BCarnellBoss_Speaking(BCarnellBoss carnell) : BCarnellBoss_StateBase(carnell)
{
    private static readonly FieldInfo _audTimescale = AccessTools.DeclaredField(typeof(PropagatedAudioManager), "pitchTimeScaleType");

    public override void Enter()
    {
        base.Enter();
        _audTimescale.SetValue(bcarnell.audMan as PropagatedAudioManager, TimeScaleType.Null);
        bcarnell.Navigator.Entity.SetTrigger(false);
        bcarnell.Navigator.Entity.SetFrozen(true);
    }

    public override void Exit()
    {
        base.Exit();
        _audTimescale.SetValue(bcarnell.audMan, TimeScaleType.Npc);
        bcarnell.Navigator.Entity.SetTrigger(true);
        bcarnell.Navigator.Entity.SetFrozen(false);
    }

    public override void PlayerInSight(PlayerManager player)
    {
    }

    public override void Hear(GameObject source, Vector3 position, int value)
    {
    }

    public override void DestinationEmpty()
    {
    }
}

public class BCarnellBoss_Chase(BCarnellBoss carnell) : BCarnellBoss_StateBase(carnell)
{
    protected float delayTimer;

    public override void Enter()
    {
        base.Enter();
        delayTimer = bcarnell.Delay;
        bcarnell.ResetSlapDistance();
    }

    public override void OnStateTriggerStay(Entity entity, Collider other, bool isValid)
    {
        base.OnStateTriggerStay(entity, other, isValid);
        if (!other.CompareTag("Player"))
            return;

        bcarnell.looker.Raycast(other.transform, Vector3.Magnitude(bcarnell.transform.position - other.transform.position), out var targetSighted);
        if (!targetSighted)
            return;

        PlayerManager component = other.GetComponent<PlayerManager>();
        ItemManager itm = component.itm;
        if (!component.invincible && isValid)
            bcarnell.CaughtPlayer(component);
    }

    public override void Update()
    {
        bcarnell.UpdateSlapDistance();
        if (!bcarnell.Attacking)
        {
            delayTimer -= Time.deltaTime * npc.TimeScale;
            if (delayTimer <= 0f)
            {
                bcarnell.Slap();
                ActivateSlapAnimation();
                delayTimer = bcarnell.Delay;
            }
        }
    }

    public override void PlayerSighted(PlayerManager player)
    {
        if (!IsActuallyHidden(player) && !bcarnell.Attacking && bcarnell.AbleToAttack)
            bcarnell.Attack();
    }
}

public class BCarnellBoss_ChaseIntense(BCarnellBoss carnell) : BCarnellBoss_Chase(carnell)
{

    public override void PlayerSighted(PlayerManager player)
    {
        if (!bcarnell.Attacking && bcarnell.AbleToAttack && !IsActuallyHidden(player))
            bcarnell.Attack();
    }

    public override void PlayerInSight(PlayerManager player)
    {
        if (!bcarnell.Attacking && !IsActuallyHidden(player))
            base.PlayerInSight(player);
    }

    public override void DestinationEmpty()
    {
        var players = bcarnell.ec.Players;
        if (Vector3.Distance(bcarnell.transform.position, players[0].transform.position) >= 65 && bcarnell.AbleToAttack)
            bcarnell.Attack();
        else
            base.DestinationEmpty();
    }

    protected override void ActivateSlapAnimation()
    {
    }
}