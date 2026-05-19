using BCarnellChars.Characters;
using HarmonyLib;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace BCarnellChars.ItemStuff;

public class ITM_AnyportalOutput : Item
{
    [SerializeField] internal SoundObject wrongPlacement, outputGeneration, destroySnd;
    [SerializeField] internal AudioManager audMan;

    [SerializeField] internal Material mask;

    private bool readyToUse = false;
    public bool Ready => readyToUse;
    public PlayerManager Owner => pm;
    [SerializeField] internal Sticker anyportalSticker;

    public override bool Use(PlayerManager pm)
    {
        if ((pm.ec.Npcs.Find(x => x is MrPortalMan) || StickerManager.Instance.HasSticker(anyportalSticker))
            && pm.ec.CellFromPosition(pm.transform.position).HardCoverageFits(CellCoverage.Down)
            && !pm.ec.CellFromPosition(pm.transform.position).HasObjectBase)
        {
            base.pm = pm;
            audMan.QueueAudio(outputGeneration, true);
            StartCoroutine(playAnim(base.pm.ec.CellFromPosition(base.pm.transform.position)));
            return true;
        }

        CoreGameManager.Instance.audMan.PlaySingle(wrongPlacement);
        Destroy(gameObject);
        return false;
    }

    private IEnumerator playAnim(Cell tile)
    {
        float time = 6.21f;
        var quad = transform.Find("Quad").gameObject;
        transform.position = tile.CenterWorldPosition;
        transform.rotation = Directions.RandomDirection.ToRotation();
        tile.HardCover(CellCoverage.Down);
        ParticleSystem particle = gameObject.GetComponentInChildren<ParticleSystem>();
        var main = particle.main;
        quad.transform.localScale = new Vector3(0f, 0f, 0f);
        particle.Play();
        while (time > 0f)
        {
            if (pm.ec.EnvironmentTimeScale == 0f && audMan.QueuedAudioIsPlaying)
                audMan.Pause(true);
            else if (pm.ec.EnvironmentTimeScale != 0f && !audMan.QueuedAudioIsPlaying)
                audMan.Pause(false);
            time -= Time.deltaTime * pm.ec.EnvironmentTimeScale;
            main.simulationSpeed += 0.5f * (Time.deltaTime * pm.ec.EnvironmentTimeScale);
            if (quad.transform.localScale.x < 10f)
                quad.transform.localScale += new Vector3(1.5f, 1.5f, 1.5f) * (Time.deltaTime * pm.ec.EnvironmentTimeScale);
            yield return null;
        }
        quad.transform.localScale = new Vector3(10f, 10f, 10f);
        readyToUse = true;
        particle.Stop();
        MaterialModifier.ChangeHole(quad.GetComponent<MeshRenderer>(), mask, quad.GetComponent<MeshRenderer>().materials[1]);
        MaterialModifier.SetBase(quad.GetComponent<MeshRenderer>(), BCPPPlugin.bcppAssets.Get<RenderTexture>("MrPortalMan/ManCamRender"));
    }

    private static FieldInfo
        _hardCoverage = AccessTools.DeclaredField(typeof(Cell), "hardCoverage"),
        _softCoverage = AccessTools.DeclaredField(typeof(Cell), "softCoverage");
    public void Break(bool instant = false)
    {
        if (readyToUse || instant)
        {
            CoreGameManager.Instance.audMan.PlaySingle(destroySnd);
            var cell = pm.ec.CellFromPosition(gameObject.transform.position);
            CellCoverage hard = (CellCoverage)_hardCoverage.GetValue(cell);
            CellCoverage soft = (CellCoverage)_softCoverage.GetValue(cell);
            hard &= ~CellCoverage.Down;
            soft &= ~CellCoverage.Down;
            _hardCoverage.SetValue(cell, hard);
            _softCoverage.SetValue(cell, soft);
            Destroy(gameObject);
        }
    }
}
