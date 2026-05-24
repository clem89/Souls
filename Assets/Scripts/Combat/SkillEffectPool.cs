using System.Collections.Generic;
using UnityEngine;

public class SkillEffectPool
{
    readonly Queue<AttackHitbox> _pool = new();
    readonly SkillEffectData _data;
    readonly Transform _parent;

    public SkillEffectPool(SkillEffectData data, Transform parent)
    {
        _data = data;
        _parent = parent;
        for (int i = 0; i < data.poolSize; i++)
            _pool.Enqueue(Spawn());
    }

    public AttackHitbox Get()
    {
        var hb = _pool.Count > 0 ? _pool.Dequeue() : Spawn();
        hb.OnExpired += Return;
        return hb;
    }

    void Return(AttackHitbox hb)
    {
        hb.OnExpired -= Return;
        _pool.Enqueue(hb);
    }

    AttackHitbox Spawn()
    {
        var go = Object.Instantiate(_data.effectPrefab, _parent);
        go.SetActive(false);
        var hb = go.GetComponent<AttackHitbox>();
        Debug.Assert(hb != null, $"[SkillEffectPool] '{_data.effectPrefab.name}'에 AttackHitbox 컴포넌트 없음");
        return hb;
    }
}
