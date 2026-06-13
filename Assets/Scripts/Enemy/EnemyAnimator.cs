using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    static readonly int SpeedHash   = Animator.StringToHash("Speed");
    static readonly int HurtHash    = Animator.StringToHash("HurtTrigger");
    static readonly int IsDeadHash  = Animator.StringToHash("IsDead");

    Animator   _animator;
    DummyEnemy _enemy;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _enemy    = GetComponent<DummyEnemy>();
    }

    void OnEnable()
    {
        if (_enemy == null) return;
        _enemy.OnHpChanged += OnHpChanged;
        _enemy.OnDeath     += OnDeath;
    }

    void OnDisable()
    {
        if (_enemy == null) return;
        _enemy.OnHpChanged -= OnHpChanged;
        _enemy.OnDeath     -= OnDeath;
    }

    void Update()
    {
        if (_animator == null) return;
        _animator.SetFloat(SpeedHash, 0f); // AI 연결 전 고정
    }

    void OnHpChanged(float ratio)
    {
        if (ratio > 0f && _animator != null)
            _animator.SetTrigger(HurtHash);
    }

    void OnDeath()
    {
        if (_animator != null)
            _animator.SetBool(IsDeadHash, true);
    }
}
