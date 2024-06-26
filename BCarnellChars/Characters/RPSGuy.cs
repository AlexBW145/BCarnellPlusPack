﻿using MTM101BaldAPI.AssetTools;
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
        private SoundObject[] audCongrats;
        private SoundObject audWannaPlay;
        private SoundObject audFine;
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
            alive = BasePlugin.bcppAssets.Get<Sprite>("RPSGuy/Alive");
            dead = BasePlugin.bcppAssets.Get<Sprite>("RPSGuy/Dead");

            audCongrats = [BasePlugin.bcppAssets.Get<SoundObject>("RPSGuy/LostGame"), BasePlugin.bcppAssets.Get<SoundObject>("RPSGuy/LostGameQuote2")];
            audYouLose = BasePlugin.bcppAssets.Get<SoundObject>("RPSGuy/WonGame");
            audDestroyed = BasePlugin.bcppAssets.Get<SoundObject>("RPSGuy/Killed");
            audWannaPlay = BasePlugin.bcppAssets.Get<SoundObject>("RPSGuy/WannaPlay");
            audFine = BasePlugin.bcppAssets.Get<SoundObject>("RPSGuy/LostSightOfPlayer");

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
            behaviorStateMachine.ChangeNavigationState(new NavigationState_TargetPlayer(this, 63, player.transform.position));
            if (!audMan.audioDevice.isPlaying)
                audMan.PlaySingle(audWannaPlay);
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
                audMan.PlayRandomAudio(audCongrats);
                streakPoints += streakInitial;
            }
            if (destroyGame) {
                curRPS.Destroy();
                if (!won && !isDead)
                    audMan.PlaySingle(audFine);
            }
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
                navigator.maxSpeed = normSpeed;
                navigator.SetSpeed(normSpeed);
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
                curRPS.Destroy();
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
            navigator.Entity.AddForce(new Force(transform.position - player.transform.position, 20f, -60f));
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
