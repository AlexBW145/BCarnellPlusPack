using BCarnellChars.Characters.States;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace BCarnellChars.Characters
{
    public class MrRobber : NPC
    {
        [SerializeField] internal Canvas overlay;
        private MovementModifier playerMod = new MovementModifier(Vector3.zero, 0.1f);

        [SerializeField] internal AudioManager audMan, sfxMan;

        [SerializeField] internal List<SoundObject> voiceIntro = new List<SoundObject>(), voiceFlee = new List<SoundObject>();
        [SerializeField] internal SoundObject sfxBeat;
        [SerializeField] internal SoundObject[] sfxScreech;

        [SerializeField] internal Sprite wanderingSprite, huntingSprite;

        public override void Initialize()
        {
            this.ChangeRadarColor();
            base.Initialize();
            navigator.accel = 1000f;
            behaviorStateMachine.ChangeState(new MrRobber_Hunting(this));
        }

        protected override void VirtualUpdate()
        {
            if (behaviorStateMachine.CurrentNavigationState.GetType() == typeof(NavigationState_PartyEvent))
                behaviorStateMachine.ChangeNavigationState(new NavigationState_WanderRandom(this, 1));

            if (behaviorStateMachine.CurrentState.GetType() == typeof(MrRobber_Hunting))
            {
                if (spriteRenderer[0].color == Color.clear)
                {
                    if (sfxMan.volumeModifier > 0f)
                        sfxMan.volumeModifier -= .1f * (Time.deltaTime * TimeScale);
                    else if (sfxMan.volumeModifier <= 0f)
                        sfxMan.volumeModifier = 0f;
                }
                else
                {
                    if (sfxMan.volumeModifier < 1f)
                        sfxMan.volumeModifier += .1f * (Time.deltaTime * TimeScale);
                    else if (sfxMan.volumeModifier >= 1f)
                        sfxMan.volumeModifier = 1f;
                }
                return;
            }
            else if (sfxMan.volumeModifier < 1f)
                sfxMan.volumeModifier = 1f;
            sfxMan.pitchModifier += .01f * (Time.deltaTime * TimeScale);
        }

        public IEnumerator GoNuts()
        {
            if (behaviorStateMachine.CurrentState.GetType() == typeof(MrRobber_Crazy))
                yield break;
            navigator.SetRoomAvoidance(false);
            navigator.maxSpeed = 0f;
            navigator.SetSpeed(0f);
            behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 1));
            behaviorStateMachine.ChangeState(new MrRobber_Crazy(this));

            audMan.QueueRandomAudio(voiceIntro.ToArray());
            audMan.QueueAudio(sfxScreech[0]);
            audMan.QueueAudio(sfxScreech[1]);
            sfxMan.FlushQueue(false);
            spriteRenderer[0].sprite = huntingSprite;

            while (sfxMan.QueuedAudioIsPlaying)
                yield return null;

            sfxMan.QueueAudio(sfxBeat);
            sfxMan.SetLoop(true);

            while (audMan.audioSourceManager.GetAudioSource(SoundType.Effect).clip != sfxScreech[0].soundClip)
                yield return null;

            sfxMan.audioSourceManager.dopplerLevel = 1f;
            navigator.maxSpeed = 30f;
            navigator.SetSpeed(30f);
            behaviorStateMachine.ChangeNavigationState(new NavigationState_WanderRandom(this, 1));

            while (audMan.audioSourceManager.GetAudioSource(SoundType.Effect).clip != sfxScreech[1].soundClip)
                yield return null;
            audMan.SetLoop(true);

            yield break;
        }

        public void DoneFor(PlayerManager player)
        {
            if (behaviorStateMachine.CurrentState.GetType() != typeof(MrRobber_Crazy))
                return;
            AudioMixer mixer = Resources.FindObjectsOfTypeAll<AudioMixer>().ToList().Find(x => x.name.ToLower() == "master");

            player.plm.am.moveMods.Add(playerMod);
            overlay.gameObject.SetActive(true);
            overlay.worldCamera = CoreGameManager.Instance.GetCamera(player.playerNumber).canvasCam;

            sfxMan.FlushQueue(true);
            sfxMan.audioSourceManager.dopplerLevel = 0f;
            spriteRenderer[0].color = Color.clear;

            player.itm.RemoveRandomItem();
            player.itm.RemoveRandomItem();
            player.itm.RemoveRandomItem();
            player.plm.stamina = -10f;

            AudioListener.volume = 0f;
            StartCoroutine(volumeListenerRise(player, mixer));

            audMan.FlushQueue(true);
            audMan.PlayRandomAudio(voiceFlee.ToArray());
            behaviorStateMachine.ChangeState(new MrRobber_DoNothing(this));
        }

        private IEnumerator volumeListenerRise(PlayerManager player, AudioMixer mixer)
        {
            var audioJungle = 0f;
            var idkhow = 1f;
            Image whiteBang = overlay.transform.Find("Image").GetComponent<Image>();
            while (AudioListener.volume < 1f)
            {
                audioJungle += .06f * (Time.deltaTime * TimeScale);
                AudioListener.volume = audioJungle;
                if (idkhow > 0f)
                {
                    idkhow -= .1f * (Time.deltaTime * TimeScale);
                    whiteBang.color = new Color(1f, 1f, 1f, idkhow);
                }
                else
                {
                    idkhow = 0f;
                    overlay.gameObject.SetActive(false);
                }
                yield return null;
            }
            AudioListener.volume = 1f;
            player.plm.am.moveMods.Remove(playerMod);
            overlay.gameObject.SetActive(false);
            whiteBang.color = Color.white;
            yield break;
        }

        private void OnDestroy()
        {
            if (behaviorStateMachine.CurrentState is MrRobber_DoNothing && overlay?.gameObject.activeSelf == true && AudioListener.volume < 1f)
                AudioListener.volume = 1f;
        }
    }
}