using BCarnellChars.Characters;
using MTM101BaldAPI;
using MTM101BaldAPI.Reflection;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BCarnellChars.OtherStuff
{
    public class SiegeCartBalls : MonoBehaviour, IEntityTrigger
    {
        private Entity entity;
        private SpriteRenderer spriteRenderer;
        public EnvironmentController ec;

        private float speed = 80;
        private float time = 5;

        private MovementModifier moveMod;
        private List<ActivityModifier> activityMods = new List<ActivityModifier>();

        private void Start()
        {
            moveMod = new MovementModifier(Vector3.zero,0f, 11);
            spriteRenderer = transform.Find("RendereBase").GetComponentInChildren<SpriteRenderer>();
            entity = gameObject.GetComponent<Entity>();
        }

        private void Update()
        {
            if (ec == null)
                return;
            moveMod.movementAddend = entity.ExternalActivity.Addend + transform.forward * speed * ec.EnvironmentTimeScale;
            entity.UpdateInternalMovement(transform.forward * speed * ec.EnvironmentTimeScale);
            time -= Time.deltaTime * ec.EnvironmentTimeScale;
            if (!(time <= 0f))
                return;
            foreach (ActivityModifier activityMod in activityMods)
                activityMod.moveMods.Remove(moveMod);
            Destroy(gameObject);
        }

        public void EntityTriggerEnter(Collider other)
        {
            if (moveMod == null) return;
            // The original concept was every collision would result the entity being squished, but due to physics, that is very out of context.
            Entity component = other.GetComponent<Entity>();
            if (other.CompareTag("Player") || (other.CompareTag("NPC") && other.GetComponent<NPC>().Character != EnumExtensions.GetFromExtendedName<Character>("SiegeCanonCart")) && component != null)
            {
                component.ExternalActivity.moveMods.Add(moveMod);
                activityMods.Add(component.ExternalActivity);
            }
        }

        public void EntityTriggerStay(Collider other)
        {
        }

        public void EntityTriggerExit(Collider other)
        {
            if (moveMod == null) return;
            Entity component = other.GetComponent<Entity>();
            if (other.CompareTag("Player") || (other.CompareTag("NPC") && other.GetComponent<NPC>().Character != EnumExtensions.GetFromExtendedName<Character>("SiegeCanonCart")) && component != null)
            {
                component.ExternalActivity.moveMods.Remove(moveMod);
                activityMods.Remove(component.ExternalActivity);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Window"))
            {
                Window component = other.gameObject.GetComponent<Window>();
                if (component != null && !(bool)component.ReflectionGetVariable("broken"))
                    component.Break(false);
            }
        }

        private void OnCollisionExit(Collider other)
        {
            if (other.CompareTag("Default"))
            {
                foreach (ActivityModifier activityMod in activityMods)
                    activityMod.moveMods.Remove(moveMod);
                Destroy(gameObject);
            } 
        }
    }
}
