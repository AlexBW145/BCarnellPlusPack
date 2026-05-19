using UnityEngine;

namespace BCarnellChars.Characters.States;

public class RPSGuy_StateBase(RPSGuy rpsguy) : NpcState(rpsguy)
{
    protected RPSGuy rpsGuy = rpsguy;

    public override void OnStateTriggerEnter(Entity entity, Collider other, bool isValid)
    {
        base.OnStateTriggerEnter(entity, other, isValid);
        if (other.CompareTag("NPC") && isValid)
        {
            switch (other.GetComponent<NPC>().Character)
            {
                case Character.Prize:
                    FirstPrize prize = other.GetComponent<FirstPrize>();
                    if (!rpsGuy.Dead && prize.Navigator.speed >= prize.slamSpeed)
                        rpsGuy.fuckingDies();
                    break;
            }
        }
    }
}

public class RPSGuy_Wandering(RPSGuy rpsguy) : RPSGuy_StateBase(rpsguy)
{

    public override void Enter()
    {
        base.Enter();
        npc.looker.Blink();
        if (!npc.Navigator.HasDestination)
            ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
    }

    public override void OnStateTriggerEnter(Entity entity, Collider other, bool isValid)
    {
        base.OnStateTriggerEnter(entity, other, isValid);
        if (other.CompareTag("Player"))
        {
            PlayerManager component = other.GetComponent<PlayerManager>();
            if (!component.Tagged && !component.Invisible && isValid)
            {
                rpsGuy.StartRPS(component);
            }
        }
    }

    public override void DestinationEmpty()
    {
        base.DestinationEmpty();
        ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
    }

    public override void PlayerSighted(PlayerManager player)
    {
        base.PlayerSighted(player);
        if (!player.Tagged)
        {
            rpsGuy.StartPersuingPlayer(player);
        }
    }

    public override void PlayerInSight(PlayerManager player)
    {
        base.PlayerInSight(player);
        if (!player.Tagged)
        {
            rpsGuy.PersuePlayer(player);
        }
    }

    public override void PlayerLost(PlayerManager player)
    {
        base.PlayerLost(player);
        rpsGuy.PlayerTurnAround(player);
    }
}

public class RPSGuy_Playing(RPSGuy rpsguy) : RPSGuy_StateBase(rpsguy)
{

    public override void Enter()
    {
    }

    public override void PlayerLost(PlayerManager player)
    {
        base.PlayerLost(player);
        rpsGuy.EndRPS(false, true);
    }
}

public class RPSGuy_Cooldown(RPSGuy rpsguy, float time) : RPSGuy_StateBase(rpsguy)
{
    private float time = time;

    public override void Enter()
    {
        base.Enter();
        ChangeNavigationState(new NavigationState_WanderRandom(npc, 0));
    }

    public override void Update()
    {
        base.Update();
        time -= Time.deltaTime * npc.TimeScale;
        if (time <= 0f)
        {
            rpsGuy.EndCooldown();
        }
    }
}