using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] float _maxHp = 100f;

    PlayerDodge _dodge;

    public float CurrentHp { get; private set; }
    public float MaxHp => _maxHp;
    public bool IsInvincible => _dodge != null && _dodge.IsInvincible;

    public event Action<float> OnHpChanged;
    public event Action OnDeath;

    bool _isDead;

    void Awake()
    {
        CurrentHp = _maxHp;
        _dodge = GetComponent<PlayerDodge>();
    }

    public void TakeDamage(float amount, GameObject source)
    {
        if (IsInvincible || _isDead) return;
        CurrentHp = Mathf.Max(0f, CurrentHp - amount);
        OnHpChanged?.Invoke(CurrentHp / _maxHp);
        if (CurrentHp <= 0f)
        {
            _isDead = true;
            OnDeath?.Invoke();
        }
    }
}
