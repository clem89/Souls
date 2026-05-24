using System.Collections.Generic;
using UnityEngine;

public class SkillEffectPoolManager : MonoBehaviour
{
    [SerializeField] PlayerLoadout _loadout;

    readonly Dictionary<SkillEffectData, SkillEffectPool> _pools = new();

    public PlayerLoadout Loadout => _loadout;

    void Awake()
    {
        Debug.Assert(_loadout != null, "SkillEffectPoolManager: PlayerLoadout이 할당되지 않음");
        Register(_loadout.lightAttack);
    }

    public SkillEffectPool GetPool(SkillEffectData data)
    {
        _pools.TryGetValue(data, out var pool);
        return pool;
    }

    void Register(SkillEffectData data)
    {
        if (data == null || _pools.ContainsKey(data)) return;
        var container = new GameObject($"Pool_{data.name}");
        container.transform.SetParent(transform);
        _pools[data] = new SkillEffectPool(data, container.transform);
    }
}
