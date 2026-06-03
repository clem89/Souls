using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillState : MonoBehaviour
{
    readonly Dictionary<string, string> _current = new();

    public string GetCurrentId(string baseSkillId)
    {
        return _current.TryGetValue(baseSkillId, out var id) ? id : baseSkillId;
    }

    public void Evolve(string baseSkillId, string chosenSkillId)
    {
        _current[baseSkillId] = chosenSkillId;
        Debug.Log($"[SkillState] {baseSkillId} → {chosenSkillId}");
    }
}
