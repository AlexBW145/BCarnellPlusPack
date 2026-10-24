using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Components.Animation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeacherAPI;
using UnityEngine;

namespace PrototypeBotAnywhere
{
    public class PrototypeBot01_Teacher : Teacher
    {
        internal List<PlayerManager> players => ec.Players.ToList();
        [SerializeField] internal AudioManager ambientaudMan;
        public Fog fog = new Fog()
        {
            color = Color.black,
            startDist = 5f,
            maxDist = 250f,
            priority = 0,
            strength = 0
        };
        public AudioManager Ambience => ambientaudMan;
        [SerializeField] internal CustomSpriteRendererAnimator animator;
        [SerializeField] internal CustomImageAnimator overlayAnimator;
        [SerializeField] internal Canvas overlay;
        [SerializeField] internal SoundObject faststompSnd, impatientSnd, ceilingclimbSnd, stunnedSnd;
        private DijkstraMap fleeMap;
        public DijkstraMap FleeMap => fleeMap;

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
        public override TeacherState GetAngryState() => new PrototypeBot_Teacher_Chase(this, transform.position);
        public override TeacherState GetHappyState() => new PrototypeBot_Teacher_Intermission(this);
        public override TeacherState GetPraiseState(float time, NpcState previousState) => (TeacherState)behaviorStateMachine.currentState ?? new Prototype01_Teacher_TeleportLater(this);

        public override AssistantPolicy GetAssistantPolicy() => new AssistantPolicy(PossibleAssistantAllowType.Allow).MaxAssistants(0);

        public override string GetNotebooksText(string amount) => $"{amount} Notebooks";

        public new void SlapNormal()
        {
        }
        public new void SlapBroken()
        {
        }
        // SHOULD NOT INVOKE UNLESS FOR CUTSCENE PURPOSES!!
        public override void Slap()
        {
            animator.SetDefaultAnimation("Idle", 1f, true);
            animator.Play("Running", 1f);
        }

        private static readonly FieldInfo
            _arrows = AccessTools.DeclaredField(typeof(Map), "arrows");

        public override void Initialize()
        {
            base.Initialize();
            ReplacementMusic = "mute";
            if (Chainloader.PluginInfos.ContainsKey("org.aestheticalz.baldi.characterradar"))
                if (!ec.map.arrowTargets.Contains(navigator.Entity))
                    ec.map.AddArrow(navigator.Entity, new Color(0.3089626f, 0.1634033f, 0.6792453f, 1f));
                else
                {
                    var arrows = _arrows.GetValue(ec.map) as List<MapIcon>;
                    arrows[ec.map.arrowTargets.LastIndexOf(navigator.Entity)].spriteRenderer.color = new Color(0.3089626f, 0.1634033f, 0.6792453f, 1f);
                }
            animator.ec = ec;
            fleeMap = new DijkstraMap(ec, PathType.Const, 1000, transform);
        }

        internal void ScaryMode()
        {
            ec.MaxRaycast = int.MaxValue;
            ambientaudMan.FlushQueue(true);
            ambientaudMan.QueueAudio(Proto01Plugin.assets01.Get<SoundObject>("PrototypeBot-01/TimeOut"), true);
            ambientaudMan.QueueAudio(Proto01Plugin.assets01.Get<SoundObject>("PrototypeBot-01/TimeOutLoop"));
            ambientaudMan.SetLoop(true);
        }

        protected override void VirtualUpdate()
        {
            if (breakRuler)
            {
                OnRulerBroken();
                breakRuler = false;
            }
            else if (restoreRuler)
            {
                OnRulerRestored();
                restoreRuler = false;
            }
        }

        public override float DistanceCheck(float val) => val;

        protected override void OnRulerBroken()
        {
            behaviorStateMachine.ChangeState(new PrototypeBot_Teacher_Stunned(this));
            AudMan.FlushQueue(true);
            AudMan.SetLoop(true);
            AudMan.maintainLoop = true;
            AudMan.QueueAudio(stunnedSnd, true);
            AudMan.maintainLoop = false;
        }

        protected override void OnRulerRestored()
        {
            AudMan.FlushQueue(true);
            WeightedSelection<Action>.RandomSelection(mechFuncts).Invoke();
        }

        public void MechFunctInvoke()
        {
#if DEBUG
            for (int k = 0; k < CoreGameManager.Instance.setPlayers; k++)
                Debug.Log("Distance between PrototypeBot-01 & player"+k+ ": " + Vector3.Distance(transform.position, players[k].transform.position));
#endif
            if (!behaviorStateMachine.currentState.GetType().Equals(typeof(PrototypeBot_Teacher_Stunned)))
            {
                AudMan.FlushQueue(true);
                WeightedSelection<Action>.RandomSelection(mechFuncts).Invoke();
            }
            if (fleeMap.UpdateIsNeeded())
                fleeMap.QueueUpdate();
        }

        public IEnumerator StunHearing(float time)
        {
            if (ec.CurrentEventTypes.Contains(RandomEventType.Snap))
                yield break;
            OnRulerBroken();
            yield return new WaitForSecondsEnvironmentTimescale(ec, time);
            if (ec.CurrentEventTypes.Contains(RandomEventType.Snap))
                yield break;
            OnRulerRestored();
        }

        // Action Functions
        private void SneakySnitch()
        {
            behaviorStateMachine.ChangeState(new PrototypeBot_Teacher_Sneak(this, players[0]));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_TargetPlayer(this, 99, players[0].transform.position));
            AudMan.FlushQueue(true);
            AudMan.SetLoop(true);
            AudMan.maintainLoop = true;
            AudMan.QueueAudio(impatientSnd, true);
            AudMan.maintainLoop = false;
        }
        private void CeilingClimb()
        {
            behaviorStateMachine.ChangeState(new PrototypeBot_Teacher_Ceiling(this));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_WanderRandom(this, 0));
            AudMan.FlushQueue(true);
            AudMan.SetLoop(true);
            AudMan.maintainLoop = true;
            AudMan.QueueAudio(ceilingclimbSnd, true);
            AudMan.maintainLoop = false;
        }
        private void DoorPrank()
        {
            AudMan.FlushQueue(true);
            behaviorStateMachine.ChangeState(new PrototypeBot_Teacher_Prank(this));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 0));
        }

        public override void CaughtPlayer(PlayerManager player)
        {
            base.CaughtPlayer(player);
            if (behaviorStateMachine.CurrentState is not Baldi_Attack) return;
            spriteBase.SetActive(false);
            overlay.gameObject.SetActive(true);
            overlayAnimator.Play("Jump" + Mathf.RoundToInt(UnityEngine.Random.Range(1f, 2f)), 1f);
            overlay.worldCamera = CoreGameManager.Instance.GetCamera(player.playerNumber).canvasCam;
            ambientaudMan.FlushQueue(true);
        }

        public void KillNPC(NPC victim)
        {
            victim.Navigator.Entity.SetInteractionState(false);
            IEnumerator killingSpree()
            {
                behaviorStateMachine.ChangeState(new PrototypeBot_Teacher_KillNPC(this, behaviorStateMachine.currentState, victim));
                float time = 0f;
                float limit = UnityEngine.Random.Range(3f,5f);
                animator.SetDefaultAnimation("Running", 2f, true);

                while (time < limit)
                {
                    time += TimeScale * Time.deltaTime;
                    yield return null;
                }
                victim.Entity.SetActive(false);
                var be = behaviorStateMachine.currentState as PrototypeBot_Teacher_KillNPC;
                be?.DespawnedNPC();
            }

            StartCoroutine(killingSpree());
        }
    }
}
