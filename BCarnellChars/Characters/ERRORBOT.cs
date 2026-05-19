using BCarnellChars.Characters.States;
using System.Collections.Generic;
using UnityEngine;
using MTM101BaldAPI.Reflection;
using BCarnellChars.OtherStuff;

namespace BCarnellChars.Characters
{
    public class ERRORBOT : NPC
    {
        [SerializeField] internal AudioManager audMan, staticMan;

        [SerializeField] internal SoundObject audAlarm, audScan, bang, splat;
        [SerializeField] internal SoundObject[] audItemGet;

        // WELL, I GUESS THAT THIS BOT IS STUPIDLY ANNOYING... THEN MAKE IT STOP MOVING WHEN THE PLAYER IS LOOKING AT ITS POSITION! (And that's why camMask exists...)
        public LayerMask regularMask => LayerMask.GetMask("Default", "Block Raycast", "Player", "Windows");
        public LayerMask camMask => LayerMask.GetMask("Player");

        public override void Initialize()
        {
            this.ChangeRadarColor();
            base.Initialize();
            StartCooldown();
        }

        public void StartCooldown()
        {
            looker.ReflectionSetVariable("layerMask", regularMask);
            Teleport();
            audMan.SetLoop(false);
            audMan.FlushQueue(true);
            behaviorStateMachine.ChangeState(new ERRORBOT_Cooldown(this, UnityEngine.Random.RandomRangeInt(90, 120)));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 99));
            navigator.maxSpeed = 0;
            navigator.SetSpeed(0);
            spriteRenderer[1].color = Color.grey;
        }

        public void EndCooldown()
        {
            Teleport();
            //audMan.PlaySingle(audScan);
            behaviorStateMachine.ChangeState(new ERRORBOT_Active(this));
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
                for (int k = 0; k < CoreGameManager.Instance.setPlayers; k++)
                {
                    if (!flag)
                        break;
                    if ((spawn.CenterWorldPosition - CoreGameManager.Instance.GetPlayer(k).transform.position).magnitude <= 20f || ec.TrapCheck(spawn))
                        flag = false;
                }
            }
            transform.position = spawn.CenterWorldPosition;
        }

        public void BecomeEvil(bool hasItems, PlayerManager player)
        {
            looker.ReflectionSetVariable("layerMask", regularMask);
            if (hasItems)
            {
                looker.ReflectionSetVariable("layerMask", camMask);
                navigator.maxSpeed = 25f;
                behaviorStateMachine.ChangeState(new ERRORBOT_WithItemsMode(this, player));
            }
            else
                behaviorStateMachine.ChangeState(new ERRORBOT_NoItemsMode(this, player));
        }

        protected static readonly HashSet<string> lockedinRuleBreaks = new HashSet<string>() // In case of compats...
        {
            "Faculty",
            "Escaping",
            "AfterHours"
        };
        public static void AddLockedInRuleBreak(string rule) // Remember that you need to call this on a PostLoad function. (Don't you dare call it in during gameplay time...)
        {
            if (lockedinRuleBreaks.Contains(rule) | string.IsNullOrWhiteSpace(rule)) {
                Debug.LogWarning(string.IsNullOrWhiteSpace(rule) ? "String cannot be blank for a locked in rule break!" : "ERROR-BOT_ITEM-STEALER already has a locked in rule break: " + rule);
                return;
            }

            lockedinRuleBreaks.Add(rule);
        }
        public void Alert(PlayerManager player)
        {
            transform.position = player.transform.position;
            if (GetComponent<Entity>().Squished)
                return;
            behaviorStateMachine.ChangeState(new ERRORBOT_FinalCooldown(this, 120f));
            audMan.QueueAudio(audAlarm, true);
            audMan.SetLoop(true);
            ec.MakeNoise(transform.position, 112); // You're fucked.
            if (player.Disobeying && lockedinRuleBreaks.Contains(player.ruleBreak) && ec.Npcs.Find(p => p.Character == Character.Principal))
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
            behaviorStateMachine.ChangeState(new ERRORBOT_FinalCooldown(this, 0.2f));
            Singleton<CoreGameManager>.Instance.audMan.PlayRandomAudio(audItemGet);
            if (!navigator.Entity.Squished)
                player.itm.RemoveRandomItem();
        }

        public void Jammed(Entity other)
        {
            if (other.CompareTag("GrapplingHook") || other.GetComponent<ITM_GrapplingHook>()
                || other.GetComponent<ITM_BSODA>() || other.GetComponent<SiegeCartBalls>()
                || other.GetComponent<Gum>()) // Pierced
            {
                looker.ReflectionSetVariable("layerMask", regularMask);
                spriteRenderer[1].color = other.GetComponent<ITM_BSODA>() ? Color.blue : other.GetComponent<Gum>() ? Color.magenta : Color.grey;
                behaviorStateMachine.ChangeState(new ERRORBOT_Cooldown(this, 120f));
                behaviorStateMachine.ChangeNavigationState(new NavigationState_DoNothing(this, 99));
                navigator.maxSpeed = 0;
                navigator.SetSpeed(0);
                audMan.PlaySingle((other.GetComponent<ITM_BSODA>() || other.GetComponent<Gum>()) ? splat : bang);
            }
        }
    }
}
