using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BCarnellChars.OtherStuff;

internal class DogcheckBCPP : MonoBehaviour
{
    internal static AudioClip music;
    internal static void SendToDogcheckScreen()
    {
        Time.timeScale = 0f;
        GlobalCam.Instance.Transition(UiTransition.Dither, 0.04f);
        if (ElevatorScreen.Instance != null)
            Destroy(ElevatorScreen.Instance.gameObject);
        Scene dogScene = SceneManager.CreateScene("Dogcheck");
        SceneManager.SetActiveScene(dogScene);
        SceneManager.UnloadSceneAsync("Game", UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
        var dogobj = new GameObject("Dogscene").AddComponent<DogcheckBCPP>();
        dogobj.Initialize();
        Time.timeScale = 0f;
        CoreGameManager.Instance.GetHud(0).Hide(true);
    }

    private void Initialize()
    {
        MusicManager.Instance.StopMidi();
        var canvas = CreateCanvas("DogcheckCanvas", 10, new Vector2(480, 360), 100f, true);
        canvas.transform.SetParent(transform);
        canvas.gameObject.AddComponent<RawImage>().color = new Color(0.1f, 0.1f, 0.1f);
        canvas.gameObject.GetComponent<RawImage>().texture = BCPPPlugin.bcppAssets.Get<Texture2D>("Boss/BlueBrickWall");
        var bg = new GameObject("ImgBG", typeof(RectTransform)).GetComponent<RectTransform>();
        bg.gameObject.layer = LayerMask.NameToLayer("UI");
        bg.transform.SetParent(canvas.transform);
        gameObject.SetActive(false);
        gameObject.AddComponent<BaldiBG>().faceAnchor = bg;
        gameObject.SetActive(true);
        Sprite beans = Resources.FindObjectsOfTypeAll<Sprite>().Last(x => x.name == "BaldiChalkSheet_0");
        var baldibg = Resources.FindObjectsOfTypeAll<CoreGameManager>().Last(x => x.name == "CoreGameManager").transform.Find("PauseMenuScreens").Find("PauseScreen").Find("BaldiBG");
        for (int i = 0; i < 24; i++)
        {
            Image gridItem = Instantiate(baldibg.GetComponentsInChildren<Image>()[i], bg.transform);
            gridItem.color = new Color(0f, 0.3935943f, 1f);
            gridItem.sprite = beans;
        }
        bg.localScale = Vector3.one;
        Instantiate(GlobalCam.Instance.transform.GetChild(0).Find("Bottom"), canvas.transform).gameObject.SetActive(true);
        TMP_Text text = Instantiate(Resources.FindObjectsOfTypeAll<CoreGameManager>().Last(x => x.name == "CoreGameManager").transform.Find("PauseMenuScreens").Find("PauseScreen").Find("Main").GetComponentsInChildren<TMP_Text>()[2], canvas.transform);
        Destroy(text.gameObject.GetComponent<TextLocalizer>());
        text.richText = true;
        text.fontSize = 30f;
        text.color = Color.blue;
        text.text = "Are you forgetting\nsomething?";
        text.rectTransform.anchorMin.Set(0.5f, 0.5f);
        text.rectTransform.anchorMax.Set(0.5f, 0.5f);
        text.rectTransform.pivot.Set(0.5f, 0.5f);
        text.rectTransform.anchoredPosition = Vector2.zero;
        var aud = gameObject.AddComponent<AudioSource>();
        aud.loop = true;
        aud.playOnAwake = false;
        aud.bypassEffects = true;
        aud.bypassListenerEffects = true;
        aud.bypassReverbZones = true;
        aud.dopplerLevel = 0f;
        aud.spatialBlend = 0f;
        aud.ignoreListenerPause = true;
        aud.outputAudioMixerGroup = Resources.FindObjectsOfTypeAll<AudioMixerGroup>().Last(x => x.name == "Wav");
        aud.clip = music;
        aud.Play();
    }

    private void Update()
    {
        if (Input.anyKeyDown)
            CoreGameManager.Instance.Quit();
    }

    private static Canvas CreateCanvas(string name, int sortOrder, Vector2 resolution, float refPixels = 100f, bool includePlaneDistance = true)
    {
        // Cursor be small, because of the reference resolution being in default.
        Canvas canvas = new GameObject(name, [typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)]).GetComponent<Canvas>();
        canvas.gameObject.layer = LayerMask.NameToLayer("UI");
        canvas.sortingOrder = sortOrder;
        canvas.pixelPerfect = false;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 & AdditionalCanvasShaderChannels.Normal & AdditionalCanvasShaderChannels.Tangent;
        canvas.worldCamera = GlobalCam.Instance.Cam;
        CanvasScaler scaler = canvas.gameObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = resolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        scaler.scaleFactor = refPixels;
        scaler.referencePixelsPerUnit = 16f;
        GraphicRaycaster graphicRaycaster = canvas.gameObject.GetComponent<GraphicRaycaster>();
        graphicRaycaster.ignoreReversedGraphics = true;
        graphicRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        graphicRaycaster.blockingMask = ~0;
        if (includePlaneDistance)
            canvas.gameObject.AddComponent<PlaneDistance>();
        return canvas;
    }
}
