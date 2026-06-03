using System.Collections.Generic;
using UnityEngine;

public class LevelSystem : MonoBehaviour
{
    [SerializeField] float            _xpPerLevel = 100f;
    [SerializeField] PlayerSkillState _skillState;

    public int   Level     { get; private set; } = 1;
    public float CurrentXp { get; private set; }
    public float XpToNext  => _xpPerLevel;

    public void AddXp(float amount)
    {
        CurrentXp += amount;
        while (CurrentXp >= _xpPerLevel)
        {
            CurrentXp -= _xpPerLevel;
            Level++;
            TriggerSkillChoice();
        }
    }

    void TriggerSkillChoice()
    {
        var available = SkillTable.GetAvailable(_skillState.OwnedIds);
        if (available.Count == 0)
        {
            Debug.Log("[LevelSystem] 선택 가능한 스킬 없음");
            return;
        }

        for (int i = available.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (available[i], available[j]) = (available[j], available[i]);
        }

        var options = available.GetRange(0, Mathf.Min(3, available.Count));
        SkillUpgradeUI.Instance.Show(options.ToArray(), _skillState);
    }
}
