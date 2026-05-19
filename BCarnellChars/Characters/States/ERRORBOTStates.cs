using UnityEngine;

namespace BCarnellChars.Characters.States;

public class ERRORBOT_StateBase(ERRORBOT errbot) : NpcState(errbot)
{
    protected ERRORBOT erbot = errbot;

    public override void OnStateTriggerStay(Entity entity, Collider other, bool isValid)
    {
        if (isValid && entity != null)
            erbot.Jammed(entity);
    }
}

public class ERRORBOT_Cooldown(ERRORBOT errbot, float time) : ERRORBOT_StateBase(errbot)
{
    private float time = time;

    public override void Update()
    {
        base.Update();
        time -= Time.deltaTime * npc.TimeScale;
        if (time <= 0f)
        {
            erbot.EndCooldown();
        }
    }

    public override void OnStateTriggerStay(Entity entity, Collider other, bool isValid)
    {
    }
}

public class ERRORBOT_Active(ERRORBOT errbot) : ERRORBOT_StateBase(errbot)
{
    private float timer = 60f;
    public override void Update()
    {
        base.Update();
        timer -= Time.deltaTime * npc.TimeScale;
        if (timer <= 0f)
            erbot.EndCooldown();
    }

    public override void PlayerSighted(PlayerManager player)
    {
        base.PlayerSighted(player);
        if (!player.Tagged)
        {
            erbot.BecomeEvil(player.itm.HasItem(), player);
        }
    }
}

public class ERRORBOT_NoItemsMode(ERRORBOT errbot, PlayerManager player) : ERRORBOT_StateBase(errbot)
{
    private PlayerManager player = player;

    public override void Update()
    {
        base.Update();
        float distance = (erbot.transform.position - player.transform.position).magnitude;
        if (!erbot.looker.PlayerInSight() && distance >= 45f)
            erbot.Alert(player);
    }
}

public class ERRORBOT_WithItemsMode(ERRORBOT errbot, PlayerManager player) : ERRORBOT_StateBase(errbot)
{
    private PlayerManager player = player;
    private NavigationState targetState;
    private NavigationState_DoNothing noMovementState;

    public override void Enter()
    {
        base.Enter();
        erbot.Navigator.maxSpeed = 0f;
        erbot.Navigator.SetSpeed(25f);
        targetState = new NavigationState_TargetPlayer(erbot, 99, player.transform.position);
        noMovementState = new NavigationState_DoNothing(erbot, 99);
        ChangeNavigationState(targetState);
    }

    public override void Update()
    {
        base.Update();
        float distance = (erbot.transform.position - player.transform.position).magnitude;
        targetState.UpdatePosition(player.transform.position);
        npc.looker.Raycast(player.transform, Mathf.Min(distance, Mathf.Min(npc.looker.distance, npc.ec.MaxRaycast)), Singleton<CoreGameManager>.Instance.GetPlayer(player.playerNumber), erbot.regularMask, out bool _noObstacles);
        if (distance > 10f && !erbot.looker.IsVisible && !player.Invisible)
            ChangeNavigationState(targetState);
        else if (_noObstacles && distance < 10f && erbot.looker.PlayerInSight() && erbot.looker.IsVisible) // _noObstacles is just the usual layer mask for lookers...
            erbot.SayByeByeToYourItems(player);
        else
            ChangeNavigationState(noMovementState);

        // Player's weak point is exposed, they are fucking dead!
        if (!player.itm.HasItem() && distance >= 10f)
        {
            erbot.Navigator.maxSpeed = 0f;
            erbot.Navigator.SetSpeed(0f);
            erbot.BecomeEvil(false, player);
        }
    }
}

public class ERRORBOT_FinalCooldown(ERRORBOT errbot, float time) : ERRORBOT_StateBase(errbot)
{
    private float time = time;

    public override void Update()
    {
        base.Update();
        time -= Time.deltaTime * npc.TimeScale;
        if (time <= 0f)
        {
            erbot.StartCooldown();
        }
    }

    public override void OnStateTriggerStay(Entity entity, Collider other, bool isValid)
    {
    }
}