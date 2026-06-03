using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillState : MonoBehaviour
{
    // rootBaseId → 현재 활성 스킬 Id (진화 추적)
    readonly Dictionary<string, string> _active = new();
    readonly HashSet<string>            _owned  = new();

    public HashSet<string> OwnedIds => _owned;

    void Awake()
    {
        foreach (var def in SkillTable.GetAllBase())
        {
            _owned.Add(def.skillId);
            _active[def.skillId] = def.skillId;
        }
    }

    public string GetCurrentId(string baseSkillId)
    {
        return _active.TryGetValue(baseSkillId, out var id) ? id : baseSkillId;
    }

    public void OwnSkill(string skillId)
    {
        _owned.Add(skillId);
        var root = FindRoot(skillId);
        _active[root] = skillId;
        Debug.Log($"[SkillState] {root} → {skillId}");
    }

    string FindRoot(string skillId)
    {
        while (true)
        {
            var def = SkillTable.Get(skillId);
            if (def == null || string.IsNullOrEmpty(def.parentSkillId)) return skillId;
            skillId = def.parentSkillId;
        }
    }
}
