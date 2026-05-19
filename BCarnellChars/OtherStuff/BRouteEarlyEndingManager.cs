using MTM101BaldAPI.SaveSystem;
using MTM101BaldAPI.UI;
using TMPro;
using UnityEngine;

namespace BCarnellChars.OtherStuff;

internal class BRouteEarlyEndingManager : BaseGameManager
{
    private MovementModifier moveMod = new MovementModifier(default, 0f);

    public override void BeginPlay()
    {
        Time.timeScale = 1f;
        CoreGameManager.Instance.disablePause = false;
        InputManager.Instance.ActivateActionSet("InGame");
        AudioListener.pause = false;
        PropagatedAudioManager.paused = false;
        MusicManager.Instance.StopMidi();
        for (var player = 0; player < CoreGameManager.Instance.setPlayers; player++)
        {
            CoreGameManager.Instance.GetPlayer(player).Am.moveMods.Add(moveMod);
            CoreGameManager.Instance.GetPlayer(player).itm.Disable(true);
        }
        var saveExists = false;
        ModdedFileManager.Instance.saveData.saveAvailable = false;
        PlayerFileManager.Instance.savedGameData.saveAvailable = saveExists;
        PlayerFileManager.Instance.Save();
    }

    public override void Initialize()
    {
        if (CoreGameManager.Instance.currentMode == Mode.Free)
        {
            Destroy(ElevatorScreen.Instance.gameObject);
            CoreGameManager.Instance.Quit();
            Destroy(gameObject);
        }
        else
        {
            CoreGameManager.Instance.SpawnPlayers(ec);
            CoreGameManager.Instance.SaveEnabled = false;
            CoreGameManager.Instance.readyToStart = true;
            var inform = new GameObject("message from the developer", typeof(MeshRenderer), typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            inform.transform.position = ec.spawnPoint + (Vector3.forward * 15f);
            inform.alignment = TextAlignmentOptions.Center;
            var font = BaldiFonts.ComicSans12.FontAsset();
            inform.font = font;
            inform.fontSize = 12f;
            inform.text = "There was going to be a true challenge that will invite our special guest.\nToday is not that day...";
            inform.color = Color.white;
        }
    }
}