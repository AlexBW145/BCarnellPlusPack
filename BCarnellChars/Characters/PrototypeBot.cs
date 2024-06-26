using BCarnellChars.Characters.States;
using BCarnellChars.OtherStuff;
using MonoMod.Utils;
using MTM101BaldAPI;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace BCarnellChars.Characters
{
    public class PrototypeBot : NPC
    {
        private AudioManager audMan;
        public CustomSpriteAnimator animator;
        public static List<List<Sprite>> animStuff = new List<List<Sprite>>();

        public AudioManager AudMan => audMan;
        private WeightedSelection<SoundObject>[] jumpscares;
        public WeightedSelection<SoundObject>[] loseSounds => jumpscares;
        public SoundObject faststompSnd;
        private SoundObject impatientSnd;
        private SoundObject ceilingclimbSnd;

        private Action[] mechFuncts => new Action[]
        {
            SneakySnitch,
            CeilingClimb,
            DoorPrank,
            TeleportToSides
        };

        private void Awake()
        {
            audMan = gameObject.GetComponent<AudioManager>();
            audMan.ReflectionSetVariable("subtitleColor", new Color(0.3089626f, 0.1634033f, 0.6792453f, 1f));
            audMan.ReflectionSetVariable("overrideSubtitleColor", true);

            jumpscares = [
                new WeightedSelection<SoundObject>()
                {
                    selection = BasePlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/Jumpscare_01"),
                    weight = 90,
                },
                new WeightedSelection<SoundObject>()
                {
                    selection = BasePlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/Jumpscare_02"),
                    weight = 90,
                },
                new WeightedSelection<SoundObject>()
                {
                    selection = BasePlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/Jumpscare_03"),
                    weight = 90,
                },
                new WeightedSelection<SoundObject>()
                {
                    selection = BasePlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/Jumpscare_04"),
                    weight = 90,
                }
            ];
            faststompSnd = BasePlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/FastStomp");
            impatientSnd = BasePlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/Waiting");
            ceilingclimbSnd = BasePlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/Ceiling");
        }

        private void Start()
        {
            if (BasePlugin.RadarModExists && ec != null)
                Singleton<BaseGameManager>.Instance.Ec.map.AddArrow(transform, new Color(0.3089626f, 0.1634033f, 0.6792453f, 1f));
        }

        public override void Initialize()
        {
            animator.animations.Add("Idle", new CustomAnimation<Sprite>(animStuff[0].ToArray(), .9f));
            animator.animations.Add("Running", new CustomAnimation<Sprite>(animStuff[1].ToArray(), .3f));
            animator.animations.Add("Climbing", new CustomAnimation<Sprite>(animStuff[2].ToArray(), .8f));
            animator.animations.Add("ClimbOut", new CustomAnimation<Sprite>(animStuff[3].ToArray(), .7f));

            base.Initialize();
            behaviorStateMachine.ChangeState(new PrototypeBot_StateBase(this, this));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 99));
            navigator.maxSpeed = 10;
            navigator.SetSpeed(10);

            MechFunctInvoke();
        }

        public void CaughtPlayer(PlayerManager player)
        {
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
            if (!behaviorStateMachine.currentState.GetType().Equals(typeof(PrototypeBot_Stunned)))
            {
                audMan.FlushQueue(true);
                mechFuncts[Mathf.RoundToInt(UnityEngine.Random.Range(0f, mechFuncts.Length - 1f))].Invoke();
            }
        }
        public virtual IEnumerator StunHearing(float time)
        {
            behaviorStateMachine.ChangeState(new PrototypeBot_Stunned(this, this));
            audMan.FlushQueue(true);
            audMan.SetLoop(true);
            audMan.maintainLoop = true;
            audMan.QueueAudio(BasePlugin.bcppAssets.Get<SoundObject>("PrototypeBot-01/Stunned"), true);
            yield return new WaitForSecondsEnviromentTimescale(ec, time);
            audMan.FlushQueue(true);
            mechFuncts[Mathf.RoundToInt(UnityEngine.Random.Range(0f, mechFuncts.Length - 1f))].Invoke();
        }

        // Action Functions
        private void SneakySnitch()
        {
            behaviorStateMachine.ChangeState(new PrototypeBot_Sneak(this, this, players[UnityEngine.Random.RandomRangeInt(0, players.Count-1)]));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_TargetPlayer(this, 99, players[UnityEngine.Random.RandomRangeInt(0, players.Count - 1)].transform.position));
            audMan.SetLoop(true);
            audMan.maintainLoop = true;
            audMan.QueueAudio(impatientSnd, true);
        }
        private void CeilingClimb()
        {
            behaviorStateMachine.ChangeState(new PrototypeBot_Ceiling(this, this));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 0));
            audMan.SetLoop(true);
            audMan.maintainLoop = true;
            audMan.QueueAudio(ceilingclimbSnd, true);
        }
        private void DoorPrank()
        {
            audMan.SetLoop(false);
            audMan.maintainLoop = false;
            behaviorStateMachine.ChangeState(new PrototypeBot_Prank(this, this));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 0));
        }
        private void TeleportToSides() // This thing busted...
        {
            DoorPrank();
            /*audMan.SetLoop(false);
            audMan.maintainLoop = false;
            behaviorStateMachine.ChangeState(new PrototypeBot_Corner(this, this));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 1));*/
        }
    }
}

namespace BCarnellChars.Characters.States
{
    public class PrototypeBot_StateBase : NpcState
    {
        protected PrototypeBot prototypebot;
        protected internal bool flagged = false; // Was the bot teleporting infront of you a threat?

        public PrototypeBot_StateBase(NPC npc, PrototypeBot bot)
            : base(npc)
        {
            prototypebot = bot;
        }

        public override void OnStateTriggerStay(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            prototypebot.looker.Raycast(other.transform, Vector3.Magnitude(prototypebot.transform.position - other.transform.position), out var targetSighted);
            if (!targetSighted)
            {
                return;
            }

            PlayerManager component = other.GetComponent<PlayerManager>();
            if (!component.invincible)
            {
                prototypebot.CaughtPlayer(component);
            }
        }

        public override void Hear(Vector3 position, int value)
        {
            if (value >= 112 && Mathf.RoundToInt(Vector3.Distance(npc.transform.position, position)) < (245 + value))
                prototypebot.behaviorStateMachine.ChangeState(new PrototypeBot_Chase(prototypebot, prototypebot, position));
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
            float num = Vector3.Distance(npc.transform.position, npc.players[0].transform.position);
            if (num < 100f)
            {
                float signedAngle = Vector3.SignedAngle(npc.transform.position - npc.players[0].transform.position, npc.players[0].transform.forward, Vector3.up);
                InputManager.Instance.Rumble(1f - num / 100f, duration, signedAngle);
            }
        }
    }

    public class PrototypeBot_Stunned : PrototypeBot_StateBase
    {
        public PrototypeBot_Stunned(NPC npc, PrototypeBot bot)
            : base(npc, bot)
        {
        }

        public override void OnStateTriggerStay(Collider other)
        {
        }
        public override void Hear(Vector3 position, int value)
        {
        }
        public override void Enter()
        {
            base.Enter();
            prototypebot.animator.SetDefaultAnimation("Idle", 2f);
            npc.Navigator.SetSpeed(0f);
            npc.Navigator.maxSpeed = 0f;
            npc.behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(npc, 99));
        }
    }

    public class PrototypeBot_Ceiling : PrototypeBot_StateBase
    {
        public PrototypeBot_Ceiling(NPC npc, PrototypeBot bot) : base(npc, bot)
        {
        }
        private PlayerManager player;
        private int maxattempts = 30;
        private float elaspedTime;

        public override void Enter()
        {
            base.Enter();
            flagged = true;
            prototypebot.animator.SetDefaultAnimation("Climbing", 1f);
            npc.Navigator.maxSpeed = 30;
            npc.Navigator.SetSpeed(30);
            npc.Navigator.Entity.SetGrounded(false);
            for (int k = 0; k < CoreGameManager.Instance.setPlayers; k++)
                npc.Navigator.Entity.IgnoreEntity(npc.players[k].plm.Entity, true);
            player = npc.players[Mathf.RoundToInt(UnityEngine.Random.Range(0, npc.players.Count - 1))];
            bool flag = false;
            int attempts = 0;
            while (!flag && attempts < maxattempts)
            {
                List<Cell> hall = npc.ec.FindHallways()[UnityEngine.Random.RandomRangeInt(0, npc.ec.FindHallways().Count - 1)];
                npc.Navigator.Entity.Teleport(hall[Mathf.RoundToInt(UnityEngine.Random.Range(0, hall.Count - 1))].CenterWorldPosition);
                flag = true;
                attempts++;
                for (int k = 0; k < CoreGameManager.Instance.setPlayers; k++)
                {
                    if (!flag)
                        break;
                    if (Vector3.Distance(npc.transform.position, npc.players[k].transform.position) > 450f
                        || Vector3.Distance(npc.transform.position, npc.players[k].transform.position) <= 340f)
                        flag = false;
                }
            }
            if (attempts >= maxattempts)
                prototypebot.MechFunctInvoke();
            flagged = false;
        }

        public override void Update()
        {
            if (player == null | flagged)
                return;
            elaspedTime += Time.deltaTime * npc.TimeScale;
            if (elaspedTime >= 55f)
                prototypebot.MechFunctInvoke();
            if (player.plm.Entity.CurrentRoom.type != RoomType.Hall | player.hidden) // Is this fair enough??
            {
                if (npc.Navigator.HasDestination && (npc.ec.CellFromPosition(player.transform.position).room.type != RoomType.Hall | player.hidden)
                    && !npc.behaviorStateMachine.CurrentNavigationState.GetType().Equals(typeof(NavigationState_TargetPosition)))
                    npc.Navigator.ClearDestination();
                if (!npc.Navigator.HasDestination || npc.ec.CellFromPosition(npc.Navigator.CurrentDestination).room.type != RoomType.Hall)
                {
                    List<Cell> hall = npc.ec.FindHallways()[UnityEngine.Random.RandomRangeInt(0, npc.ec.FindHallways().Count - 1)];
                    ChangeNavigationState(new NavigationState_TargetPosition(npc, 99, hall[Mathf.RoundToInt(UnityEngine.Random.Range(0, hall.Count - 1))].CenterWorldPosition));
                }
                return;
            }
            float distance = Vector3.Distance(npc.transform.position, player.transform.position);
            if (distance <= 400f)
            {
                ChangeNavigationState(new NavigationState_TargetPlayer(npc, 99, player.transform.position));
                Rumble(0.1f);
                if (distance <= 80f && npc.looker.PlayerInSight(player))
                    prototypebot.StartCoroutine(animationPlay());
            }
            else
            {
                bool flag = false;
                while (!flag)
                {
                    List<Cell> hall = npc.ec.FindHallways()[UnityEngine.Random.RandomRangeInt(0, npc.ec.FindHallways().Count - 1)];
                    npc.Navigator.Entity.Teleport(hall[Mathf.RoundToInt(UnityEngine.Random.Range(0, hall.Count - 1))].CenterWorldPosition);
                    flag = true;
                    for (int k = 0; k < CoreGameManager.Instance.setPlayers; k++)
                    {
                        if (!flag)
                            break;
                        if (Vector3.Distance(npc.transform.position, npc.players[k].transform.position) > 250f
                            && Vector3.Distance(npc.transform.position, npc.players[k].transform.position) <= 180f)
                            flag = false;
                    }
                }
            }
        }

        private IEnumerator animationPlay()
        {
            npc.Navigator.maxSpeed = 5f;
            npc.Navigator.SetSpeed(5f);
            prototypebot.animator.Play("ClimbOut", 1f);
            if (prototypebot.animator.currentFrameIndex == 7)
            {
                npc.SetSpriteRotation(180f);
                npc.Navigator.Entity.SetBaseRotation(180f);
            }
            yield return prototypebot.animator.currentAnimationName == "ClimbOut";
            prototypebot.behaviorStateMachine.ChangeState(new PrototypeBot_Chase(prototypebot, prototypebot, player.transform.position));
        }

        public override void PlayerLost(PlayerManager player)
        {
            if (Vector3.Distance(npc.transform.position, player.transform.position) <= 40f)
                prototypebot.MechFunctInvoke();
        }

        public override void Exit()
        {
            npc.SetSpriteRotation(0f);
            npc.Navigator.Entity.SetBaseRotation(0f);
            npc.Navigator.Entity.SetGrounded(true);
            for (int k = 0; k < CoreGameManager.Instance.setPlayers; k++)
                npc.Navigator.Entity.IgnoreEntity(npc.players[k].plm.Entity, false);
        }
    }

    public class PrototypeBot_Corner : PrototypeBot_StateBase
    {
        public PrototypeBot_Corner(NPC npc, PrototypeBot bot) : base(npc, bot)
        {
        }

        private int maxattempts = 30;
        public override void Enter()
        {
            base.Enter();
            flagged = true;
            prototypebot.animator.SetDefaultAnimation("Running", 1.9f);
            npc.Navigator.maxSpeed = 31;
            npc.Navigator.SetSpeed(31);
            npc.Navigator.Entity.SetTrigger(false); // Don't want accidents happen...
            bool flag = false;
            int attempts = 0;
            while (!flag & attempts < maxattempts)
            {
                List<Cell> hall = npc.ec.FindHallways()[UnityEngine.Random.RandomRangeInt(0, npc.ec.FindHallways().Count - 1)];
                npc.Navigator.Entity.Teleport(hall[Mathf.RoundToInt(UnityEngine.Random.Range(0, hall.Count - 1))].CenterWorldPosition);
                flag = true;
                attempts++;
                for (int k = 0; k < CoreGameManager.Instance.setPlayers; k++)
                {
                    if (!flag)
                        break;
                    if (Vector3.Distance(npc.transform.position, npc.players[k].transform.position) <= 150f && Vector3.Distance(npc.transform.position, npc.players[k].transform.position) > 250f)
                        flag = false;
                }
            }
            if (attempts >= maxattempts)
                prototypebot.MechFunctInvoke();
            npc.Navigator.Entity.SetTrigger(true);
            player = npc.players[Mathf.RoundToInt(UnityEngine.Random.Range(0, npc.players.Count - 1))];
            flagged = false;
        }

        private PlayerManager player;
        public override void Update()
        {
            if (player)
            {
                if (player.plm.Entity.CurrentRoom.type != RoomType.Hall)
                    prototypebot.MechFunctInvoke();
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

    public class PrototypeBot_Sneak : PrototypeBot_StateBase
    {
        private PlayerManager player;

        public PrototypeBot_Sneak(NPC npc, PrototypeBot bot, PlayerManager _player)
            : base(npc, bot)
        {
            player = _player;
        }
        private int maxattempts = 30;

        public override void Enter()
        {
            base.Enter();
            flagged = true;
            prototypebot.animator.SetDefaultAnimation("Idle", 1f);
            npc.Navigator.maxSpeed = 40;
            npc.Navigator.SetSpeed(40);
            npc.Navigator.Entity.SetTrigger(false); // Don't want accidents happen...
            bool flag = false;
            int attempts = 0;
            while (!flag && attempts < maxattempts)
            {
                List<Cell> hall = npc.ec.FindHallways()[UnityEngine.Random.RandomRangeInt(0, npc.ec.FindHallways().Count - 1)];
                npc.Navigator.Entity.Teleport(hall[Mathf.RoundToInt(UnityEngine.Random.Range(0, hall.Count - 1))].CenterWorldPosition);
                flag = true;
                attempts++;
                for (int k = 0; k < CoreGameManager.Instance.setPlayers; k++)
                {
                    if (!flag)
                        break;
                    if (Vector3.Distance(npc.transform.position, npc.players[k].transform.position) <= 340f ||
                        Vector3.Distance(npc.transform.position, npc.players[k].transform.position) > 400f)
                        flag = false;
                }
            }
            npc.Navigator.Entity.SetTrigger(true);
            if (attempts >= maxattempts)
                prototypebot.MechFunctInvoke();
            flagged = false;
        }

        public override void Update()
        {
            float distance = Vector3.Distance(npc.transform.position, player.transform.position);
            if (distance <= 400f)
            {
                if (!player.hidden)
                    ChangeNavigationState(new NavigationState_TargetPlayer(npc, 99, player.transform.position));
                else
                    ChangeNavigationState(new NavigationState_WanderRandom(npc, 100));
                if (distance < 100f && !npc.looker.IsVisible && !prototypebot.AudMan.QueuedAudioIsPlaying)
                    prototypebot.AudMan.Pause(false);
            }
            else
                prototypebot.MechFunctInvoke();
            if (distance >= 35f && !npc.looker.IsVisible)
                npc.Navigator.SetSpeed(25f);
            else if (!flagged && ((distance < 65f && npc.looker.IsVisible) || distance < 29f) && npc.looker.PlayerInSight())
                prototypebot.StartCoroutine(isReady());
            else
            {
                npc.Navigator.SetSpeed(0f);
                if (prototypebot.AudMan.QueuedAudioIsPlaying)
                    prototypebot.AudMan.Pause(true);
            }

            if (prototypebot.AudMan.QueuedAudioIsPlaying)
                Rumble(0.1f);
        }

        private IEnumerator isReady()
        {
            npc.Navigator.maxSpeed = 0;
            npc.Navigator.SetSpeed(0);
            yield return new WaitForSecondsNPCTimescale(npc.ec, 0.4f);
            prototypebot.behaviorStateMachine.ChangeState(new PrototypeBot_Chase(prototypebot, prototypebot, player.transform.position));
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
                prototypebot.behaviorStateMachine.ChangeState(new PrototypeBot_StateBase(prototypebot, prototypebot));
            else
                ChangeNavigationState(new NavigationState_TargetPlayer(npc, 99, player.transform.position));
        }

        public override void OnStateTriggerStay(Collider other)
        {
        }
    }

    public class PrototypeBot_Prank : PrototypeBot_StateBase
    {
        private float passedTime = 0f;
        private Door nearestDoor;
        public PrototypeBot_Prank(NPC npc, PrototypeBot bot)
            : base(npc, bot)
        {
        }

        public override void Enter()
        {
            base.Enter();
            flagged = true;
            prototypebot.animator.SetDefaultAnimation("Idle", 1f);
            npc.Navigator.maxSpeed = 0;
            npc.Navigator.SetSpeed(0);
            if (BaseGameManager.Instance.FoundNotebooks < 1) {
                prototypebot.MechFunctInvoke();
                return;
            }
            List<RoomController> peek = new List<RoomController>();
            foreach (RoomController room in npc.ec.rooms)
            {
                if (room == null) continue;
                for (int k = 0; k < CoreGameManager.Instance.setPlayers; k++)
                {
                    if (room.category != RoomCategory.Class || room.ec.activities.Find(x => x.room == room & !x.IsCompleted)
                        || Vector3.Distance(npc.ec.RealRoomMid(room), npc.players[k].transform.position) > 360f || Vector3.Distance(npc.ec.RealRoomMid(room), npc.players[k].transform.position) <= 190f)
                        continue;
                    //Debug.Log("Adding classroom");
                    peek.Add(room);
                }
            }
            if (peek.Count > 0) {
                npc.Navigator.Entity.Teleport(npc.ec.RealRoomMid(peek[Mathf.RoundToInt(UnityEngine.Random.Range(0, peek.Count - 1))]));
                float nearest = float.PositiveInfinity;
                foreach (var tile in npc.Navigator.Entity.CurrentRoom.AllTilesNoGarbage(false, true))
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
            }
            else
                prototypebot.MechFunctInvoke();
            flagged = false;
        }

        public override void Update()
        {
            if (nearestDoor == null)
                return;
            passedTime += ((BaseGameManager.Instance.FoundNotebooks / BaseGameManager.Instance.NotebookTotal) * 2) * (Time.deltaTime * npc.ec.NpcTimeScale);
            float distance = Vector3.Distance(npc.ec.RealRoomMid(npc.Navigator.Entity.CurrentRoom), npc.players[0].transform.position);
            float doorDist = Vector3.Distance(nearestDoor.transform.position, npc.players[0].transform.position);
            Physics.Raycast(nearestDoor.transform.position, npc.players[0].transform.position - nearestDoor.transform.position, out RaycastHit playerHit, float.PositiveInfinity, LayerMask.GetMask("Default", "Player"), QueryTriggerInteraction.Ignore);
            if (distance >= 360f || passedTime >= (20f * BaseGameManager.Instance.FoundNotebooks))
                prototypebot.MechFunctInvoke();
            if ((doorDist < 30f | playerHit.transform.CompareTag("Player")) & !flagged)
                prototypebot.StartCoroutine(isReady(npc.players[0]));
        }

        public override void PlayerInSight(PlayerManager player)
        {
            prototypebot.StartCoroutine(isReady(player));
        }

        private IEnumerator isReady(PlayerManager player)
        {
            yield return new WaitForSecondsNPCTimescale(npc.ec, 0.4f);
            prototypebot.behaviorStateMachine.ChangeState(new PrototypeBot_Chase(prototypebot, prototypebot, player.transform.position));
        }

        public override void Exit()
        {
        }
    }

    public class PrototypeBot_Chase : PrototypeBot_StateBase
    {
        /*private float stompDistance = 5f;
        private float toNextStomp;*/

        private Vector3 position;
        public PrototypeBot_Chase(NPC npc, PrototypeBot bot, Vector3 pos)
            : base(npc, bot)
        {
            position = pos;
        }

        public override void Enter()
        {
            base.Enter();
            prototypebot.animator.SetDefaultAnimation("Running", 1f);
            prototypebot.AudMan.FlushQueue(true);
            prototypebot.AudMan.SetLoop(false);
            prototypebot.AudMan.maintainLoop = false;
            npc.Navigator.maxSpeed = 70;
            npc.Navigator.SetSpeed(70);
            ChangeNavigationState(new NavigationState_TargetPosition(npc, 9, position));
        }

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

            if (prototypebot.animator.currentAnimationName == "Running" && Time.timeScale != 0)
            {
                if (prototypebot.animator.currentFrameIndex % 3 == 0) {
                    prototypebot.AudMan.PlaySingle(prototypebot.faststompSnd);
                    Rumble(0.3f);
                }
            }
        }

        public override void PlayerInSight(PlayerManager player)
        {
            ChangeNavigationState(new NavigationState_TargetPosition(npc, 9, player.transform.position));
        }

        public override void DestinationEmpty()
        {
            prototypebot.MechFunctInvoke();
        }
    }
}
