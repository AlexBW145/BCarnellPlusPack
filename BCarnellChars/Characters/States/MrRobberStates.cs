using System.Linq;
using UnityEngine;

namespace BCarnellChars.Characters.States;

public class MrRobber_StateBase(MrRobber robber) : NpcState(robber)
{
    protected MrRobber robber = robber;
}

public class MrRobber_Hunting(MrRobber robber) : MrRobber_StateBase(robber)
{
    private float timeInSight = 0f;

    public override void Initialize()
    {
        base.Initialize();
        robber.Navigator.maxSpeed = 15f;
        robber.Navigator.SetSpeed(15f);
        robber.Navigator.SetRoomAvoidance(true);
        ChangeNavigationState(new NavigationState_WanderRandom(robber, 1));
    }

    // Never open doors, ever.
    public override void DoorHit(StandardDoor door) { }

    public override void Update()
    {
        bool sighted = false;
        foreach (NPC npc in robber.ec.Npcs.Where(x => x.gameObject != robber.gameObject))
        {
            robber.looker.Raycast(npc.transform, float.PositiveInfinity, out var _sighted);
            if (_sighted || Vector3.Distance(robber.transform.position, npc.transform.position) <= 180f)
                sighted = true;
        }
        if (npc.ec.timeOut && sighted)
            sighted = !(npc.ec.ContainsCoordinates(IntVector2.GetGridPosition(npc.transform.position)) && npc.ec.LightLevel(npc.transform.position) <= 0.1f);

        robber.spriteRenderer[0].color = sighted ? Color.clear : Color.white;
        if (robber.Navigator.Entity.Visible && sighted)
            robber.Navigator.Entity.SetVisible(false);
        else if (!robber.Navigator.Entity.Visible && !sighted)
            robber.Navigator.Entity.SetVisible(true);
    }

    public override void PlayerInSight(PlayerManager player)
    {
        if (robber.spriteRenderer[0].color != Color.clear)
        {
            if (timeInSight < 1)
                timeInSight += .5f * (Time.deltaTime * robber.TimeScale);
            else
                robber.StartCoroutine(robber.GoNuts());
        }
    }

    public override void DestinationEmpty()
    {
        base.DestinationEmpty();
        ChangeNavigationState(new NavigationState_WanderRandom(npc, 1));
    }
}

public class MrRobber_Crazy(MrRobber robber) : MrRobber_StateBase(robber)
{
    public override void Initialize()
    {
        base.Initialize();
        robber.Navigator.maxSpeed = 0f;
        robber.Navigator.SetSpeed(0f);
        robber.Navigator.SetRoomAvoidance(false);
    }

    public override void Update()
    {
        if (robber.Navigator.maxSpeed == 0f)
            return;
        robber.Navigator.maxSpeed = robber.Navigator.speed + 4f;
        robber.Navigator.SetSpeed(robber.Navigator.maxSpeed);
    }

    public override void PlayerInSight(PlayerManager player)
    {
        ChangeNavigationState(new NavigationState_TargetPlayer(npc, 99, player.transform.position));
    }

    public override void DestinationEmpty()
    {
        base.DestinationEmpty();
        ChangeNavigationState(new NavigationState_WanderRandom(npc, 1));
    }

    public override void OnStateTriggerEnter(Entity entity, Collider other, bool isValid)
    {
        if (!other.CompareTag("Player") || robber.Navigator.maxSpeed == 0f || !isValid)
            return;

        robber.DoneFor(other.GetComponent<PlayerManager>());
    }
}

public class MrRobber_DoNothing(MrRobber robber) : MrRobber_StateBase(robber)
{
    public override void Initialize()
    {
        base.Initialize();
        robber.Navigator.maxSpeed = 15f;
        robber.Navigator.SetSpeed(15f);
        robber.Navigator.SetRoomAvoidance(true);
        ChangeNavigationState(new NavigationState_WanderRandom(npc, 1));
    }

    public override void DestinationEmpty()
    {
        base.DestinationEmpty();
        ChangeNavigationState(new NavigationState_WanderRandom(npc, 1));
    }
}