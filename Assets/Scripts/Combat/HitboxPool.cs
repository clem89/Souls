using System.Collections.Generic;
using UnityEngine;

public class HitboxPool : MonoBehaviour
{
    readonly Dictionary<string, Queue<TagBasedHitbox>> _pools = new();

    public TagBasedHitbox Get(string effectName)
    {
        if (!_pools.TryGetValue(effectName, out var queue) || queue.Count == 0)
        {
            var fresh = Create(effectName);
            Subscribe(effectName, fresh);
            return fresh;
        }

        var pooled = queue.Dequeue();
        Subscribe(effectName, pooled);
        return pooled;
    }

    void Subscribe(string effectName, TagBasedHitbox hb)
    {
        void Handler(TagBasedHitbox h)
        {
            h.OnExpired -= Handler;
            if (!_pools.TryGetValue(effectName, out var q))
            {
                q = new Queue<TagBasedHitbox>();
                _pools[effectName] = q;
            }
            q.Enqueue(h);
        }
        hb.OnExpired += Handler;
    }

    TagBasedHitbox Create(string effectName)
    {
        var prefab = Resources.Load<GameObject>($"Effects/{effectName}");
        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab, transform);
        }
        else
        {
            go = new GameObject(effectName);
            go.transform.SetParent(transform);
            go.AddComponent<CircleCollider2D>().isTrigger = true;
        }
        go.SetActive(false);
        return go.GetComponent<TagBasedHitbox>() ?? go.AddComponent<TagBasedHitbox>();
    }
}
