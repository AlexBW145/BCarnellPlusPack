using HarmonyLib;
using MTM101BaldAPI;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BCarnellChars.Characters.States;

public class PrototypeBot_StateBase(PrototypeBot bot) : NpcState(bot)
{
    protected PrototypeBot prototypebot = bot;
    protected internal bool flagged = false; // Was the bot teleporting infront of you a threat?
    protected static readonly FieldInfo
        _hiddenByLight = AccessTools.DeclaredField(typeof(PlayerManager), "hiddenByLight"),
        _hides = AccessTools.DeclaredField(typeof(Entity), "hides");
    protected bool IsActuallyHidden(PlayerManager player)
    {
        if ((bool)_hiddenByLight.GetValue(player) && (int)_hides.GetValue(player.plm.Entity) > 2)
            return true;
        return !(bool)_hiddenByLight.GetValue(player) && player.Invisible;
    }

    public override void OnStateTriggerStay(Entity entity, Collider other, bool isValid)
    {
        if (!other.CompareTag("Player") || flagged)
            return;

        prototypebot.looker.Raycast(other.transform, Vector3.Magnitude(prototypebot.transform.position - other.transform.position), out var targetSighted);
        if (!targetSighted)
        {
            return;
        }

        PlayerManager component = other.GetComponent<PlayerManager>();
        if (!component.invincible && isValid)
        {
            prototypebot.CaughtPlayer(component);
        }
    }

    public override void Hear(GameObject source, Vector3 position, int value)
    {
        if (value >= 112 && Mathf.RoundToInt(Vector3.Distance(npc.transform.position, position)) < (245 + value))
        {
            if (readyEnumerator == null)
            {
                readyEnumerator = IsReady(position);
                prototypebot.StartCoroutine(readyEnumerator);
            }
        }
        else if (!npc.Navigator.HasDestination && npc.behaviorStateMachine.currentState.GetType().Equals(typeof(PrototypeBot_StateBase)))
            prototypebot.MechFunctInvoke();
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

    protected void Rumble(float duration)
    {
        float num = Vector3.Distance(npc.transform.position, prototypebot.players[0].transform.position);
        if (num < 100f)
        {
            float signedAngle = Vector3.SignedAngle(npc.transform.position - prototypebot.players[0].transform.position, prototypebot.players[0].transform.forward, Vector3.up);
            InputManager.Instance.Rumble(1f - num / 100f, duration, signedAngle);
        }
    }

    protected IEnumerator readyEnumerator;
    internal virtual IEnumerator IsReady(Vector3 pos)
    {
        yield break;
    }
}

public class PrototypeBot_Stunned(PrototypeBot bot) : PrototypeBot_StateBase(bot)
{
    public override void OnStateTriggerStay(Entity entity, Collider other, bool isValid)
    {
    }
    public override void Hear(GameObject source, Vector3 position, int value)
    {
    }
    public override void Enter()
    {
        base.Enter();
        prototypebot.animator.SetDefaultAnimation("Idle", 2f, true);
        npc.Navigator.SetSpeed(0f);
        npc.Navigator.maxSpeed = 0f;
        npc.behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(npc, 99));
    }
}

public class PrototypeBot_Ceiling(PrototypeBot bot) : PrototypeBot_StateBase(bot)
{
    private PlayerManager player;
    private int maxattempts = 30;
    private float elaspedTime;

    public override void Enter()
    {
        base.Enter();
        flagged = true;
        prototypebot.animator.SetDefaultAnimation("Climbing", 1f, true);
        npc.Navigator.maxSpeed = 30;
        npc.Navigator.SetSpeed(30);
        npc.Navigator.Entity.SetGrounded(false);
        for (int k = 0; k < CoreGameManager.Instance.setPlayers; k++)
            npc.Navigator.Entity.IgnoreEntity(prototypebot.players[k].plm.Entity, true);
        player = prototypebot.players[0];
        bool flag = false;
        int attempts = 0;
        while (!flag && attempts < maxattempts)
        {
            List<Cell> hall = npc.ec.mainHall.cells;
            npc.Navigator.Entity.Teleport(hall[Mathf.RoundToInt(UnityEngine.Random.Range(0f, hall.Count - 1))].CenterWorldPosition);
            flag = true;
            attempts++;
            if (!flag)
                break;
            if (Vector3.Distance(npc.transform.position, player.transform.position) > ((250f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z))
                || Vector3.Distance(npc.transform.position, player.transform.position) <= ((180f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z)))
                flag = false;
        }
        if (attempts >= maxattempts)
            prototypebot.MechFunctInvoke();
        flagged = false;
    }
    public override void Update()
    {
        if (player == null || flagged)
            return;
        elaspedTime += Time.deltaTime * npc.TimeScale;
        if (elaspedTime >= 55f)
        {
            prototypebot.MechFunctInvoke();
            return;
        }
        if (player.plm.Entity.CurrentRoom?.type != RoomType.Hall || IsActuallyHidden(player)) // Is this fair enough??
        {
            if (npc.Navigator.HasDestination && npc.ec.CellFromPosition(player.transform.position)?.room?.type != RoomType.Hall
                && !npc.behaviorStateMachine.CurrentNavigationState.GetType().Equals(typeof(NavigationState_TargetPosition)))
                npc.Navigator.ClearDestination();
            if (!npc.Navigator.HasDestination || npc.ec.CellFromPosition(npc.Navigator.CurrentDestination)?.room?.type != RoomType.Hall)
            {
                List<Cell> hall = npc.ec.mainHall.cells;
                ChangeNavigationState(new NavigationState_TargetPosition(npc, 99, hall[Mathf.RoundToInt(UnityEngine.Random.Range(0f, hall.Count - 1))].CenterWorldPosition));
            }
            return;
        }
        float distance = Vector3.Distance(npc.transform.position, player.transform.position);
        if (distance <= ((400f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z)) && !IsActuallyHidden(player))
        {
            ChangeNavigationState(new NavigationState_TargetPlayer(npc, 99, player.transform.position));
            Rumble(0.1f);
            if (distance <= ((80f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z)) && npc.looker.PlayerInSight(player))
            {
                if (readyEnumerator == null)
                {
                    readyEnumerator = IsReady(player.transform.position);
                    prototypebot.StartCoroutine(readyEnumerator);
                }
            }
        }
        else
        {
            bool flag = false;
            while (!flag)
            {
                List<Cell> hall = npc.ec.mainHall.cells;
                npc.Navigator.Entity.Teleport(hall[Mathf.RoundToInt(UnityEngine.Random.Range(0, hall.Count - 1))].CenterWorldPosition);
                flag = true;
                if (!flag)
                    break;
                if (Vector3.Distance(npc.transform.position, prototypebot.players[0].transform.position) > ((250f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z))
                    && Vector3.Distance(npc.transform.position, prototypebot.players[0].transform.position) <= ((180f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z)))
                    flag = false;
            }
        }
    }

    public override void DestinationEmpty()
    {
        if (player.plm.Entity.CurrentRoom?.type != RoomType.Hall | IsActuallyHidden(player)) // Is this fair enough??
        {
            List<Cell> hall = npc.ec.mainHall.cells;
            ChangeNavigationState(new NavigationState_TargetPosition(npc, 99, hall[Mathf.RoundToInt(UnityEngine.Random.Range(0f, hall.Count - 1))].CenterWorldPosition));
        }
    }

    internal override IEnumerator IsReady(Vector3 pos)
    {
        prototypebot.AudMan.FlushQueue(true);
        npc.Navigator.maxSpeed = 5f;
        npc.Navigator.SetSpeed(5f);
        prototypebot.animator.Play("ClimbOut", 1f);
        yield return null;
        while (prototypebot.animator.AnimationId == "ClimbOut")
        {
            if (prototypebot.animator.AnimationFrame == 7)
            {
                npc.spriteRenderer[0].SetSpriteRotation(180f);
                npc.Navigator.Entity.SetBaseRotation(180f);
            }
            yield return null;
        }
        prototypebot.behaviorStateMachine.ChangeState(new PrototypeBot_Chase(prototypebot, pos));
    }

    public override void PlayerLost(PlayerManager player)
    {
        if (Vector3.Distance(npc.transform.position, player.transform.position) <= ((40f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z)))
            prototypebot.MechFunctInvoke();
    }

    public override void Exit()
    {
        npc.spriteRenderer[0].SetSpriteRotation(0f);
        npc.Navigator.Entity.SetBaseRotation(0f);
        npc.Navigator.Entity.SetGrounded(true);
        for (int k = 0; k < CoreGameManager.Instance.setPlayers; k++)
            npc.Navigator.Entity.IgnoreEntity(prototypebot.players[k].plm.Entity, false);
    }
}

public class PrototypeBot_Corner(PrototypeBot bot) : PrototypeBot_StateBase(bot)
{
    private int maxattempts = 30;
    public override void Enter()
    {
        base.Enter();
        flagged = true;
        prototypebot.animator.SetDefaultAnimation("Running", 1.9f, true);
        npc.Navigator.maxSpeed = 31;
        npc.Navigator.SetSpeed(31);
        npc.Navigator.Entity.SetTrigger(false); // Don't want accidents happen...
        bool flag = false;
        int attempts = 0;
        while (!flag & attempts < maxattempts)
        {
            List<Cell> hall = npc.ec.mainHall.cells;
            npc.Navigator.Entity.Teleport(hall[Mathf.RoundToInt(UnityEngine.Random.Range(0f, hall.Count - 1))].CenterWorldPosition);
            flag = true;
            attempts++;
            for (int k = 0; k < CoreGameManager.Instance.setPlayers; k++)
            {
                if (!flag)
                    break;
                if (Vector3.Distance(npc.transform.position, prototypebot.players[k].transform.position) <= 150f && Vector3.Distance(npc.transform.position, prototypebot.players[k].transform.position) > 250f)
                    flag = false;
            }
        }
        if (attempts >= maxattempts)
            prototypebot.MechFunctInvoke();
        else
            flagged = false;
        npc.Navigator.Entity.SetTrigger(true);
        player = prototypebot.players[Mathf.RoundToInt(UnityEngine.Random.Range(0f, prototypebot.players.Length - 1))];
    }

    private PlayerManager player;
    public override void Update()
    {
        if (player)
        {
            if (player.plm.Entity.CurrentRoom?.type != RoomType.Hall)
            {
                prototypebot.MechFunctInvoke();
                return;
            }
            else
                ChangeNavigationState(new NavigationState_TargetPlayer(npc, 99, player.transform.position));
            if (npc.looker.PlayerInSight())
            {
                npc.Navigator.maxSpeed = 11;
                npc.Navigator.SetSpeed(11);
            }
        }
    }

    public override void PlayerLost(PlayerManager player)
    {
        if (Vector3.Distance(npc.transform.position, player.transform.position) <= 211f)
            prototypebot.MechFunctInvoke();
    }
}

public class PrototypeBot_Sneak(PrototypeBot bot, PlayerManager player) : PrototypeBot_StateBase(bot)
{
    private PlayerManager player = player;
    private int maxattempts = 30;

    public override void Enter()
    {
        base.Enter();
        flagged = true;
        prototypebot.animator.SetDefaultAnimation("Idle", 1f, true);
        npc.Navigator.maxSpeed = 40;
        npc.Navigator.SetSpeed(40);
        npc.Navigator.Entity.SetTrigger(false); // Don't want accidents happen...
        bool flag = false;
        int attempts = 0;
        while (!flag && attempts < maxattempts)
        {
            List<Cell> hall = npc.ec.mainHall.cells;
            npc.Navigator.Entity.Teleport(hall[Mathf.RoundToInt(UnityEngine.Random.Range(0f, hall.Count - 1))].CenterWorldPosition);
            flag = true;
            attempts++;
            if (!flag)
                break;
            if (Vector3.Distance(npc.transform.position, player.transform.position) > ((250f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z))
                || Vector3.Distance(npc.transform.position, player.transform.position) <= ((180f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z)))
                flag = false;
        }
        npc.Navigator.Entity.SetTrigger(true);
        if (attempts >= maxattempts)
            prototypebot.MechFunctInvoke();
        else
            flagged = false;
    }

    public override void Update()
    {
        float distance = Vector3.Distance(npc.transform.position, player.transform.position);
        if (distance <= ((250f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z)))
        {
            if (!IsActuallyHidden(player))
                ChangeNavigationState(new NavigationState_TargetPlayer(npc, 99, player.transform.position));
            else
                ChangeNavigationState(new NavigationState_WanderRandom(npc, 100));
            if (distance < ((180f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z)) && !npc.looker.IsVisible && !prototypebot.AudMan.QueuedAudioIsPlaying)
                prototypebot.AudMan.Pause(false);
        }
        else
        {
            prototypebot.MechFunctInvoke();
            return;
        }
        if (distance >= ((45f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z)) && !npc.looker.IsVisible)
            npc.Navigator.SetSpeed(25f);
        else if (!flagged && ((distance < ((45f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z)) && npc.looker.IsVisible) || distance < ((29f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z))) && npc.looker.PlayerInSight())
        {
            if (readyEnumerator == null)
            {
                readyEnumerator = IsReady(player.transform.position);
                prototypebot.StartCoroutine(readyEnumerator);
            }
        }
        else if (npc.looker.IsVisible)
        {
            npc.Navigator.SetSpeed(0f);
            if (prototypebot.AudMan.QueuedAudioIsPlaying)
                prototypebot.AudMan.Pause(true);
        }

        if (prototypebot.AudMan.QueuedAudioIsPlaying)
            Rumble(0.1f);
    }

    internal override IEnumerator IsReady(Vector3 pos)
    {
        npc.Navigator.maxSpeed = 0;
        npc.Navigator.SetSpeed(0);
        yield return new WaitForSecondsNPCTimescale(npc, 0.4f);
        prototypebot.behaviorStateMachine.ChangeState(new PrototypeBot_Chase(prototypebot, pos));
    }

    public override void PlayerInSight(PlayerManager player)
    {
        ChangeNavigationState(new NavigationState_TargetPosition(npc, 9, player.transform.position));
    }

    public override void Exit()
    {
        prototypebot.AudMan.FlushQueue(true);
    }

    public override void DestinationEmpty()
    {
        if (!npc.looker.PlayerInSight())
            prototypebot.behaviorStateMachine.ChangeState(new PrototypeBot_StateBase(prototypebot));
        else
            ChangeNavigationState(new NavigationState_TargetPlayer(npc, 99, player.transform.position));
    }

    public override void OnStateTriggerStay(Entity entity, Collider other, bool isValid)
    {
    }
}

public class PrototypeBot_Prank(PrototypeBot bot) : PrototypeBot_StateBase(bot)
{
    private float passedTime = 0f;
    private Door nearestDoor;

    public override void Enter()
    {
        base.Enter();
        flagged = true;
        prototypebot.animator.SetDefaultAnimation("Idle", 1f, true);
        npc.Navigator.maxSpeed = 0;
        npc.Navigator.SetSpeed(0);
        if (BaseGameManager.Instance.FoundNotebooks < 1)
        {
            prototypebot.MechFunctInvoke();
            return;
        }
        List<RoomController> peek = new List<RoomController>();
        foreach (RoomController room in npc.ec.rooms.FindAll(x => x.category == RoomCategory.Class))
        {
            if (room == null) continue;
            if (room.ec.activities.Find(x => x.room == room & !x.IsCompleted)
                || Vector3.Distance(npc.ec.RealRoomMid(room), prototypebot.players[0].transform.position) > ((260f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z)) || Vector3.Distance(npc.ec.RealRoomMid(room), prototypebot.players[0].transform.position) <= ((230f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z)))
                continue;
            //Debug.Log("Adding classroom");
            peek.Add(room);
        }
        if (peek.Count > 0)
        {
            npc.Navigator.Entity.Teleport(npc.ec.RealRoomMid(peek[Mathf.RoundToInt(UnityEngine.Random.Range(0f, peek.Count - 1))]));
            float nearest = float.PositiveInfinity;
            foreach (var tile in npc.Navigator.Entity.CurrentRoom?.AllTilesNoGarbage(false, true))
            {
                foreach (var door in tile.doors)
                {
                    var distance = Vector3.Distance(door.transform.position, npc.transform.position);
                    if (distance <= nearest)
                    {
                        nearestDoor = door;
                        nearest = distance;
                    }
                }
            }
            flagged = false;
        }
        else
            prototypebot.MechFunctInvoke();
    }

    public override void Update()
    {
        if (nearestDoor == null)
            return;
        passedTime += ((BaseGameManager.Instance.FoundNotebooks / BaseGameManager.Instance.NotebookTotal) * 2) * (Time.deltaTime * npc.ec.NpcTimeScale);
        float distance = Vector3.Distance(npc.ec.RealRoomMid(npc.Navigator.Entity.CurrentRoom), prototypebot.players[0].transform.position);
        float doorDist = Vector3.Distance(nearestDoor.transform.position, prototypebot.players[0].transform.position);
        Physics.Raycast(nearestDoor.transform.position, prototypebot.players[0].transform.position - nearestDoor.transform.position, out RaycastHit playerHit, float.PositiveInfinity, LayerMask.GetMask("Default", "Player"), QueryTriggerInteraction.Ignore);
        if (distance >= ((260f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z)) || passedTime >= (20f * BaseGameManager.Instance.FoundNotebooks))
        {
            prototypebot.MechFunctInvoke();
            return;
        }
        if ((doorDist < 30f | playerHit.transform.CompareTag("Player")) & !flagged)
        {
            var pm = playerHit.transform.GetComponent<PlayerManager>();
            if (pm != null && !IsActuallyHidden(pm))
            {
                if (readyEnumerator == null)
                {
                    readyEnumerator = IsReady(playerHit.transform.position);
                    prototypebot.StartCoroutine(readyEnumerator);
                }
            }
        }
    }

    public override void PlayerInSight(PlayerManager player)
    {
        if (readyEnumerator == null)
        {
            readyEnumerator = IsReady(player.transform.position);
            prototypebot.StartCoroutine(readyEnumerator);
        }
    }

    internal override IEnumerator IsReady(Vector3 pos)
    {
        yield return new WaitForSecondsNPCTimescale(npc, 0.4f);
        prototypebot.behaviorStateMachine.ChangeState(new PrototypeBot_Chase(prototypebot, pos));
    }

    public override void Exit()
    {
    }
}

public class PrototypeBot_Chase(PrototypeBot bot, Vector3 pos) : PrototypeBot_StateBase(bot)
{
    /*private float stompDistance = 5f;
    private float toNextStomp;*/

    private Vector3 position = pos;

    public override void Enter()
    {
        base.Enter();
        prototypebot.animator.SetDefaultAnimation("Running", 1f, true);
        prototypebot.AudMan.FlushQueue(true);
        npc.Navigator.maxSpeed = 70;
        npc.Navigator.SetSpeed(70);
        ChangeNavigationState(new NavigationState_TargetPosition(npc, 9, position));
    }

    private bool iAlreadyStomped;
    public override void Update()
    {
        /*if (Time.timeScale > 0f && npc.Navigator.Velocity.magnitude > 0.1f * Time.deltaTime)
        {
            float num = npc.Navigator.Velocity.magnitude - toNextStomp;
            toNextStomp -= npc.Navigator.Velocity.magnitude;
            if (toNextStomp <= 0f)
            {
                toNextStomp = stompDistance - num;
                prototypebot.AudMan.PlaySingle(prototypebot.faststompSnd);
                InputManager.Instance.Rumble(1f - toNextStomp / 100f, 0.2f, Vector3.SignedAngle(npc.transform.position - CoreGameManager.Instance.GetPlayer(0).transform.position, CoreGameManager.Instance.GetPlayer(0).transform.forward, Vector3.up));
            }
        }*/

        if (prototypebot.animator.AnimationId == "Running" && Time.timeScale != 0)
        {
            if (prototypebot.animator.AnimationFrame % 3 == 0 && !iAlreadyStomped)
            {
                iAlreadyStomped = true;
                prototypebot.AudMan.PlaySingle(prototypebot.faststompSnd);
                Rumble(0.3f);
            }
            else if (!(prototypebot.animator.AnimationFrame % 3 == 0) && iAlreadyStomped)
                iAlreadyStomped = false;
        }
    }

    public override void PlayerInSight(PlayerManager player)
    {
        ChangeNavigationState(new NavigationState_TargetPosition(npc, 10, player.transform.position));
    }

    public override void DestinationEmpty()
    {
        prototypebot.MechFunctInvoke();
    }

    internal override IEnumerator IsReady(Vector3 pos)
    {
        ChangeNavigationState(new NavigationState_TargetPosition(npc, 9, pos));
        yield break;
    }
}

public class PrototypeBot_Kill(PrototypeBot bot) : PrototypeBot_StateBase(bot)
{
    public override void Hear(GameObject source, Vector3 position, int value) { }
    public override void OnStateTriggerStay(Entity entity, Collider other, bool isValid) { }
}