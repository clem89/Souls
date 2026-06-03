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

    // 조건 없는 기본 스킬 (parentSkillId가 비어있는 것)
    public static List<SkillDef> GetAllBase()
    {
        Load();
        var result = new List<SkillDef>();
        foreach (var def in _cache.Values)
            if (string.IsNullOrEmpty(def.parentSkillId))
                result.Add(def);
        return result;
    }

    // 특정 스킬의 직계 자식들
    public static List<SkillDef> GetChildren(string parentId)
    {
        Load();
        var result = new List<SkillDef>();
        foreach (var def in _cache.Values)
            if (def.parentSkillId == parentId)
                result.Add(def);
        return result;
    }

    // 레벨업 시 선택 가능한 스킬: 미보유 base OR 부모를 보유 중인 것
    public static List<SkillDef> GetAvailable(HashSet<string> ownedIds)
    {
        Load();
        var result = new List<SkillDef>();
        foreach (var def in _cache.Values)
        {
            if (ownedIds.Contains(def.skillId)) continue;
            bool isBase = string.IsNullOrEmpty(def.parentSkillId);
            if (isBase || ownedIds.Contains(def.parentSkillId))
                result.Add(def);
        }
        return result;
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
