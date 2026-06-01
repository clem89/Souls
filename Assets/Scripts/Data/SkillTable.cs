using System;
using System.Collections.Generic;
using UnityEngine;

public static class SkillTable
{
    static Dictionary<string, SkillDef> _cache;

    public static SkillDef Get(string skillId)
    {
        Load();
        _cache.TryGetValue(skillId, out var def);
        return def;
    }

    static void Load()
    {
        if (_cache != null) return;
        _cache = new Dictionary<string, SkillDef>();
        var text = Resources.Load<TextAsset>("Data/SkillTable");
        if (text == null)
        {
            Debug.LogError("[SkillTable] Resources/Data/SkillTable.json 을 찾을 수 없음");
            return;
        }
        var wrapper = JsonUtility.FromJson<Wrapper>(text.text);
        foreach (var def in wrapper.entries)
            _cache[def.skillId] = def;
    }

    [Serializable] class Wrapper { public List<SkillDef> entries; }
}
