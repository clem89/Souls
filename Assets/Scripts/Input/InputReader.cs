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

    InputSystem_Actions _actions;

    void OnEnable()
    {
        if (_actions == null) _actions = new InputSystem_Actions();

        _actions.Player.Move.performed += ctx => MoveEvent?.Invoke(ctx.ReadValue<Vector2>());
        _actions.Player.Move.canceled += _ => MoveEvent?.Invoke(Vector2.zero);
        _actions.Player.Look.performed += ctx => LookEvent?.Invoke(ctx.ReadValue<Vector2>());
        _actions.Player.Look.canceled += _ => LookEvent?.Invoke(Vector2.zero);
        _actions.Player.Attack.started += _ => AttackStarted?.Invoke();
        _actions.Player.Jump.performed += _ => DodgePerformed?.Invoke();
        _actions.Player.Crouch.performed += _ => ParryPerformed?.Invoke();
        _actions.Player.Next.performed += _ => LockOnPerformed?.Invoke();

        _actions.Enable();
    }

    void OnDisable() => _actions?.Disable();
}
