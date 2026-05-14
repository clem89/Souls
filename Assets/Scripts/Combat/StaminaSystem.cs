using System;
using UnityEngine;

public class StaminaSystem
{
    public float Max { get; }
    public float Current { get; private set; }
    public event Action<float> OnChanged;

    public StaminaSystem(float max)
    {
        Max = max;
        Current = max;
    }

    public bool TryConsume(float amount)
    {
        if (Current < amount) return false;
        Current = Mathf.Max(0f, Current - amount);
        OnChanged?.Invoke(Current);
        return true;
    }

    public void Recover(float amount)
    {
        Current = Mathf.Min(Max, Current + amount);
        OnChanged?.Invoke(Current);
    }
}
