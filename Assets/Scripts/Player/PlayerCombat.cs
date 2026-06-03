using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] InputReader _input;
    [SerializeField] float _baseDamage     = 20f;
    [SerializeField] float _riposteDamage  = 60f;
    [SerializeField] float _comboWindow    = 0.6f;

    [SerializeField] float _parryActiveDuration  = 0.35f;
    [SerializeField] float _parryCooldown        = 0.8f;
    [SerializeField] float _parryDetectionRadius = 2f;

    [SerializeField] string[] _comboSkillIds =
        { "knight_attack01", "knight_attack02", "knight_attack03" };

    // Attack01=7f, Attack02=10f, Attack03=11f @10fps — KnightAnimatorGenerator 클립과 일치해야 함
    static readonly float[] AttackDurations = { 0.7f, 1.0f, 1.1f };

    HitboxPool       _pool;
    [SerializeField] PlayerSkillState _skillState;
    PlayerController _controller;
    PlayerDodge      _dodge;

    public int  ComboStep   => _comboStep;
    public bool IsAttacking => _isAttacking;
    public bool IsParrying  { get; private set; }
    public bool RiposteReady { get; private set; }
    public DummyEnemy RiposteTarget { get; private set; }

    int   _comboStep;
    float _comboTimer;
    bool  _isAttacking;
    bool  _isParryCooldown;
    float _riposteTimer;
    const float RiposteTimeLimit = 2f;

    void Awake()
    {
        Debug.Assert(_input != null, "PlayerCombat: InputReader not assigned");
        _pool       = GetComponent<HitboxPool>();
        _controller = GetComponent<PlayerController>();
        _dodge      = GetComponent<PlayerDodge>();
        Debug.Assert(_pool != null, "PlayerCombat: HitboxPool not found on Player");
        _input.AttackStarted  += OnAttackInput;
        _input.ParryPerformed += OnParryInput;
    }

    void OnDisable() => _isParryCooldown = false;

    void OnDestroy()
    {
        if (_input == null) return;
        _input.AttackStarted  -= OnAttackInput;
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
        int stepIndex = _comboStep % 3;
        _comboStep  = stepIndex + 1;
        _comboTimer = _comboWindow;

        string skillId = stepIndex < _comboSkillIds.Length ? _comboSkillIds[stepIndex] : null;
        SpawnEffects(skillId);

        yield return new WaitForSeconds(AttackDurations[stepIndex]);
        _isAttacking = false;
    }

    void SpawnEffects(string baseSkillId)
    {
        if (baseSkillId == null) return;
        var id     = _skillState != null ? _skillState.GetCurrentId(baseSkillId) : baseSkillId;
        var skill  = SkillTable.Get(id);
        if (skill == null) return;
        var effect = EffectTable.Get(skill.effectId);
        if (effect == null) return;

        float damage = _baseDamage * skill.baseCoefficient;
        foreach (var entry in effect.hitboxes)
            StartCoroutine(SpawnHitboxAt(entry, damage));
    }

    IEnumerator SpawnHitboxAt(HitboxEntry entry, float damage)
    {
        yield return new WaitForSeconds(entry.delay);

        float facing = _controller != null ? _controller.FacingSign : 1f;
        var offset   = new Vector2(entry.offsetX * facing, entry.offsetY);
        var pos      = (Vector2)transform.position + offset;

        _pool.Get(entry.effectName).Fire(pos, damage, gameObject, entry.maxHit, entry.hitDelay, entry.duration);
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

    IEnumerator ParryCoroutine()
    {
        IsParrying    = true;
        _isParryCooldown = true;
        float elapsed = 0f;
        bool  success = false;

        while (elapsed < _parryActiveDuration && !success)
        {
            elapsed += Time.deltaTime;
            var hits = Physics2D.OverlapCircleAll(
                (Vector2)transform.position, _parryDetectionRadius,
                LayerMask.GetMask("Enemy"));

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

        IsParrying = false;
        yield return new WaitForSeconds(_parryCooldown);
        _isParryCooldown = false;
    }

    void OnParryInput()
    {
        if (_isParryCooldown || IsParrying || _isAttacking) return;
        StartCoroutine(ParryCoroutine());
    }

    public void SetRiposteTarget(DummyEnemy enemy)
    {
        RiposteReady  = true;
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
}
