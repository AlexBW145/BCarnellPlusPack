using BCarnellChars.Characters;
using HarmonyLib;
using MTM101BaldAPI;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BCarnellChars.OtherStuff;

public class SiegeCartBalls : MonoBehaviour, IEntityTrigger
{
    [SerializeField] internal Entity entity;
    public EnvironmentController ec;

    private float speed = 80, time = 5;

    private MovementModifier moveMod = new MovementModifier(Vector3.zero, 0f, 11);
    private List<ActivityModifier> activityMods = new List<ActivityModifier>();

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

    public void EntityTriggerEnter(Entity entity, Collider other, bool isValid)
    {
        if (moveMod == null || !isValid) return;
        // The original concept was every collision would result the entity being squished, but due to physics, that is very out of context.
        if ((other.CompareTag("Player") || (other.CompareTag("NPC") && other.GetComponent<NPC>().Character.ToStringExtended() != "SiegeCanonCart")) && entity != null)
        {
            entity.ExternalActivity.moveMods.Add(moveMod);
            activityMods.Add(entity.ExternalActivity);
        }
    }

    public void EntityTriggerStay(Entity entity, Collider other, bool isValid)
    {
        if (moveMod == null || !entity.Squished) return;
        if ((other.CompareTag("Player") || (other.CompareTag("NPC") && other.GetComponent<NPC>().Character.ToStringExtended() != "SiegeCanonCart")) && entity != null)
        {
            entity.ExternalActivity.moveMods.Remove(moveMod);
            activityMods.Remove(entity.ExternalActivity);
        }
    }

    public void EntityTriggerExit(Entity entity, Collider other, bool isValid)
    {
        if (moveMod == null || !isValid) return;
        if ((other.CompareTag("Player") || (other.CompareTag("NPC") && other.GetComponent<NPC>().Character.ToStringExtended() != "SiegeCanonCart")) && entity != null)
        {
            entity.ExternalActivity.moveMods.Remove(moveMod);
            activityMods.Remove(entity.ExternalActivity);
        }
    }

    private static readonly FieldInfo _broken = AccessTools.DeclaredField(typeof(Window), "broken");
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Window") && !entity.Squished)
        {
            Window component = other.gameObject.GetComponent<Window>();
            if (component != null && !(bool)_broken.GetValue(component))
                component.Break(false);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Wall") && other.enabled && other.gameObject.activeSelf && other.transform.parent.gameObject.activeSelf)
            time -= 1.5f * (Time.deltaTime * ec.NpcTimeScale);
    }
}
#if DEBUG
public class Sawblades : MonoBehaviour, IEntityTrigger
{
    [SerializeField] internal Entity entity;
    [SerializeField] internal SpriteRenderer spriteRenderer;
    public EnvironmentController ec;

    private float speed = 90;
    internal float time = 90;

    internal void Initialize(BCarnellBoss boss)
    {
        entity.Initialize(boss.ec, transform.position);
        entity.CopyStatusEffects(boss.Navigator.Entity);
        ec = boss.ec;
    }

    private void Update()
    {
        if (ec == null)
            return;
        entity.UpdateInternalMovement(transform.forward * speed * ec.EnvironmentTimeScale);
        time -= Time.deltaTime * ec.NpcTimeScale;
        if (!(time <= 0f))
            return;
        Destroy(gameObject);
    }

    public void EntityTriggerEnter(Entity entity, Collider other, bool isValid)
    {
        Entity component = other.GetComponent<Entity>();
        if (other.CompareTag("Player") && component != null && isValid)
            BaseGameManager.Instance.EndGame(other.transform, ec.Npcs.Find(x => x.GetComponent<Baldi>()).GetComponent<Baldi>());
    }

    public void EntityTriggerStay(Entity entity, Collider other, bool isValid)
    {
    }

    public void EntityTriggerExit(Entity entity, Collider other, bool isValid)
    {
    }

    private static readonly FieldInfo _broken = AccessTools.DeclaredField(typeof(Window), "broken");
    private void OnTriggerEnter(Entity entity, Collider other)
    {
        if (other.gameObject.CompareTag("Window"))
        {
            Window component = other.gameObject.GetComponent<Window>();
            if (component != null && !(bool)_broken.GetValue(component))
                component.Break(false);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Wall") && other.enabled && other.gameObject.activeSelf && other.transform.parent.gameObject.activeSelf)
            time -= 15f * (Time.deltaTime * ec.NpcTimeScale);
    }
}
#endif