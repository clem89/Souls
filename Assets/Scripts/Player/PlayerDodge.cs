using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(StaminaController))]
public class PlayerDodge : MonoBehaviour
{
    [SerializeField] InputReader _input;
    [SerializeField] float _dodgeDistance = 4f;
    [SerializeField] float _dodgeDuration = 0.3f;
    [SerializeField] float _iFrameDuration = 0.2f;
    [SerializeField] float _staminaCost = 25f;
    [SerializeField] float _cooldown = 0.5f;

    CharacterController _cc;
    StaminaController _stamina;
    PlayerController _controller;

    public bool IsDodging { get; private set; }
    public bool IsInvincible { get; private set; }

    bool _onCooldown;

    void Awake()
    {
        Debug.Assert(_input != null, "PlayerDodge: InputReader not assigned");
        _cc = GetComponent<CharacterController>();
        _stamina = GetComponent<StaminaController>();
        _controller = GetComponent<PlayerController>();
        _input.DodgePerformed += OnDodgeInput;
    }

    void OnDestroy() => _input.DodgePerformed -= OnDodgeInput;

    void OnDodgeInput()
    {
        if (IsDodging || _onCooldown) return;
        if (!_stamina.TryConsume(_staminaCost)) return;

        var dir = (_controller != null && _controller.MoveDirection.sqrMagnitude > 0.01f)
            ? _controller.MoveDirection
            : transform.forward;

        StartCoroutine(DodgeCoroutine(dir));
    }

    IEnumerator DodgeCoroutine(Vector3 direction)
    {
        IsDodging = true;
        _onCooldown = true;
        IsInvincible = true;

        float elapsed = 0f;
        float speed = _dodgeDistance / _dodgeDuration;

        while (elapsed < _dodgeDuration)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= _iFrameDuration) IsInvincible = false;
            _cc.Move(direction * speed * Time.deltaTime);
            yield return null;
        }

        IsInvincible = false;
        IsDodging = false;

        yield return new WaitForSeconds(_cooldown);
        _onCooldown = false;
    }
}
