using System.Collections.Generic;
using UnityEngine;

public class SkillBook : MonoBehaviour
{
    [SerializeField] string _baseSkillId;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent<PlayerSkillState>(out var state)) return;

        var currentId = state.GetCurrentId(_baseSkillId);
        var def       = SkillTable.Get(currentId);

        if (def == null || def.nextSkillIds == null || def.nextSkillIds.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        var pool = new List<string>(def.nextSkillIds);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        var options = new List<SkillDef>();
        for (int i = 0; i < pool.Count && options.Count < 3; i++)
        {
            var d = SkillTable.Get(pool[i]);
            if (d != null) options.Add(d);
        }

        if (options.Count == 0) { Destroy(gameObject); return; }
        SkillUpgradeUI.Instance.Show(_baseSkillId, options.ToArray(), state);
        Destroy(gameObject);
    }
}
