using UnityEngine;

namespace BCarnellTeacher;

public class ITM_CarnellDestabilizer : Item, IEntityTrigger
{
    [SerializeField] internal Entity entity;
    [SerializeField] internal AudioManager audMan;
    private EnvironmentController ec;
    [SerializeField] internal SoundObject blastSnd, boomSnd;
    public override bool Use(PlayerManager pm)
    {
        ec = pm.ec;
        transform.position = pm.transform.position;
        transform.forward = CoreGameManager.Instance.GetCamera(pm.playerNumber).transform.forward;
        entity.Initialize(ec, transform.position);
        entity.CopyStatusEffects(pm.plm.Entity);
        entity.OnEntityMoveInitialCollision += OnHit;
        return true;
    }

    public override void PostUse(PlayerManager pm)
    {
        audMan.PlaySingle(blastSnd);
    }

    void Update()
    {
        entity.UpdateInternalMovement(transform.forward * 100f * ec.EnvironmentTimeScale);
    }

    private void OnHit(RaycastHit hit)
    {
        Destroy();
    }

    private void Destabilize(BCarnell carnell)
    {
        if (carnell.IsHelping())
        {
            carnell.Despawn();
            return;
        }
        carnell.behaviorStateMachine.ChangeState(new BCarnell_Destabilized(carnell));
    }

    private void Destroy()
    {
        CoreGameManager.Instance.audMan.PlaySingle(boomSnd);
        Destroy(gameObject);
    }

    public void EntityTriggerEnter(Entity otherEntity, Collider other, bool validCollision)
    {
        if (validCollision && other.CompareTag("NPC"))
        {
            if (otherEntity.GetComponent<NPC>().behaviorStateMachine.CurrentState is BCarnell_Tutor or BCarnell_GettingAngry) return;
            if (otherEntity.gameObject.GetComponent<BCarnell>() != null)
                Destabilize(otherEntity.gameObject.GetComponent<BCarnell>());
            Destroy();
        }
    }

    public void EntityTriggerStay(Entity otherEntity, Collider other, bool validCollision)
    {
    }

    public void EntityTriggerExit(Entity otherEntity, Collider other, bool validCollision)
    {
    }
}
