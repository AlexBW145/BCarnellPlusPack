using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using BCarnellChars.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BCarnellChars.OtherStuff
{
    public class RockPaperScissors : MonoBehaviour
    {
        private void Awake()
        {
            // Apparently, this is too errorish with GetComponentInChildren<T>(), meaning that it'll fail to find.
            textCanvas = transform.Find("TextCanvas").GetComponent<Canvas>();
            textScaler = transform.Find("TextCanvas").GetComponent<CanvasScaler>();
            instructionsTmp = textCanvas.transform.Find("Instructions").GetComponent<TMP_Text>();
            cover = textCanvas.transform.Find("Border").GetComponent<Image>();
            you = textCanvas.transform.Find("Player").GetComponent<RawImage>();
            opponent = textCanvas.transform.Find("Opponent").GetComponent<RawImage>();
            youChose = textCanvas.transform.Find("Player_Chose").GetComponent<Image>();
            opponentChose = textCanvas.transform.Find("Opponent_Chose").GetComponent<Image>();
        }
        private void Start()
        {
            instructionsTmp.color = Color.white;
            if (BaseGameManager.Instance == null)
                return;
            ec = BaseGameManager.Instance.Ec;
            publicActive = true;
            transform.position = player.cameraBase.position;
            player.plm.am.moveMods.Add(moveMod);
            CoreGameManager.Instance.GetCamera(player.playerNumber).UpdateTargets(transform, 24);
            textCanvas.worldCamera = CoreGameManager.Instance.GetCamera(player.playerNumber).canvasCam;
            textCanvas.transform.SetParent(null);
            textCanvas.transform.position = Vector3.zero;
            if (!PlayerFileManager.Instance.authenticMode)
            {
                // Imagine using the mod menu for this...
                textScaler.scaleFactor = Mathf.RoundToInt(PlayerFileManager.Instance.resolutionY / 360f);
            }
            instructionsTmp.text = string.Format(LocalizationManager.Instance.GetLocalizedText("Hud_RPSGuy_Instructions"), InputManager.Instance.GetInputButtonName("Interact"), InputManager.Instance.GetInputButtonName("Run"), InputManager.Instance.GetInputButtonName("LookBack"));
            instructionsTmp.gameObject.SetActive(false);
            cover.gameObject.SetActive(false);
            you.gameObject.SetActive(false);
            opponent.gameObject.SetActive(false);
            StartCoroutine(Fade(textCanvas.GetComponent<RawImage>(), true, transitionTime, () =>
            {
                instructionsTmp.gameObject.SetActive(true);
                cover.gameObject.SetActive(true);
                you.gameObject.SetActive(true);
                opponent.gameObject.SetActive(true);
            }));
        }

        // Token: 0x060000A3 RID: 163 RVA: 0x000068EC File Offset: 0x00004AEC
        private void Update()
        {
            if (BaseGameManager.Instance == null)
                return;
            transform.position = player.transform.position;
            transform.rotation = player.transform.rotation;
            if (CoreGameManager.Instance.Paused || checkDelay > 0 || !instructionsTmp.gameObject.activeSelf)
                return;
            if (!pickedChoice)
            {
                opponentChoice = Mathf.RoundToInt(UnityEngine.Random.Range(0f, 2f)); //Pick a random type between 0 and 2

                // Cannot use switch statements over this
                if (Singleton<InputManager>.Instance.GetDigitalInput("Interact", true))
                    playerChoice = 0;
                else if (Singleton<InputManager>.Instance.GetDigitalInput("Run", true))
                    playerChoice = 1;
                else if (Singleton<InputManager>.Instance.GetDigitalInput("LookBack", true))
                    playerChoice = 2;

                if (Singleton<InputManager>.Instance.GetDigitalInput("Interact", true) || Singleton<InputManager>.Instance.GetDigitalInput("Run", true)
                    || Singleton<InputManager>.Instance.GetDigitalInput("LookBack", true))
                    ResultRPS();
            }
        }

        public void Destroy()
        {
            player.plm.am.moveMods.Remove(moveMod);
            Singleton<CoreGameManager>.Instance.GetCamera(player.playerNumber).UpdateTargets(null, 24);
            Destroy(textCanvas.gameObject);
            Destroy(gameObject);
        }

        private void ResultRPS()
        {
            pickedChoice = true;
            StartCoroutine(CheckDelay(0.6f));
            youChose.sprite = chosenSprites[playerChoice];
            opponentChose.sprite = chosenSprites[opponentChoice];
            instructionsTmp.text = LocalizationManager.Instance.GetLocalizedText("Hud_RPSGuy_Result" + playerChoice) + LocalizationManager.Instance.GetLocalizedText("Hud_RPSGuy_versus") + LocalizationManager.Instance.GetLocalizedText("Hud_RPSGuy_Result" + opponentChoice);
            if (playerChoice == 0 & opponentChoice == 0 || playerChoice == 1 & opponentChoice == 1 || playerChoice == 2 & opponentChoice == 2)
                CoreGameManager.Instance.audMan.PlaySingle(hitTie);
            else if (playerChoice == 0 & opponentChoice == 2 || playerChoice == 2 & opponentChoice == 1 || playerChoice == 1 & opponentChoice == 0)
                CoreGameManager.Instance.audMan.PlaySingle(hitWin);
            else if (playerChoice == 0 & opponentChoice == 1 || playerChoice == 1 & opponentChoice == 2 || playerChoice == 2 & opponentChoice == 0)
                CoreGameManager.Instance.audMan.PlaySingle(hitLose);
        }

        private void checkRPS()
        {
            pickedChoice = false;
            publicActive = false;
            //if its a tie
            if (playerChoice == 0 & opponentChoice == 0 || playerChoice == 1 & opponentChoice == 1 || playerChoice == 2 & opponentChoice == 2) {
                instructionsTmp.text = string.Format(LocalizationManager.Instance.GetLocalizedText("Hud_RPSGuy_Instructions"), InputManager.Instance.GetInputButtonName("Interact"), InputManager.Instance.GetInputButtonName("Run"), InputManager.Instance.GetInputButtonName("LookBack"));
                youChose.sprite = Resources.FindObjectsOfTypeAll<Sprite>().ToList().Find(x => x.name == "Transparent");
                opponentChose.sprite = Resources.FindObjectsOfTypeAll<Sprite>().ToList().Find(x => x.name == "Transparent");
            }
            //if the player wins
            else if (playerChoice == 0 & opponentChoice == 2 || playerChoice == 2 & opponentChoice == 1 || playerChoice == 1 & opponentChoice == 0)
            {
                instructionsTmp.gameObject.SetActive(false);
                cover.gameObject.SetActive(false);
                you.gameObject.SetActive(false);
                opponent.gameObject.SetActive(false);
                youChose.gameObject.SetActive(false);
                opponentChose.gameObject.SetActive(false);
                rps.EndRPS(true);
                StartCoroutine(Fade(textCanvas.GetComponent<RawImage>(), false, transitionTime, () =>
                {
                    Destroy();
                }));
            }
            //if the player loses
            else if (playerChoice == 0 & opponentChoice == 1 || playerChoice == 1 & opponentChoice == 2 || playerChoice == 2 & opponentChoice == 0)
            {
                instructionsTmp.gameObject.SetActive(false);
                cover.gameObject.SetActive(false);
                you.gameObject.SetActive(false);
                opponent.gameObject.SetActive(false);
                youChose.gameObject.SetActive(false);
                opponentChose.gameObject.SetActive(false);
                rps.Losah();
                rps.EndRPS(false);
                StartCoroutine(Fade(textCanvas.GetComponent<RawImage>(), false, transitionTime, () =>
                {
                    Destroy();
                }));
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

        private IEnumerator Fade(RawImage thing, bool isIn, float val = 1f, Action act = null)
        {
            float timer = isIn ? 0f : 0.50f;
            while (isIn ? timer < 0.50f : timer > 0f)
            {
                if (isIn)
                    timer += val * (Time.deltaTime * ec.EnvironmentTimeScale);
                else
                    timer -= val * (Time.deltaTime * ec.EnvironmentTimeScale);
                thing.color = new Color(thing.color.r, thing.color.g, thing.color.b, timer);
                yield return null;
            }
            if (act != null)
                act.Invoke();
            yield break;
        }

        private readonly float transitionTime = 1.1666667f;
        public bool publicActive = false;
        private bool pickedChoice = false;
        private int playerChoice;
        private int opponentChoice;

        private float checkDelay;

        private MovementModifier moveMod = new MovementModifier(default, 0f);

        private Canvas textCanvas;
        private CanvasScaler textScaler;
        private TMP_Text instructionsTmp;
        private Image cover;
        private RawImage you;
        private RawImage opponent;
        private Image youChose;
        private Image opponentChose;
        public RPSGuy rps;
        public EnvironmentController ec;
        public PlayerManager player;

        public SoundObject hitWin;
        public SoundObject hitLose;
        public SoundObject hitTie;
        public List<Sprite> chosenSprites = new List<Sprite>();
    }
}
