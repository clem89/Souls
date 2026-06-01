using System;
using System.Collections.Generic;
using UnityEngine;

public static class EffectTable
{
    static Dictionary<string, EffectDef> _cache;

    public static EffectDef Get(string effectId)
    {
        Load();
        _cache.TryGetValue(effectId, out var def);
        return def;
    }

    static void Load()
    {
        if (_cache != null) return;
        _cache = new Dictionary<string, EffectDef>();
        var text = Resources.Load<TextAsset>("Data/EffectTable");
        if (text == null)
        {
            Debug.LogError("[EffectTable] Resources/Data/EffectTable.json 을 찾을 수 없음");
            return;
        }
        var wrapper = JsonUtility.FromJson<Wrapper>(text.text);
        foreach (var def in wrapper.entries)
            _cache[def.effectId] = def;
    }

    [Serializable] class Wrapper { public List<EffectDef> entries; }
}
