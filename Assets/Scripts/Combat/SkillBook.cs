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
        var children  = SkillTable.GetChildren(currentId);

        if (children.Count == 0) { Destroy(gameObject); return; }

        for (int i = children.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (children[i], children[j]) = (children[j], children[i]);
        }

        var options = children.GetRange(0, Mathf.Min(3, children.Count));
        SkillUpgradeUI.Instance.Show(options.ToArray(), state);
        Destroy(gameObject);
    }
}
