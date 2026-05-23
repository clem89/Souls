using System;
using UnityEngine;

public class DummyEnemy : MonoBehaviour, IDamageable
{
    [SerializeField] float _maxHp = 200f;

    public float CurrentHp { get; private set; }
    public float MaxHp => _maxHp;
    public bool IsGroggy { get; private set; }
    public bool IsInvincible { get; private set; } = false;

    public event Action<float> OnHpChanged;
    public event Action OnDeath;

    SpriteRenderer _spriteRenderer;
    Color _defaultColor;

    void Awake()
    {
        CurrentHp = _maxHp;
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer != null)
            _defaultColor = _spriteRenderer.color;
    }

    public void TakeDamage(float amount, GameObject source)
    {
        if (IsInvincible) return;
        float actual = IsGroggy ? amount * 2f : amount;
        CurrentHp = Mathf.Max(0f, CurrentHp - actual);
        OnHpChanged?.Invoke(CurrentHp / _maxHp);
        Debug.Log($"[Enemy] HP: {CurrentHp:F0}/{_maxHp} (데미지: {actual:F0})");
        if (CurrentHp <= 0f) OnDeath?.Invoke();
    }

    public void SetGroggy(bool value)
    {
        IsGroggy = value;
        if (_spriteRenderer != null)
            _spriteRenderer.color = value ? Color.yellow : _defaultColor;
    }
}
