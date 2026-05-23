using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    Animator _animator;
    PlayerController _controller;
    PlayerCombat _combat;
    PlayerDodge _dodge;
    PlayerHealth _health;

    static readonly int SpeedHash      = Animator.StringToHash("Speed");
    static readonly int AttackHash     = Animator.StringToHash("Attack");
    static readonly int AttackStepHash = Animator.StringToHash("AttackStep");
    static readonly int DodgeHash      = Animator.StringToHash("Dodge");
    static readonly int IsParryingHash = Animator.StringToHash("IsParrying");
    static readonly int RiposteHash    = Animator.StringToHash("Riposte");
    static readonly int IsDeadHash     = Animator.StringToHash("IsDead");

    bool _wasAttacking;
    bool _wasDodging;
    bool _wasRiposteReady;

    void Awake()
    {
        _animator    = GetComponentInChildren<Animator>();
        _controller  = GetComponent<PlayerController>();
        _combat      = GetComponent<PlayerCombat>();
        _dodge       = GetComponent<PlayerDodge>();
        _health      = GetComponent<PlayerHealth>();
        Debug.Assert(_animator != null, "PlayerAnimator: Animator not found in children");

        _health.OnDeath += OnDeath;
    }

    void OnDestroy() => _health.OnDeath -= OnDeath;

    void Update()
    {
        _animator.SetFloat(SpeedHash, _controller.MoveDirection.magnitude);
        _animator.SetBool(IsParryingHash, _combat.IsParrying);

        // Attack 트리거: 공격 시작 프레임
        if (_combat.IsAttacking && !_wasAttacking)
        {
            _animator.SetInteger(AttackStepHash, _combat.ComboStep);
            _animator.SetTrigger(AttackHash);
        }
        _wasAttacking = _combat.IsAttacking;

        // Dodge 트리거: 회피 시작 프레임
        if (_dodge.IsDodging && !_wasDodging)
            _animator.SetTrigger(DodgeHash);
        _wasDodging = _dodge.IsDodging;

        // Riposte 트리거: RiposteReady → false 전환 (리포스트 실행 순간)
        if (_wasRiposteReady && !_combat.RiposteReady && _combat.IsAttacking)
            _animator.SetTrigger(RiposteHash);
        _wasRiposteReady = _combat.RiposteReady;
    }

    void OnDeath() => _animator.SetBool(IsDeadHash, true);
}
