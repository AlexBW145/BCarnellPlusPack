using MTM101BaldAPI;
using UnityEngine;
using BCarnellChars.Characters.States;
using BCarnellChars.OtherStuff;
using System.Linq;
using BCarnellChars.Patches;

namespace BCarnellChars.Characters
{
    public class RPSGuy : NPC, IItemAcceptor
    {
        [SerializeField] internal Sprite alive, dead;

        private RockPaperScissors curRPS;
        [SerializeField] internal RockPaperScissors rpsPre;

        [SerializeField] internal AudioManager audMan;
        /*private SoundObject audCall = new SoundObject();
        private SoundObject audLetsPlay;
        private SoundObject audGo;*/
        [SerializeField] internal SoundObject[] audCongrats;
        [SerializeField] internal SoundObject audWannaPlay, audFine, audYouLose, audDestroyed;

        private float normSpeed = 12f, runSpeed = 20f, initialCooldown = 25f;

        private bool isDead = false, playing = false;
        public bool Dead => isDead;
        public bool Playing => playing;
        private readonly int streakInitial = 15;
        private int StreakInitTotal => streakInitial + (streakInitial * Mathf.CeilToInt(1.05f * StickerManager.Instance.StickerValue(StickerPatches.rpsSticker)));
        private int streakPoints;
        public int StreakPoints => streakPoints;

        public bool ItemFits(Items item)
        {
            return new string[] {"BHammer".ToLower(), "Hammer".ToLower()}.Contains(item.ToStringExtended().ToLower()) && playing && !isDead;
        }
        public void InsertItem(PlayerManager player, EnvironmentController ec)
        {
            fuckingDies();
        }

        public override void Initialize()
        {
            this.ChangeRadarColor();
            base.Initialize();
            streakPoints = StreakInitTotal;
            behaviorStateMachine.ChangeState(new RPSGuy_Wandering(this, this));
            navigator.maxSpeed = normSpeed;
            navigator.SetSpeed(normSpeed);
        }

        public override void Despawn()
        {
            if (curRPS != null) EndRPS(false, true);
            base.Despawn();
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
                streakPoints += StreakInitTotal;
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
            streakPoints = StreakInitTotal;
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

        public void StartRPS(PlayerManager player)
        {
            playing = true;
            if (curRPS != null)
                curRPS.Destroy();
            // IF PLAYTIME IS PLAYIN' WITH PLAYER, MAKE HER SAD BCUS SHE WAS INTERRUPTED!!
            if (player.jumpropes.Count > 0)
            {
                while (player.jumpropes.Count > 0)
                    player.jumpropes[0].End(false);
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
