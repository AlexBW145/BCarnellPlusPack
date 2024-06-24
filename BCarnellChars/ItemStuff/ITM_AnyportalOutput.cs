using BCarnellChars.Characters;
using BCarnellChars.GeneratorStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace BCarnellChars.ItemStuff
{
    public class ITM_AnyportalOutput : Item
    {
        public SoundObject wrongPlacement;
        public SoundObject outputGeneration;
        public SoundObject destroySnd;
        public AudioManager audMan;

        public Material mask;

        private bool readyToUse = false;
        public bool Ready => readyToUse;

        public override bool Use(PlayerManager pm)
        {
            if (pm.ec.Npcs.Find(x => x.gameObject.GetComponent<MrPortalMan>())
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
            MaterialModifier.SetBase(quad.GetComponent<MeshRenderer>(), BasePlugin.bcppAssets.Get<RenderTexture>("MrPortalMan/ManCamRender"));
        }

        public void Break(bool instant = false)
        {
            if (readyToUse || instant)
            {
                CoreGameManager.Instance.audMan.PlaySingle(destroySnd);
                pm.ec.CellFromPosition(gameObject.transform.position).HardCover(~CellCoverage.Down);
                Destroy(gameObject);
            }
        }
    }
}
