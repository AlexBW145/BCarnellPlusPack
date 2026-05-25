using BCarnellChars.OtherStuff;
using HarmonyLib;
using MTM101BaldAPI;
using System.Collections;
using System.Linq;
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

public class BCarnellBoss_GoingTo(BCarnellBoss carnell, Vector3 pos, NpcState nextState) : BCarnellBoss_StateBase(carnell)
{
    private NpcState nextState = nextState;
    private Vector3 pos = pos;
    public override void Hear(GameObject source, Vector3 position, int value)
    {
    }

    public override void PlayerInSight(PlayerManager player)
    {
    }

    public override void Enter()
    {
        base.Enter();
        ChangeNavigationState(new NavigationState_TargetPosition(npc, 126, pos));
    }

    public override void DestinationEmpty()
    {
        if (nextState != null)
            npc.behaviorStateMachine.ChangeState(nextState);
    }

    public override void Update()
    {
        bcarnell.UpdateSlapDistance();
    }
}

public class BCarnellBoss_PreBattle(BCarnellBoss carnell, Vector3 pos) : BCarnellBoss_GoingTo(carnell, pos, null)
{
    public override void DestinationEmpty() => CarnellBossManager.BossInstance.CutsceneIntro();

    public override void OnRoomEnter(RoomController room)
    {
        base.OnRoomEnter(room);
        if (room.functionObject.GetComponent<B2BaseRoomFunction>() != null)
        {
            room.doors.Do(x =>
            {
                x.Lock(true);
                x.Shut();
            });
            room.lights.Do(x => x.SetLight(true));
        }
    }

    public override void Enter()
    {
        base.Enter();
        bcarnell.Navigator.Entity.SetTrigger(false);
    }
    public override void Exit()
    {
        base.Exit();
        bcarnell.Navigator.Entity.SetTrigger(true);
    }
}

public class BCarnellBoss_PostBattle(BCarnellBoss carnell) : BCarnellBoss_StateBase(carnell)
{
    private static readonly Sprite[] randoms = Resources.FindObjectsOfTypeAll<Sprite>().Where(x => x.GetInstanceID() >= 0 && x.pixelsPerUnit >= 30f).ToArray();
    public override void PlayerInSight(PlayerManager player)
    {
    }

    public override void Hear(GameObject source, Vector3 position, int value)
    {
    }

    public override void DestinationEmpty()
    {
    }

    public override void Initialize()
    {
        base.Initialize();
        ChangeNavigationState(new NavigationState_Disabled(bcarnell));
        bcarnell.audMan.QueueAudio(bcarnell.postbattle, true);
        IEnumerator Wellyoudie()
        {
            var source = bcarnell.audMan.audioSourceManager.GetAudioSource(SoundType.Effect);
            yield return new WaitUntil(() => source.time >= 12.42f);
            bcarnell.ec.StartCoroutine(Bitch());
            CoreGameManager.Instance.audMan.volumeModifier = 0.6f;
            CoreGameManager.Instance.audMan.PlaySingle(bcarnell.death);
            yield return new WaitUntil(() => !bcarnell.audMan.AnyAudioIsPlaying);
            bcarnell.ec.ElevatorManager.SetTotalOutOfOrderElevators(0);
            bcarnell.ec.ElevatorManager.SetAllElevators(ElevatorState.OpenForExit);
            bcarnell.Despawn();
            CoreGameManager.Instance.audMan.volumeModifier = 1f;
            for (int i = 0; i < CoreGameManager.Instance.setPlayers; i++)
                CoreGameManager.Instance.GetPlayer(i).Am.moveMods.RemoveAt(0);
            if (CoreGameManager.Instance.SaveEnabled && MTM101BaldiDevAPI.SaveGamesEnabled && CarnellBossManager.BossInstance != null
            && CoreGameManager.Instance.currentMode != Mode.Free)
            {
                BCPPSave.Instance.death = true;
                BCPPSave.Instance.Save();
            }
        }
        IEnumerator Bitch()
        {
            float elaspedTime = 0f;
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            while (bcarnell != null)
            {
                elaspedTime += Time.deltaTime;
                if (elaspedTime >= 10f)
                    elaspedTime += elaspedTime * Time.deltaTime;
                bcarnell.spriteRenderer[0].transform.localScale = Vector3.Max(Vector3.zero, Vector3.one - (Vector3.one * (elaspedTime * 0.00005f)));
                bcarnell.spriteRenderer[0].GetPropertyBlock(propertyBlock);
                propertyBlock.SetInt("_SpriteColorGlitching", 1);
                propertyBlock.SetFloat("_SpriteColorGlitchVal", Random.Range(0f, 4096f));
                if (Random.Range(0f, 1f) <= 0.5f)
                {
                    propertyBlock.SetInt("_AlphaGlitch", 1);
                    propertyBlock.SetFloat("_SpriteColorGlitchPercent", Mathf.Clamp(elaspedTime * 0.1f, 0f, 1f));
                }
                else
                {
                    propertyBlock.SetInt("_AlphaGlitch", 0);
                    propertyBlock.SetFloat("_SpriteColorGlitchPercent", 1f);
                }
                if (Random.Range(0f, 1f) <= 0.5f)
                {
                    propertyBlock.SetInt("_ChunkGlitch", 1);
                    propertyBlock.SetFloat("_ChunkGlitchThreshold", Mathf.Clamp(1f - (elaspedTime * 0.05f), 0f, 1f));
                    propertyBlock.SetFloat("_ChunkGlitchFullThreshold", Mathf.Clamp(1f - (elaspedTime * 0.025f), 0f, 1f));
                }
                else
                    propertyBlock.SetInt("_ChunkGlitch", 0);
                bcarnell.spriteRenderer[0].SetPropertyBlock(propertyBlock);
                if (elaspedTime >= 7f && Random.Range(0f, 1f) <= 0.5f)
                    bcarnell.spriteRenderer[0].sprite = randoms[UnityEngine.Random.RandomRangeInt(0, randoms.Length)];
                yield return null;
            }
        }
        bcarnell.ec.StartCoroutine(Wellyoudie());
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
        _audTimescale.SetValue(bcarnell.audMan as PropagatedAudioManager, TimeScaleType.Npc);
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
    private float elaspedTime;
    public override void PlayerSighted(PlayerManager player)
    {
        if (!bcarnell.Attacking && bcarnell.AbleToAttack && !IsActuallyHidden(player))
        {
            elaspedTime -= 10f;
            elaspedTime = Mathf.Max(0f, elaspedTime);
            bcarnell.Attack();
        }
    }

    public override void PlayerInSight(PlayerManager player)
    {
        if (!bcarnell.Attacking && !IsActuallyHidden(player))
            base.PlayerInSight(player);
    }

    public override void Update()
    {
        base.Update();
        elaspedTime += Time.deltaTime * npc.TimeScale;
        if (elaspedTime > (30f - (2.5f * (float)bcarnell.phases)) && !bcarnell.Attacking && bcarnell.AbleToAttack)
        {
            elaspedTime = 0f;
            bcarnell.Attack();
        }
    }

    public override void DestinationEmpty()
    {
        var players = bcarnell.ec.Players;
        if (Vector3.Distance(bcarnell.transform.position, players[0].transform.position) >= 65 && bcarnell.AbleToAttack)
            bcarnell.Attack();
        else
            base.DestinationEmpty();
    }

    public override void Hear(GameObject source, Vector3 position, int value)
    {
        base.Hear(source, position, value);
        if (bcarnell.phases == BCarnellBoss.B2Boss.Phase3 && !bcarnell.Attacking && bcarnell.AbleToAttack)
            bcarnell.Attack();
    }

    protected override void ActivateSlapAnimation()
    {
    }
}