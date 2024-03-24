using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BCarnellChars.Characters
{
    public class RockPaperScissors : MonoBehaviour
    {
        private void Awake()
        {
            // Apparently, this is too errorish with GetComponentInChildren<T>(), meaning that it'll fail to find.
            textCanvas = transform.Find("TextCanvas").GetComponent<Canvas>();
            textScaler = transform.Find("TextCanvas").GetComponent<CanvasScaler>();
            instructionsTmp = textCanvas.transform.Find("Instructions").GetComponent<TMP_Text>();
        }
        private void Start()
        {
            if (Singleton<BaseGameManager>.Instance == null)
                return;
            ec = Singleton<BaseGameManager>.Instance.Ec;
            publicActive = true;
            transform.position = player.cameraBase.position;
            player.plm.am.moveMods.Add(moveMod);
            Singleton<CoreGameManager>.Instance.GetCamera(player.playerNumber).UpdateTargets(transform, 24);
            textCanvas.worldCamera = Singleton<CoreGameManager>.Instance.GetCamera(player.playerNumber).canvasCam;
            textCanvas.transform.SetParent(null);
            textCanvas.transform.position = Vector3.zero;
            if (!Singleton<PlayerFileManager>.Instance.authenticMode)
            {
                // Imagine using the mod menu for this...
                textScaler.scaleFactor = (float)Mathf.RoundToInt((float)Singleton<PlayerFileManager>.Instance.resolutionY / 360f);
            }
            instructionsTmp.text = "Press " + Singleton<InputManager>.Instance.GetInputButtonName("Item1") + " For Rock \nPress " + Singleton<InputManager>.Instance.GetInputButtonName("Item2") + " for Paper \nPress " + Singleton<InputManager>.Instance.GetInputButtonName("Item3") + " for Scissors";
        }

        // Token: 0x060000A3 RID: 163 RVA: 0x000068EC File Offset: 0x00004AEC
        private void Update()
        {
            if (Singleton<BaseGameManager>.Instance == null)
                return;
            transform.position = player.transform.position;
            transform.rotation = player.transform.rotation;
            if (checkDelay > 0)
                return;
            if (!pickedChoice)
            {
                opponentChoice = (int)Mathf.RoundToInt(UnityEngine.Random.Range(0f, 2f)); //Pick a random type between 0 and 2

                // Cannot use switch statements over this
                if (Singleton<InputManager>.Instance.GetDigitalInput("Item1", true))
                    playerChoice = 0;
                else if (Singleton<InputManager>.Instance.GetDigitalInput("Item2", true))
                    playerChoice = 1;
                else if (Singleton<InputManager>.Instance.GetDigitalInput("Item3", true))
                    playerChoice = 2;

                if (Singleton<InputManager>.Instance.GetDigitalInput("Item1", true) || Singleton<InputManager>.Instance.GetDigitalInput("Item2", true)
                    || Singleton<InputManager>.Instance.GetDigitalInput("Item3", true))
                    ResultRPS();
            }
        }

        public void Destroy()
        {
            player.plm.am.moveMods.Remove(moveMod);
            Singleton<CoreGameManager>.Instance.GetCamera(player.playerNumber).UpdateTargets(null, 24);
            Destroy(textScaler);
            Destroy(textCanvas);
            Destroy(gameObject);
        }

        private void ResultRPS()
        {
            pickedChoice = true;
            StartCoroutine(CheckDelay(0.6f));
            instructionsTmp.text = playerChoiceText[playerChoice] + " VS " + opponentChoiceText[opponentChoice];
        }

        private void checkRPS()
        {
            pickedChoice = false;
            publicActive = false;
            //if its a tie
            if ((playerChoice == 0 & opponentChoice == 0) || (playerChoice == 1 & opponentChoice == 1) || (playerChoice == 2 & opponentChoice == 2))
            {
                instructionsTmp.text = "Press " + Singleton<InputManager>.Instance.GetInputButtonName("Item1") + " For Rock \nPress " + Singleton<InputManager>.Instance.GetInputButtonName("Item2") + " for Paper \nPress " + Singleton<InputManager>.Instance.GetInputButtonName("Item3") + " for Scissors";
            }
            //if the player wins
            if ((playerChoice == 0 & opponentChoice == 2) || (playerChoice == 2 & opponentChoice == 1) || (playerChoice == 1 & opponentChoice == 0))
            {
                Singleton<CoreGameManager>.Instance.AddPoints(35, player.playerNumber, true);
                rps.EndRPS(true);
            }
            //if the player loses
            if ((playerChoice == 0 & opponentChoice == 1) || (playerChoice == 1 & opponentChoice == 2) || (playerChoice == 2 & opponentChoice == 0))
            {
                rps.Losah();
                rps.EndRPS(false);
            }
        }

        public void death()
        {
            rps.fuckingDies();
        }


        private IEnumerator CheckDelay(float val)
        {
            checkDelay = val;
            while (checkDelay > 0f)
            {
                checkDelay -= Time.deltaTime * ec.EnvironmentTimeScale;
                yield return null;
            }
            checkDelay = 0f;
            checkRPS();
            yield break;
        }

        public bool publicActive = false;
        private bool pickedChoice = false;
        private int playerChoice;
        private int opponentChoice;

        private string[] opponentChoiceText = new string[]
        {
        "Rock",
        "Paper",
        "Scissors"
        };
        private string[] playerChoiceText = new string[]
        {
        "Rock",
        "Paper",
        "Scissors"
        };

        private float checkDelay;

        private MovementModifier moveMod = new MovementModifier(default(Vector3), 0f);

        private Canvas textCanvas;
        private CanvasScaler textScaler;
        private TMP_Text instructionsTmp;
        public RPSGuy rps;
        public EnvironmentController ec;
        public PlayerManager player;
    }
}
