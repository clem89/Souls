using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] InputReader _input;
    [SerializeField] float _attackRange = 1.2f;
    [SerializeField] float _attackRadius = 0.8f;
    [SerializeField] float _attackDamage = 20f;
    [SerializeField] float _riposteDamage = 60f;
    [SerializeField] float _comboWindow = 0.6f;
    [SerializeField] LayerMask _enemyLayer;

    [SerializeField] float _parryActiveDuration = 0.35f;
    [SerializeField] float _parryCooldown = 0.8f;
    [SerializeField] float _parryDetectionRadius = 2f;
    bool _isParryCooldown;

    public int ComboStep => _comboStep;
    public bool IsAttacking => _isAttacking;

    int _comboStep;
    float _comboTimer;
    bool _isAttacking;
    PlayerDodge _dodge;

    public bool IsParrying { get; private set; }
    public bool RiposteReady { get; private set; }
    public DummyEnemy RiposteTarget { get; private set; }
    float _riposteTimer;
    const float RiposteTimeLimit = 2f;

    static readonly float[] ComboMultipliers = { 1f, 1f, 2f };

    void Awake()
    {
        Debug.Assert(_input != null, "PlayerCombat: InputReader not assigned");
        _dodge = GetComponent<PlayerDodge>();
        _input.AttackStarted += OnAttackInput;
        _input.ParryPerformed += OnParryInput;
    }

    void OnDisable() => _isParryCooldown = false;

    void OnDestroy()
    {
        if (_input == null) return;
        _input.AttackStarted -= OnAttackInput;
        _input.ParryPerformed -= OnParryInput;
    }

    void Update()
    {
        if (_comboStep > 0 && !_isAttacking)
        {
            _comboTimer -= Time.deltaTime;
            if (_comboTimer <= 0f) _comboStep = 0;
        }

        if (RiposteReady)
        {
            _riposteTimer -= Time.deltaTime;
            if (_riposteTimer <= 0f) CancelRiposte();
        }
    }

    void OnAttackInput()
    {
        if (IsParrying) return;
        if (_dodge != null && _dodge.IsDodging) return;

        if (RiposteReady && RiposteTarget != null)
        {
            StartCoroutine(PerformRiposte());
            return;
        }

        if (!_isAttacking)
            StartCoroutine(PerformAttack());
    }

    IEnumerator PerformAttack()
    {
        _isAttacking = true;
        float damage = _attackDamage * ComboMultipliers[_comboStep % 3];
        _comboStep = (_comboStep % 3) + 1;
        _comboTimer = _comboWindow;

        yield return new WaitForSeconds(0.1f);
        DealDamage(damage);
        yield return new WaitForSeconds(0.4f);
        _isAttacking = false;
    }

    IEnumerator PerformRiposte()
    {
        RiposteReady = false;
        _isAttacking = true;

        yield return new WaitForSeconds(0.15f);

        if (RiposteTarget != null)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, RiposteTarget.transform.position, 1.2f);
            RiposteTarget.TakeDamage(_riposteDamage, gameObject);
            RiposteTarget.SetGroggy(false);
            RiposteTarget = null;
        }

        yield return new WaitForSeconds(0.5f);
        _isAttacking = false;
    }

    void DealDamage(float damage)
    {
        var origin = (Vector2)transform.position + (Vector2)transform.up * _attackRange;
        var hits = Physics2D.OverlapCircleAll(origin, _attackRadius, _enemyLayer);
        foreach (var h in hits)
        {
            if (h.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(damage, gameObject);
        }
    }

    public void SetRiposteTarget(DummyEnemy enemy)
    {
        RiposteReady = true;
        RiposteTarget = enemy;
        _riposteTimer = RiposteTimeLimit;
    }

    public void SetParrying(bool value) => IsParrying = value;

    public void CancelRiposte()
    {
        RiposteReady = false;
        RiposteTarget?.SetGroggy(false);
        RiposteTarget = null;
    }

    void OnParryInput()
    {
        if (_isParryCooldown || IsParrying || _isAttacking) return;
        StartCoroutine(ParryCoroutine());
    }

    IEnumerator ParryCoroutine()
    {
        SetParrying(true);
        _isParryCooldown = true;
        float elapsed = 0f;
        bool success = false;

        while (elapsed < _parryActiveDuration && !success)
        {
            elapsed += Time.deltaTime;
            var hits = Physics2D.OverlapCircleAll(
                (Vector2)transform.position,
                _parryDetectionRadius,
                _enemyLayer);

            foreach (var h in hits)
            {
                if (h.TryGetComponent<ParryReceiver>(out var pr) && pr.IsParryable)
                {
                    if (h.TryGetComponent<DummyEnemy>(out var enemy))
                    {
                        success = true;
                        pr.CloseWindow();
                        enemy.SetGroggy(true);
                        SetRiposteTarget(enemy);
                        Debug.Log("[Parry] 성공! LMB로 Riposte 입력 (2초 이내)");
                        break;
                    }
                }
            }
            yield return null;
        }

        if (!success) Debug.Log("[Parry] 실패");

        SetParrying(false);
        yield return new WaitForSeconds(_parryCooldown);
        _isParryCooldown = false;
    }
}
