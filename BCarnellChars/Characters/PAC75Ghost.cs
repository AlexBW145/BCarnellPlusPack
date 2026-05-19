using BCarnellChars.Characters.States;
using HarmonyLib;
using MTM101BaldAPI;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BCarnellChars.Characters;

public class PAC75Ghost : Baldi
{
    public new float Speed;
    private float increaseSpeed = 0.1f;
    public float NewSpeed => Speed + increaseSpeed;
    [SerializeField] internal AudioManager audman;
    public new AudioManager AudMan => audman;
    public Vector3 home { get; internal set; }
    private static readonly FieldInfo _subtitleColor = AccessTools.DeclaredField(typeof(AudioManager), "subtitleColor");

    public override float DistanceCheck(float val) => val;

    public static Sprite craftersSprite { private get; set; }

    void Awake()
    {
        loseSounds = Resources.FindObjectsOfTypeAll<Baldi>().Last().loseSounds;
        spriteRenderer[0].sprite = craftersSprite;
    }

    void Start()
    {
        if (ec != null && audman != null)
            BaseGameManager.Instance.Ec.map.AddArrow(navigator.Entity, (Color)_subtitleColor.GetValue(audman));
    }

    public override void Slap() { }

    public override void Initialize()
    {
        loseSounds = Resources.FindObjectsOfTypeAll<Baldi>().Last().loseSounds;
        navigator.Initialize(ec);
        home = transform.position;
        spriteBase.AddComponent<PickupBob>();
        navigator.maxSpeed = Speed;
        navigator.SetSpeed(Speed);
        PAC75Ghost_StateBase state = new PAC75Ghost_StateBase(this);
        switch (Character.ToStringExtended<Character>())
        {
            case "Blinky":
                state = new PAC75Ghost_RedGuy(this);
                break;
            case "Inky":
                state = new PAC75Ghost_CyanIdiot(this);
                break;
            case "Cylde":
                state = new PAC75Ghost_OrangeCurious(this);
                break;
            case "Pinky":
                state = new PAC75Ghost_PinkGal(this);
                break;
        }
        behaviorStateMachine.ChangeState(state);
    }

    public override void SetAnger(float value)
    {
        increaseSpeed = value;
        if (behaviorStateMachine.CurrentState is PAC75Ghost_Fleeing) return;
        navigator.SetSpeed(Speed + increaseSpeed);
        navigator.maxSpeed = Speed + increaseSpeed;
    }

    public override void GetAngry(float value)
    {
        increaseSpeed += value;
        if (increaseSpeed <= 0.1f)
            increaseSpeed = 0.1f;
        if (behaviorStateMachine.CurrentState is PAC75Ghost_Fleeing) return;
        navigator.SetSpeed(Speed + increaseSpeed);
        navigator.maxSpeed = Speed + increaseSpeed;
    }
}