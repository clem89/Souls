using UnityEngine;

// Stub — replaced by Task 8
public class DummyEnemy : MonoBehaviour, IDamageable
{
    public float CurrentHp { get; private set; } = 200f;
    public float MaxHp => 200f;
    public bool IsGroggy { get; private set; }
    public bool IsInvincible { get; private set; } = false;

    public void TakeDamage(float amount, GameObject source) { }
    public void SetGroggy(bool value) { IsGroggy = value; }
}
