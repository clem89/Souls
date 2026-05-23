using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(StaminaController))]
public class PlayerDodge : MonoBehaviour
{
    [SerializeField] InputReader _input;
    [SerializeField] float _dodgeDistance = 4f;
    [SerializeField] float _dodgeDuration = 0.3f;
    [SerializeField] float _iFrameDuration = 0.2f;
    [SerializeField] float _staminaCost = 25f;
    [SerializeField] float _cooldown = 0.5f;

    Rigidbody2D _rb;
    StaminaController _stamina;
    PlayerController _controller;

    public bool IsDodging { get; private set; }
    public bool IsInvincible { get; private set; }

    bool _onCooldown;

    void Awake()
    {
        Debug.Assert(_input != null, "PlayerDodge: InputReader not assigned");
        _rb = GetComponent<Rigidbody2D>();
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
            : (Vector2)transform.up;

        StartCoroutine(DodgeCoroutine(dir));
    }

    IEnumerator DodgeCoroutine(Vector2 direction)
    {
        IsDodging = true;
        _onCooldown = true;
        IsInvincible = true;

        float elapsed = 0f;
        float speed = _dodgeDistance / _dodgeDuration;

        while (elapsed < _dodgeDuration)
        {
            _rb.linearVelocity = direction * speed;
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
            if (elapsed >= _iFrameDuration) IsInvincible = false;
        }

        _rb.linearVelocity = Vector2.zero;
        IsInvincible = false;
        IsDodging = false;

        yield return new WaitForSeconds(_cooldown);
        _onCooldown = false;
    }
}
