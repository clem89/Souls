using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class AttackHitbox : MonoBehaviour
{
    public event Action<AttackHitbox> OnExpired;

    float _damage;
    GameObject _source;
    LayerMask _enemyLayer;
    readonly HashSet<Collider2D> _hit = new();

    void Awake()
    {
        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    public void Fire(Vector2 position, float damage, GameObject source, LayerMask enemyLayer, float lifetime)
    {
        transform.position = position;
        _damage = damage;
        _source = source;
        _enemyLayer = enemyLayer;
        _hit.Clear();
        gameObject.SetActive(true);
        StartCoroutine(ExpireAfter(lifetime));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if ((_enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;
        if (!_hit.Add(other)) return;
        if (other.TryGetComponent<IDamageable>(out var d))
            d.TakeDamage(_damage, _source);
    }

    IEnumerator ExpireAfter(float t)
    {
        yield return new WaitForSeconds(t);
        Expire();
    }

    public void Expire()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
        _hit.Clear();
        OnExpired?.Invoke(this);
    }
}
