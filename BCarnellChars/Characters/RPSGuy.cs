using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI;
using MTM101BaldAPI.Components;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using BCarnellChars.Characters.States;
using BCarnellChars.OtherStuff;

namespace BCarnellChars.Characters
{
    public class RPSGuy : NPC, IItemAcceptor
    {
        private Sprite alive;

        private Sprite dead;

        private RockPaperScissors curRPS;
        public RockPaperScissors rpsPre;

        private AudioManager audMan;
        /*private SoundObject audCall = new SoundObject();
        private SoundObject audLetsPlay;
        private SoundObject audGo;*/
        private SoundObject audCongrats;
        private SoundObject audYouLose;
        private SoundObject audDestroyed;

        private float normSpeed = 12f;
        private float runSpeed = 20f;
        private float initialCooldown = 15f;
        private float calloutChance = 0.05f;

        private bool isDead = false;
        public bool Dead => isDead;
        private bool playing = false;
        public bool Playing => playing;
        private readonly int streakInitial = 15;
        private int streakPoints = 15;
        public int StreakPoints => streakPoints;

        public bool ItemFits(Items item)
        {
            return item == (EnumExtensions.GetFromExtendedName<Items>("BHammer") | EnumExtensions.GetFromExtendedName<Items>("Hammer"));
        }
        public void InsertItem(PlayerManager player, EnvironmentController ec)
        {
            if (playing)
                fuckingDies();
        }

        private void Awake()
        {
            audMan = gameObject.GetComponents<PropagatedAudioManager>()[0];
            audMan.audioDevice = gameObject.AddComponent<AudioSource>();
            alive = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(BasePlugin.Instance, "Texture2D", "NPCs", "RPS Guy", "RPSDude.png"), 100);
            dead = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(BasePlugin.Instance, "Texture2D", "NPCs", "RPS Guy", "RPSDead.png"), 100);

            audCongrats = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(BasePlugin.Instance, "AudioClip", "NPCs", "RPS Guy", "RPS_lose.wav"), "Vfx_RPS_lost", SoundType.Voice, new Color(0.7176471f, 0.6941177f, 0.6235294f));
            audYouLose = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(BasePlugin.Instance, "AudioClip", "NPCs", "RPS Guy", "RPS_win.wav"), "Vfx_RPS_win", SoundType.Voice, new Color(0.7176471f, 0.6941177f, 0.6235294f));
            audDestroyed = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(BasePlugin.Instance, "AudioClip", "NPCs", "RPS Guy", "RPS_dies.wav"), "Sfx_AppleCrunch", SoundType.Effect, Color.white);

            spriteRenderer[0].sprite = alive;
        }

        private void Start()
        {
            if (BasePlugin.RadarModExists && ec != null)
                Singleton<BaseGameManager>.Instance.Ec.map.AddArrow(transform, RadarSupport.RPSGuyColor.Value);
        }

        public override void Initialize()
        {
            base.Initialize();
            behaviorStateMachine.ChangeState(new RPSGuy_Wandering(this, this));
            navigator.maxSpeed = normSpeed;
            navigator.SetSpeed(normSpeed);
        }

        protected override void VirtualUpdate()
        {
            base.VirtualUpdate();
        }

        public void PersuePlayer(PlayerManager player)
        {
            behaviorStateMachine.CurrentNavigationState.UpdatePosition(player.transform.position);
            if (!isDead)
                navigator.maxSpeed = runSpeed;
            navigator.SetSpeed(runSpeed);
        }

        public void StartPersuingPlayer(PlayerManager player)
        {
            if (!audMan.audioDevice.isPlaying)
            {
                behaviorStateMachine.ChangeNavigationState(new NavigationState_TargetPlayer(this, 63, player.transform.position));
                //audMan.PlaySingle(audLetsPlay);
            }
        }

        public void PlayerTurnAround(PlayerManager player)
        {
            Directions.ReverseList(navigator.currentDirs);
            behaviorStateMachine.ChangeNavigationState(new NavigationState_WanderRandom(this, 0));
            if (!isDead)
                navigator.maxSpeed = normSpeed;
            navigator.SetSpeed(normSpeed);
        }

        public void EndRPS(bool won, bool destroyGame)
        {
            if (won)
            {
                CoreGameManager.Instance.AddPoints(streakPoints, curRPS.player.playerNumber, true);
                if (!GlobalCam.Instance.TransitionActive) GlobalCam.Instance.Transition(UiTransition.Dither, 0.01666667f);
                audMan.PlaySingle(audCongrats);
                streakPoints += streakInitial;
            }
            if (destroyGame)
                curRPS.Destroy();
            if (!isDead)
            {
                navigator.maxSpeed = normSpeed;
                navigator.SetSpeed(normSpeed);
                behaviorStateMachine.ChangeState(new RPSGuy_Cooldown(this, this, initialCooldown));
            }
            playing = false;
        }

        // What a waste...
        public void Losah()
        {
            if (!GlobalCam.Instance.TransitionActive) GlobalCam.Instance.Transition(UiTransition.Dither, 0.01666667f);
            audMan.PlaySingle(audYouLose);
            streakPoints = streakInitial;
        }

        public void EndCooldown()
        {
            if (isDead)
            {
                spriteRenderer[0].sprite = alive;
                isDead = false;
            }
            behaviorStateMachine.ChangeState(new RPSGuy_Wandering(this, this));
        }

        public void CalloutChance()
        {
            if (UnityEngine.Random.value <= calloutChance)
            {
                RandomCallout();
            }
        }

        private void RandomCallout()
        {
            /*int num = UnityEngine.Random.Range(0, audCalls.Length);
            if (!audMan.audioDevice.isPlaying)
            {
                audMan.PlaySingle(audCalls[num]);
            }*/
        }

        public void StartRPS(PlayerManager player)
        {
            playing = true;
            if (curRPS != null)
                Destroy(curRPS);
            // IF PLAYTIME IS PLAYIN' WITH PLAYER, MAKE HER SAD BCUS SHE WAS INTERRUPTED!!
            if (player.jumpropes.Count > 0)
            {
                Playtime playtime = FindObjectOfType<Playtime>();
                playtime.EndJumprope(false);
            }
            curRPS = Instantiate(rpsPre);
            curRPS.gameObject.SetActive(true);
            if (!GlobalCam.Instance.TransitionActive) GlobalCam.Instance.FadeIn(UiTransition.Dither, 0.01666667f);
            curRPS.player = player;
            curRPS.rps = this;
            navigator.maxSpeed = 0f;
            navigator.Entity.AddForce(new Force(base.transform.position - player.transform.position, 20f, -60f));
            //audMan.PlaySingle(audGo);
            behaviorStateMachine.ChangeState(new RPSGuy_Playing(this, this));
        }

        public void fuckingDies()
        {
            if (isDead) return;
            isDead = true;
            if (curRPS != null) EndRPS(false, true);
            behaviorStateMachine.ChangeState(new RPSGuy_Cooldown(this, this, 30f));
            audMan.FlushQueue(true);
            audMan.PlaySingle(audDestroyed);
            spriteRenderer[0].sprite = dead;
            navigator.maxSpeed = 0f;
            navigator.SetSpeed(0f);
        }
    }
}
