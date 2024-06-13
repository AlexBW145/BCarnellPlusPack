using BCarnellChars.Characters.States;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using MTM101BaldAPI.Registers;
using HarmonyLib;
using MTM101BaldAPI.Reflection;

namespace BCarnellChars.Characters
{
    public class ERRORBOT : NPC
    {
        private AudioManager audMan;
        private AudioManager staticMan;

        private SoundObject audAlarm;
        private SoundObject[] audItemGet;
        private SoundObject audScan;
        private SoundObject bang;
        private SoundObject splat;

        // WELL, I GUESS THAT THIS BOT IS STUPIDLY ANNOYING... THEN MAKE IT STOP MOVING WHEN THE PLAYER IS LOOKING AT ITS POSITION! (And that's why camMask exists...)
        public LayerMask regularMask => LayerMask.GetMask("Default", "Block Raycast", "Player", "Windows");
        public LayerMask camMask => LayerMask.GetMask("Player");

        private void Awake()
        {
            audMan = gameObject.GetComponents<AudioManager>()[0];
            audMan.audioDevice = gameObject.AddComponent<AudioSource>();
            staticMan = gameObject.GetComponents<AudioManager>()[1];

            audAlarm = BasePlugin.bcppAssets.Get<SoundObject>("ERRORBOT/Alarm");
            audItemGet = [
                BasePlugin.bcppAssets.Get<SoundObject>("ERRORBOT/MineNow"), BasePlugin.bcppAssets.Get<SoundObject>("ERRORBOT/ByeToNeeds")
                ];
            audScan = BasePlugin.bcppAssets.Get<SoundObject>("ERRORBOT/Scanning");
            bang = BasePlugin.bcppAssets.Get<SoundObject>("ERRORBOT/Jammed");
            splat = BasePlugin.bcppAssets.Get<SoundObject>("ERRORBOT/Sprayed");
        }

        private void Start()
        {
            if (BasePlugin.RadarModExists && ec != null)
                Singleton<BaseGameManager>.Instance.Ec.map.AddArrow(transform, RadarSupport.ERRORBOTColor.Value);
        }

        public override void Initialize()
        {
            base.Initialize();
            StartCooldown();
        }

        public void StartCooldown()
        {
            looker.ReflectionSetVariable("layerMask", regularMask);
            Teleport();
            audMan.SetLoop(false);
            audMan.FlushQueue(true);
            behaviorStateMachine.ChangeState(new ERRORBOT_Cooldown(this, this, UnityEngine.Random.RandomRangeInt(60, 120)));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 99));
            navigator.maxSpeed = 0;
            navigator.SetSpeed(0);
            spriteRenderer[1].color = Color.grey;
        }

        public void EndCooldown()
        {
            Teleport();
            //audMan.PlaySingle(audScan);
            behaviorStateMachine.ChangeState(new ERRORBOT_Active(this, this));
            spriteRenderer[1].color = Color.white;
        }

        private void Teleport()
        {
            Cell[] list = ec.AllTilesNoGarbage(false, false).ToArray();
            List<Cell> needed = new List<Cell>();
            foreach(Cell cell in list)
            {
                if (cell.room.category == RoomCategory.Hall)
                    needed.Add(cell);
            }
            var spawn = needed[UnityEngine.Random.Range(0, needed.Count)];
            bool flag = false;
            while (!flag)
            {
                spawn = needed[UnityEngine.Random.Range(0, needed.Count)];
                flag = true;
                for (int k = 0; k < Singleton<CoreGameManager>.Instance.setPlayers; k++)
                {
                    if (!flag)
                    {
                        break;
                    }
                    if ((spawn.FloorWorldPosition + Vector3.up * 5f - Singleton<CoreGameManager>.Instance.GetPlayer(k).transform.position).magnitude <= 20f || ec.TrapCheck(spawn))
                    {
                        flag = false;
                    }
                }
            }
            transform.position = spawn.FloorWorldPosition + Vector3.up * 5f;
        }

        public void BecomeEvil(bool hasItems, PlayerManager player)
        {
            looker.ReflectionSetVariable("layerMask", regularMask);
            if (hasItems)
            {
                looker.ReflectionSetVariable("layerMask", camMask);
                navigator.maxSpeed = 25f;
                behaviorStateMachine.ChangeState(new ERRORBOT_WithItemsMode(this, this, player));
            }
            else
                behaviorStateMachine.ChangeState(new ERRORBOT_NoItemsMode(this, this, player));
        }

        public void Alert(PlayerManager player)
        {
            transform.position = player.transform.position;
            if (GetComponent<Entity>().Squished)
                return;
            behaviorStateMachine.ChangeState(new ERRORBOT_FinalCooldown(this, this, 120f));
            audMan.QueueAudio(audAlarm, true);
            audMan.SetLoop(true);
            ec.MakeNoise(transform.position, 112); // You're fucked.
            if (player.Disobeying && (player.ruleBreak == "Faculty" || player.ruleBreak == "Escaping") && ec.Npcs.Find(p => p.Character == Character.Principal))
            {
                Principal pri = ec.Npcs.Find(p => p.Character == Character.Principal).GetComponent<Principal>();
                if (!(bool)pri.ReflectionGetVariable("allKnowing"))
                    pri.behaviorStateMachine.ChangeState(new Principal_ChasingPlayer(pri, player));
                else
                    pri.behaviorStateMachine.ChangeState(new Principal_ChasingPlayer_AllKnowing(pri, player));

                pri.ReflectionSetVariable("targetedPlayer", player);
                pri.WhistleReact(transform.position);
                pri.Scold(player.ruleBreak);
            }
        }

        public void SayByeByeToYourItems(PlayerManager player)
        {
            behaviorStateMachine.ChangeState(new ERRORBOT_FinalCooldown(this, this, 0.2f));
            Singleton<CoreGameManager>.Instance.audMan.PlayRandomAudio(audItemGet);
            player.itm.RemoveRandomItem();
        }

        public void Jammed(Collider other)
        {
            if (other.CompareTag("GrapplingHook") || other.GetComponent<ITM_BSODA>()) // Pierced
            {
                looker.ReflectionSetVariable("layerMask", regularMask);
                spriteRenderer[1].color = other.GetComponent<ITM_BSODA>() ? Color.blue : Color.grey;
                behaviorStateMachine.ChangeState(new ERRORBOT_Cooldown(this, this, UnityEngine.Random.RandomRangeInt(60, 120)));
                behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 99));
                navigator.maxSpeed = 0;
                navigator.SetSpeed(0);
                audMan.PlaySingle(other.GetComponent<ITM_BSODA>() ? splat : bang);
            }
        }
    }
}
