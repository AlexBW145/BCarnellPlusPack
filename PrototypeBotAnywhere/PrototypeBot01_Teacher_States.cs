using HarmonyLib;
using JetBrains.Annotations;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TeacherAPI;
using UnityEngine;

namespace PrototypeBotAnywhere
{
    public class Prototype01_Teacher_StateBase : TeacherState
    {
        public Prototype01_Teacher_StateBase(PrototypeBot01_Teacher _01) : base(_01)
        {
            prototypebot = _01;
        }

        protected static FieldInfo
            _hiddenByLight = AccessTools.DeclaredField(typeof(PlayerManager), "hiddenByLight"),
            _hides = AccessTools.DeclaredField(typeof(Entity), "hides");
        protected bool IsActuallyHidden(PlayerManager player)
        {
            if ((bool)_hiddenByLight.GetValue(player) && (int)_hides.GetValue(player.plm.Entity) > 2)
                return true;
            return !(bool)_hiddenByLight.GetValue(player) && player.Invisible;
        }

        private bool goodEnough = false;
        public override void NotebookCollected(int currentNotebooks, int maxNotebooks)
        {
            if ((!goodEnough && npc.behaviorStateMachine.currentState is not PrototypeBot_Teacher_Chase) || goodEnough)
                prototypebot.MechFunctInvoke();
        }

        public override void GoodMathMachineAnswer(Activity activity)
        {
            goodEnough = true;
        }

        public override void Hear(GameObject source, Vector3 position, int value)
        {
            if (value >= 112 && Mathf.RoundToInt(Vector3.Distance(npc.transform.position, position)) < (245 + value))
                prototypebot.behaviorStateMachine.ChangeState(new PrototypeBot_Teacher_Chase(prototypebot, position));
            else if (!npc.Navigator.HasDestination && npc.behaviorStateMachine.currentState is Prototype01_Teacher_StateBase)
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

        public override void Update()
        {
            foreach (NPC npc in npc.ec.Npcs)
            {
                if (npc == null || npc.behaviorStateMachine.CurrentNavigationState == null || npc.gameObject == prototypebot.gameObject || !npc.Navigator.enabled || npc.GetMeta()?.flags.HasFlag(NPCFlags.HasTrigger) == true)
                    continue;
                prototypebot.looker.Raycast(npc.transform, Vector3.Magnitude(prototypebot.transform.position - npc.transform.position), LayerMask.GetMask("Default", "Block Raycast", "Windows", "NPCs"), out bool targetSighted);
                if (targetSighted && npc.behaviorStateMachine.CurrentNavigationState is not NavigationState_Disabled and not NavigationState_DoNothing)
                {
                    if (npc.behaviorStateMachine.CurrentNavigationState is not NavigationState_WanderFleeOverride || (npc.behaviorStateMachine.CurrentNavigationState is NavigationState_WanderFleeOverride state && state.priority <= 11))
                        npc.behaviorStateMachine.ChangeNavigationState(new NavigationState_WanderFleeOverride(npc, 11, prototypebot.FleeMap));
                    if (prototypebot.FleeMap.UpdateIsNeeded())
                        prototypebot.FleeMap.QueueUpdate();
                }
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

        public override void OnStateTriggerStay(Entity otherentity, Collider other, bool isValid)
        {
            if (flagged || !isValid) return;
            if (other.CompareTag("NPC"))
            {
                npc.looker.Raycast(other.transform, Vector3.Magnitude(npc.transform.position - other.transform.position), LayerMask.GetMask("Default", "Block Raycast", "Windows", "NPCs"), out bool targetSighted);
                if (targetSighted && other.GetComponent<NPC>().Character != Character.Null && other.GetComponent<NPC>().GetMeta()?.flags.HasFlag(NPCFlags.HasTrigger) != false && npc.behaviorStateMachine.currentState is PrototypeBot_Teacher_Chase)
                    prototypebot.KillNPC(other.GetComponent<NPC>());
            }
            else if (prototypebot.IsTouchingPlayer(other) && npc.behaviorStateMachine.currentState is not PrototypeBot_Teacher_KillNPC)
                prototypebot.CaughtPlayer(other.GetComponent<PlayerManager>());
        }

        protected PrototypeBot01_Teacher prototypebot;
        protected internal bool flagged = false;

        protected IEnumerator readyEnumerator;
        internal virtual IEnumerator IsReady(Vector3 pos)
        {
            yield break;
        }
    }

    public class PrototypeBot_Teacher_KillNPC : Prototype01_Teacher_StateBase
    {
        private NpcState previous;
        private System.Random rng = new System.Random();
        private NPC npcvictim;
        public PrototypeBot_Teacher_KillNPC(PrototypeBot01_Teacher _01, NpcState previousState, NPC victim) : base(_01)
        {
            previous = previousState;
            npcvictim = victim;
        }

        public override void Enter()
        {
            ChangeNavigationState(new NavigationState_DoNothing(npc, 127));
            npcvictim.behaviorStateMachine.ChangeNavigationState(new NavigationState_Disabled(npcvictim));
            prototypebot.AudMan.QueueAudio(WeightedSelection<SoundObject>.RandomSelection(prototypebot.loseSounds), true);
        }

        public override void Hear(GameObject source, Vector3 position, int value)
        {
        }

        public override void Update()
        {
            base.Update();
            if (npcvictim?.behaviorStateMachine.CurrentNavigationState is not NavigationState_Disabled)
                npcvictim.behaviorStateMachine.ChangeNavigationState(new NavigationState_Disabled(npcvictim));
            if (rng.NextDouble() < 0.5f && !prototypebot.AudMan.QueuedAudioIsPlaying)
                prototypebot.AudMan.QueueAudio(WeightedSelection<SoundObject>.RandomSelection(prototypebot.loseSounds), true);
            if (rng.NextDouble() < 0.1f)
                prototypebot.AudMan.PlaySingle(prototypebot.faststompSnd);
            if (npcvictim != null)
            {
                if (rng.NextDouble() < 0.8f && !npcvictim.Navigator.Entity.Squished)
                    npcvictim.Navigator.Entity.Squish(UnityEngine.Random.Range(0.2f, 0.3f));
            }
        }

        public void DespawnedNPC()
        {
            npc.behaviorStateMachine.ChangeState(previous);
        }

        public override void NotebookCollected(int currentNotebooks, int maxNotebooks)
        {
            previous = new Prototype01_Teacher_TeleportLater(prototypebot);
        }

        public override void GoodMathMachineAnswer(Activity activity)
        {
        }

        public override void OnStateTriggerStay(Entity otherentity, Collider other, bool isValid)
        {
        }
    }

    public class PrototypeBot_Teacher_Intermission : Prototype01_Teacher_StateBase
    {
        public PrototypeBot_Teacher_Intermission(PrototypeBot01_Teacher _01) : base(_01)
        {
        }

        private System.Random lightonRNG = new System.Random(CoreGameManager.Instance.Seed());
        private float lightonChance = 0.2f;

        public override void Initialize()
        {
            base.Initialize();
            ChangeNavigationState(new NavigationState_DoNothing(prototypebot, 127));
        }

        public override void Enter()
        {
            base.Enter();
            prototypebot.spriteBase.SetActive(false);
            prototypebot.Navigator.Entity.SetTrigger(false);
            prototypebot.FleeMap.Activate();
            bool flag = false;
            while (!flag)
            {
                List<Cell> hall = npc.ec.FindHallways()[UnityEngine.Random.RandomRangeInt(0, npc.ec.FindHallways().Count - 1)];
                npc.Navigator.Entity.Teleport(hall[Mathf.RoundToInt(UnityEngine.Random.Range(0, hall.Count - 1))].CenterWorldPosition);
                if (!flag)
                    break;
                if (Vector3.Distance(npc.transform.position, prototypebot.players[0].transform.position) < ((200f + Mathf.Min(npc.ec.levelSize.x, npc.ec.levelSize.z)) - Mathf.Max(npc.ec.levelSize.x, npc.ec.levelSize.z)))
                    flag = false;
            }
        }

        public override void Update()
        {
        }

        public override void PlayerExitedSpawn()
        {
            base.PlayerExitedSpawn();
            prototypebot.ActivateSpoopMode();
            if (CoreGameManager.Instance.currentMode != Mode.Free)
            {
                prototypebot.Ambience.QueueAudio(Proto01Plugin.assets01.Get<SoundObject>("PrototypeBot-01/AmbientIntro"), true);
                prototypebot.Ambience.QueueAudio(Proto01Plugin.assets01.Get<SoundObject>("PrototypeBot-01/AmbientLoop"));
                prototypebot.Ambience.SetLoop(true);
                npc.ec.SetAllLights(false);
                foreach (Cell light in npc.ec.lights)
                {
                    if (lightonRNG.NextDouble() < lightonChance)
                        light.SetLight(true);
                }
                npc.ec.MaxRaycast = 250f;
                BaseGameManager.Instance.StartCoroutine(FadeOnFog());
                prototypebot.MechFunctInvoke();
            }
        }

        public override void OnStateTriggerStay(Entity otherentity, Collider other, bool isValid)
        {
        }

        public override void Hear(GameObject source, Vector3 position, int value)
        {
        }

        private IEnumerator FadeOnFog()
        {
            npc.ec.AddFog(prototypebot.fog);
            prototypebot.fog.strength = 0f;
            float fogStrength2 = 0f;
            while (fogStrength2 < 1f)
            {
                fogStrength2 += 0.25f * Time.deltaTime;
                prototypebot.fog.strength = fogStrength2;
                npc.ec.UpdateFog();
                yield return null;
            }

            fogStrength2 = 1f;
            prototypebot.fog.strength = fogStrength2;
            npc.ec.UpdateFog();
        }

        public override void Exit()
        {
            prototypebot.spriteBase.SetActive(true);
            prototypebot.Navigator.Entity.SetTrigger(true);
        }

        public override void GoodMathMachineAnswer(Activity activity)
        {
        }
    }

    public class PrototypeBot_Teacher_Stunned : Prototype01_Teacher_StateBase
    {
        public PrototypeBot_Teacher_Stunned(PrototypeBot01_Teacher bot)
            : base(bot)
        {
        }

        public override void OnStateTriggerStay(Entity otherentity, Collider other, bool isValid)
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
            npc.behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(npc, 127));
        }

        public override void GoodMathMachineAnswer(Activity activity)
        {
        }
        public override void NotebookCollected(int currentNotebooks, int maxNotebooks)
        {
        }
    }

    public class PrototypeBot_Teacher_Ceiling : Prototype01_Teacher_StateBase
    {
        public PrototypeBot_Teacher_Ceiling(PrototypeBot01_Teacher bot) : base(bot)
        {
        }
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

        public override void Resume()
        {
            prototypebot.animator.SetDefaultAnimation("Climbing", 1f, true);
            npc.Navigator.maxSpeed = 30;
            npc.Navigator.SetSpeed(30);
            npc.Navigator.Entity.SetGrounded(false);
            for (int k = 0; k < CoreGameManager.Instance.setPlayers; k++)
                npc.Navigator.Entity.IgnoreEntity(prototypebot.players[k].plm.Entity, true);
            player = prototypebot.players[Mathf.RoundToInt(UnityEngine.Random.Range(0, prototypebot.players.Count - 1))];
        }

        public override void Update()
        {
            base.Update();
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
            prototypebot.behaviorStateMachine.ChangeState(new PrototypeBot_Teacher_Chase(prototypebot, pos));
        }

        public override void PlayerLost(PlayerManager player)
        {
            if (Vector3.Distance(npc.transform.position, player.transform.position) <= 40f)
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

    public class PrototypeBot_Teacher_Sneak : Prototype01_Teacher_StateBase
    {
        private PlayerManager player;

        public PrototypeBot_Teacher_Sneak(PrototypeBot01_Teacher bot, PlayerManager _player)
            : base(bot)
        {
            player = _player;
        }
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

        public override void Resume()
        {
            prototypebot.animator.SetDefaultAnimation("Idle", 1f, true);
            npc.Navigator.maxSpeed = 40;
            npc.Navigator.SetSpeed(40);
        }

        public override void Update()
        {
            base.Update();
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
            prototypebot.behaviorStateMachine.ChangeState(new PrototypeBot_Teacher_Chase(prototypebot, pos));
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
                prototypebot.behaviorStateMachine.ChangeState(new Prototype01_Teacher_StateBase(prototypebot));
            else
                ChangeNavigationState(new NavigationState_TargetPlayer(npc, 99, player.transform.position));
        }

        public override void OnStateTriggerStay(Entity otherentity, Collider other, bool isValid)
        {
        }
    }

    public class PrototypeBot_Teacher_Prank : Prototype01_Teacher_StateBase
    {
        private float passedTime = 0f;
        private Door nearestDoor;
        public PrototypeBot_Teacher_Prank(PrototypeBot01_Teacher bot)
            : base(bot)
        {
        }

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

        public override void Resume()
        {
            prototypebot.MechFunctInvoke();
        }

        public override void Update()
        {
            base.Update();
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
            prototypebot.behaviorStateMachine.ChangeState(new PrototypeBot_Teacher_Chase(prototypebot, pos));
        }

        public override void Exit()
        {
        }
    }

    public class PrototypeBot_Teacher_Chase : Prototype01_Teacher_StateBase
    {
        private Vector3 position;
        public PrototypeBot_Teacher_Chase(PrototypeBot01_Teacher bot, Vector3 pos)
            : base(bot)
        {
            position = pos;
        }

        public override void Enter()
        {
            base.Enter();
            prototypebot.animator.SetDefaultAnimation("Running", 1f, true);
            prototypebot.AudMan.FlushQueue(true);
            npc.Navigator.maxSpeed = 70;
            npc.Navigator.SetSpeed(70);
            npc.Navigator.passableObstacles.Add(PassableObstacle.Bully);
            ChangeNavigationState(new NavigationState_TargetPosition(npc, 126, position));
        }

        private bool iAlreadyStomped;
        public override void Update()
        {
            base.Update();
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
            ChangeNavigationState(new NavigationState_TargetPosition(npc, 127, player.transform.position));
        }

        public override void DestinationEmpty()
        {
            prototypebot.MechFunctInvoke();
        }

        public override void Exit()
        {
            npc.Navigator.passableObstacles.Remove(PassableObstacle.Bully);
        }
    }

    public class Prototype01_Teacher_TeleportLater : Prototype01_Teacher_StateBase
    {
        public Prototype01_Teacher_TeleportLater(PrototypeBot01_Teacher prototype) : base(prototype) { }

        public override void Enter()
        {
            prototypebot.MechFunctInvoke();
        }

        public override void Update()
        {
        }

        public override void OnStateTriggerStay(Entity otherentity, Collider other, bool isValid)
        {
        }

        public override void NotebookCollected(int currentNotebooks, int maxNotebooks)
        {
        }

        public override void Hear(GameObject source, Vector3 position, int value)
        {
        }

        public override void GoodMathMachineAnswer(Activity activity)
        {
        }
    }
}
