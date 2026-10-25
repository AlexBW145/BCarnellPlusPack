using HarmonyLib;
using MidiPlayerTK;
using MTM101BaldAPI.Components.Animation;
using System;
using System.Linq;
using TeacherAPI;
using UnityEngine;

namespace BCarnellTeacher
{
    public class BCarnell : Teacher
    {
        [SerializeField] internal CustomSpriteRendererAnimator animator;
        private Fog finaleFog = new Fog()
        {
            color = new Color(0.2431373f, 0f, 0f),
            startDist = 15f,
            maxDist = 500f,
            strength = 1f,
            priority = 9,
        };
        [SerializeField] internal Canvas overlay;
        [SerializeField] internal CustomImageAnimator overlayAnimator;

        public override TeacherState GetAngryState() => new BCarnell_Chase(this);
        public override TeacherState GetHappyState() => new BCarnell_Tutor(this);
        public override TeacherState GetPraiseState(float time, NpcState previousState) => new BCarnell_Praise(this, previousState, time);
        private bool rulerBroken = false;
        public bool RulerBroken => rulerBroken; // Had to make an another variable...
        public override AssistantPolicy GetAssistantPolicy() => new AssistantPolicy(PossibleAssistantAllowType.Allow)
            .MaxAssistants(2)
            .AddAssistants("Foxo").Probability(0.5f);

        [SerializeField]
        internal SoundObject
            hello, regenDagger, screamsInAgony;
        [SerializeField] internal Sprite[] binders;

        public override void Initialize()
        {
            ReplacementMusic = CarnellPlugin.bcarnelAssets.Get<string>("SchoolHouseCarnell");
            base.Initialize();
            navigator.Entity.SetHeight(6.5f);
            navigator.SetRoomAvoidance(false);
            animator.ec = ec;
            {
                baseAnger += 5.04f; // 5.05f;
                baseSpeed += 0.85f;
                speedMultiplier += 0.198f; //1.85f;
                slapSpeedScale += 15f; //15.65f;
                baseSpeed += 12.5f;
            }
            // Random events
            /*ReplaceEventText<RulerEvent>(LocalizationManager.Instance.GetLocalizedText("Event_BrokenRuler_BCarnell"));
            ReplaceEventText<PartyEvent>(LocalizationManager.Instance.GetLocalizedText("Event_Party_BCarnell"));*/
        }

        protected override void VirtualUpdate()
        {
            base.VirtualUpdate();
            if (behaviorStateMachine.CurrentState is not BCarnell_Chase)
                return;
            /*if ((animator.currentAnimationName == "Slap" || animator.currentAnimationName == "SlapBroken") && animator.currentFrameIndex == 4)
            {
                Slap();
                // This will cause small lag, and this is probably worthless...
                BCarnell_StateBase ufhm = behaviorStateMachine.currentState as BCarnell_StateBase;
                ufhm.ReflectionInvoke("ActivateSlapAnimation", []);
            }*/

            int closedElevators = ec.Elevators.Count(x => !x.GateIsOpen);
            if (startedFinale) // Balanced with four or more elevators??
            {
                if (closedElevators < Math.Max(1, Mathf.RoundToInt(ec.Elevators.Count / 3f)) && MusicManager.Instance.MidiPlayer.MPTK_MidiName != CarnellPlugin.bcarnelAssets.Get<string>("AlphabetFlee01"))
                    MusicManager.Instance.PlayMidi(CarnellPlugin.bcarnelAssets.Get<string>("AlphabetFlee01"), true);
                else if (closedElevators == Math.Max(1, Mathf.RoundToInt(ec.Elevators.Count / 3f)) && MusicManager.Instance.MidiPlayer.MPTK_MidiName != CarnellPlugin.bcarnelAssets.Get<string>("AlphabetFlee02"))
                    MusicManager.Instance.PlayMidi(CarnellPlugin.bcarnelAssets.Get<string>("AlphabetFlee02"), true);
                else if (closedElevators == Math.Max(2, Mathf.RoundToInt(ec.Elevators.Count / 2f)) && MusicManager.Instance.MidiPlayer.MPTK_MidiName != CarnellPlugin.bcarnelAssets.Get<string>("AlphabetFlee03"))
                    MusicManager.Instance.PlayMidi(CarnellPlugin.bcarnelAssets.Get<string>("AlphabetFlee03"), true);
                else if (closedElevators == Math.Max(3, Mathf.RoundToInt(ec.Elevators.Count / 1.5f)) && MusicManager.Instance.MidiPlayer.MPTK_MidiName != CarnellPlugin.bcarnelAssets.Get<string>("AlphabetFlee04"))
                    MusicManager.Instance.PlayMidi(CarnellPlugin.bcarnelAssets.Get<string>("AlphabetFlee04"), true);
            }
        }

        public override void Slap()
        {
            base.Slap();
#if DEBUG
            //Debug.Log("Delay of " + Delay);
#endif
        }

        public override void Hear(GameObject source, Vector3 position, int value)
        {
            ClearSoundLocations(false, Vector3.zero);
        }

        public override void CaughtPlayer(PlayerManager player)
        {
            base.CaughtPlayer(player);
            if (behaviorStateMachine.CurrentState is not Baldi_Attack) return;
            spriteBase.SetActive(false);
            overlay.gameObject.SetActive(true);
            overlayAnimator.Play("Jumpscarey", 1f);
            overlay.worldCamera = CoreGameManager.Instance.GetCamera(player.playerNumber).canvasCam;
        }

        public new void SlapNormal()
        {
            animator.SetDefaultAnimation("SlapIdle", 1f);
            if (animator.AnimationId != "Slap" && animator.AnimationId != "SlapBroken")
                animator.Play("Slap", 1f);
            SlapRumble();
            AudMan.PlaySingle(slap);
        }
        public new void SlapBroken()
        {
            animator.SetDefaultAnimation("SlapIdleBroken", 1f);
            if (animator.AnimationId != "Slap" && animator.AnimationId != "SlapBroken")
                animator.Play("SlapBroken", 1f);
            SlapRumble();
            AudMan.PlaySingle(slap);
            if (extraAnger < 2.5f)
                extraAnger = 2.5f;
        }
        protected override void OnRulerBroken()
        {
            base.OnRulerBroken();
            AudMan.PlaySingle(rulerBreak);
            rulerBroken = true;
        }
        protected override void OnRulerRestored()
        {
            base.OnRulerRestored();
            AudMan.PlaySingle(regenDagger);
            extraAnger -= 2.5f;
            if (extraAnger < 0f)
                extraAnger = 0f;
            rulerBroken = false;
        }
        public void TeleportToNearestDoor()
        {
            var playerPos = ec.Players[0].transform.position;
            Door nearestDoor = null;
            var nearest = float.PositiveInfinity;

            foreach (var tile in ec.Players[0].plm.Entity.CurrentRoom.AllEntitySafeCellsNoGarbage())
            {
                foreach (var door in tile.doors)
                {
                    var distance = (door.transform.position - playerPos).magnitude;
                    if (distance <= nearest)
                    {
                        nearestDoor = door;
                        nearest = distance;
                    }
                }
            }

            if (nearestDoor == null)
            {
                Debug.LogWarning("WTF?! NO DOOR??");
                return;
            }
            Entity.Teleport(nearestDoor.aTile.Tile.transform.position);
        }

        public override string GetNotebooksText(string amount) => amount + " Binders";
        public override WeightedTeacherNotebook GetTeacherNotebookWeight() => new WeightedTeacherNotebook(this).Weight(80).Sprite(binders);

        public void Attract(Vector3 pos)
        {
            if (behaviorStateMachine.CurrentState is not BCarnell_Chase || looker.PlayerInSight())
                return;
            behaviorStateMachine.ChangeNavigationState(new NavigationState_TargetPosition(this, 1, pos));
        }

        public void AllNotebooks()
        {
            if (!IsHelping() && BaseGameManager.Instance.levelObject.finalLevel &&
                MusicManager.Instance.MidiPlayer.MPTK_MidiName != CarnellPlugin.bcarnelAssets.Get<string>("AlphabetFleeStart") && !startedFinale)
            {
                MusicManager.OnMidiEvent += FinaleMIDIEvent;
                MusicManager.Instance.PlayMidi(CarnellPlugin.bcarnelAssets.Get<string>("AlphabetFleeStart"), false);
            }
        }

        private bool startedFinale = false;
        private bool loudBugfix = false;
        public void FinaleMIDIEvent(MPTKEvent midiEvent)
        {
            if (MusicManager.Instance.MidiPlayer.MPTK_MidiName == CarnellPlugin.bcarnelAssets.Get<string>("AlphabetFleeStart"))
            {
                switch (midiEvent.Tick)
                {
                    case 0:
                        ec.AllCells().Do(x =>
                        {
                            x.lightColor = new Color(1f, 0.8443396f, 0.8443396f);
                            x.SetLight(x.lightOn);
                        });
                        ec.AddFog(finaleFog);
                        break;
                    case 768:
                        ec.AllCells().Do(x =>
                        {
                            x.lightColor = new Color(1f, 0.5235849f, 0.5235849f);
                            x.SetLight(x.lightOn);
                        });
                        break;
                    case 1536:
                        ec.AllCells().Do(x =>
                        {
                            x.lightColor = new Color(1f, 0.2122642f, 0.2122642f);
                            x.SetLight(x.lightOn);
                        });
                        break;
                    case 2304:
                        ec.AllCells().Do(x =>
                        {
                            x.lightColor = Color.red;
                            x.SetLight(x.lightOn);
                        });
                        break;
                    case 3456:
                        if (!loudBugfix) // Don't remove this boolean, the results are very bad.
                        {
                            loudBugfix = true;
                            CoreGameManager.Instance.audMan.PlaySingle(screamsInAgony);
                        }
                        break;
                    case 6096:
                        startedFinale = true;
                        break;
                }
                
            }
        }
    }
}
