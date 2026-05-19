using BCarnellChars.Characters.States;
using BCarnellChars.OtherStuff;
using MTM101BaldAPI;
using MTM101BaldAPI.Components.Animation;
using System;
using System.Collections;
using UnityEngine;

namespace BCarnellChars.Characters;

public class PrototypeBot : NPC
{
    [SerializeField] internal AudioManager audMan;
    [SerializeField] internal CustomSpriteRendererAnimator animator;

    public AudioManager AudMan => audMan;
    [SerializeField] internal WeightedSoundObject[] jumpscares;
    public WeightedSoundObject[] loseSounds => jumpscares;
    [SerializeField] internal SoundObject faststompSnd, impatientSnd, ceilingclimbSnd, stunnedSnd;

    private WeightedSelection<Action>[] mechFuncts => [
        new WeightedSelection<Action>()
        {
            selection = SneakySnitch,
            weight = 111,
        },
        new WeightedSelection<Action>()
        {
            selection = CeilingClimb,
            weight = 100,
        },
        new WeightedSelection<Action>()
        {
            selection = DoorPrank,
            weight = 99,
        }
        ];

    internal PlayerManager[] players => ec.Players;

    public override void Initialize()
    {

        this.ChangeRadarColor();
        base.Initialize();
        animator.ec = ec;
        behaviorStateMachine.ChangeState(new PrototypeBot_StateBase(this));
        behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 99));
        navigator.maxSpeed = 10;
        navigator.SetSpeed(10);

        MechFunctInvoke();
    }

    public void CaughtPlayer(PlayerManager player)
    {
        behaviorStateMachine.ChangeState(new PrototypeBot_Kill(this));
        if (BasementGameManager.BasementInstance != null)
            BasementGameManager.BasementInstance.EndGame(player.transform, this);
        else if (CoreGameManager.Instance != null & ec.GetBaldi() != null)
            CoreGameManager.Instance.EndGame(player.transform, ec.GetBaldi());
    }

    public virtual void MechFunctInvoke()
    {
#if DEBUG
        for (int k = 0; k < CoreGameManager.Instance.setPlayers; k++)
            Debug.Log("Distance between PrototypeBot-01 & player"+k+ ": " + Vector3.Distance(transform.position, players[k].transform.position));
#endif
        if (behaviorStateMachine.currentState is not PrototypeBot_Stunned)
        {
            audMan.FlushQueue(true);
            WeightedSelection<Action>.RandomSelection(mechFuncts).Invoke();
        }
    }
    public virtual IEnumerator StunHearing(float time)
    {
        behaviorStateMachine.ChangeState(new PrototypeBot_Stunned(this));
        audMan.FlushQueue(true);
        audMan.SetLoop(true);
        audMan.maintainLoop = true;
        audMan.QueueAudio(stunnedSnd, true);
        audMan.maintainLoop = false;
        yield return new WaitForSecondsEnvironmentTimescale(ec, time);
        audMan.FlushQueue(true);
        WeightedSelection<Action>.RandomSelection(mechFuncts).Invoke();
    }

    // Action Functions
    private void SneakySnitch()
    {
        behaviorStateMachine.ChangeState(new PrototypeBot_Sneak(this, players[0])); // UnityEngine.Random.RandomRangeInt(0, players.Length-1)
        behaviorStateMachine.ChangeNavigationState(new NavigationState_TargetPlayer(this, 99, players[0].transform.position));
        audMan.FlushQueue(true);
        audMan.SetLoop(true);
        audMan.maintainLoop = true;
        audMan.QueueAudio(impatientSnd, true);
        audMan.maintainLoop = false;
    }
    private void CeilingClimb()
    {
        behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 0));
        behaviorStateMachine.ChangeState(new PrototypeBot_Ceiling(this));
        audMan.FlushQueue(true);
        audMan.SetLoop(true);
        audMan.maintainLoop = true;
        audMan.QueueAudio(ceilingclimbSnd, true);
        audMan.maintainLoop = false;
    }
    private void DoorPrank()
    {
        audMan.FlushQueue(true);
        behaviorStateMachine.ChangeState(new PrototypeBot_Prank(this));
        behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 0));
    }
    private void TeleportToSides() // This thing busted...
    {
        DoorPrank();
        /*audMan.FlushQueue(true);
        behaviorStateMachine.ChangeState(new PrototypeBot_Corner(this, this));
        behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 1));*/
    }
}
