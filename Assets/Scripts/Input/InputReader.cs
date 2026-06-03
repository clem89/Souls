using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Souls/InputReader")]
public class InputReader : ScriptableObject
{
    public event Action<Vector2> MoveEvent;
    public event Action<Vector2> LookEvent;
    public event Action AttackStarted;
    public event Action DodgePerformed;
    public event Action ParryPerformed;
    public event Action LockOnPerformed;

    public bool AttackHeld { get; private set; }

    InputSystem_Actions _actions;

    void OnEnable()
    {
        _actions = new InputSystem_Actions();
        _actions.Player.Move.performed += ctx => MoveEvent?.Invoke(ctx.ReadValue<Vector2>());
        _actions.Player.Move.canceled += _ => MoveEvent?.Invoke(Vector2.zero);
        _actions.Player.Look.performed += ctx => LookEvent?.Invoke(ctx.ReadValue<Vector2>());
        _actions.Player.Look.canceled += _ => LookEvent?.Invoke(Vector2.zero);
        _actions.Player.Attack.started  += _ => { AttackHeld = true; AttackStarted?.Invoke(); };
        _actions.Player.Attack.canceled += _ => AttackHeld = false;
        _actions.Player.Jump.performed += _ => DodgePerformed?.Invoke();    // Task 3에서 Dodge 바인딩으로 교체
        _actions.Player.Crouch.performed += _ => ParryPerformed?.Invoke();  // Task 3에서 RMB 바인딩으로 교체
        _actions.Player.Next.performed += _ => LockOnPerformed?.Invoke();   // Task 3에서 Middle Mouse 바인딩으로 교체
        _actions.Enable();
    }

    void OnDisable() => _actions?.Disable();
}
