using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    static readonly int SpeedHash        = Animator.StringToHash("Speed");
    static readonly int AttackStepHash   = Animator.StringToHash("AttackStep");
    static readonly int IsParryingHash   = Animator.StringToHash("IsParrying");
    static readonly int RiposteReadyHash = Animator.StringToHash("RiposteReady");
    static readonly int HurtTriggerHash  = Animator.StringToHash("HurtTrigger");
    static readonly int IsDeadHash       = Animator.StringToHash("IsDead");

    SpriteRenderer _sr;
    Animator _animator;
    PlayerController _controller;
    PlayerCombat _combat;
    PlayerDodge _dodge;
    PlayerHealth _health;

    void Awake()
    {
        _sr         = GetComponentInChildren<SpriteRenderer>();
        _animator   = GetComponentInChildren<Animator>();
        _controller = GetComponent<PlayerController>();
        _combat     = GetComponent<PlayerCombat>();
        _dodge      = GetComponent<PlayerDodge>();
        _health     = GetComponent<PlayerHealth>();
    }

    void OnEnable()
    {
        if (_health != null) _health.OnHpChanged += OnHpChanged;
    }

    void OnDisable()
    {
        if (_health != null) _health.OnHpChanged -= OnHpChanged;
    }

    void Update()
    {
        if (_animator == null) return;
        UpdateAnimatorParams();
        UpdateIFrameAlpha();
    }

    void UpdateAnimatorParams()
    {
        _animator.SetBool(IsDeadHash, _health != null && _health.CurrentHp <= 0f);

        int step = (_combat != null && _combat.IsAttacking) ? _combat.ComboStep : 0;
        _animator.SetInteger(AttackStepHash, step);

        _animator.SetBool(IsParryingHash,   _combat != null && _combat.IsParrying);
        _animator.SetBool(RiposteReadyHash, _combat != null && _combat.RiposteReady);

        float speed = _controller != null ? _controller.MoveDirection.sqrMagnitude : 0f;
        _animator.SetFloat(SpeedHash, speed);
    }

    void UpdateIFrameAlpha()
    {
        if (_sr == null || _dodge == null) return;
        if (_dodge.IsInvincible)
            _sr.color = new Color(1f, 1f, 1f, Mathf.PingPong(Time.time * 8f, 1f));
        else
            _sr.color = Color.white;
    }

    void OnHpChanged(float ratio)
    {
        // ratio == 0은 사망 — IsDead bool이 담당하므로 Hurt는 생존 피격만
        if (ratio > 0f && _animator != null)
            _animator.SetTrigger(HurtTriggerHash);
    }
}
