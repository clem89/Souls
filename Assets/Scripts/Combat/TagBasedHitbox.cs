using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TagBasedHitbox : MonoBehaviour
{
    public event Action<TagBasedHitbox> OnExpired;

    GameObject _owner;
    float      _damage;
    int        _maxHit;
    float      _hitDelay;

    readonly Dictionary<Collider2D, (int count, float lastTime)> _hitRecord = new();

    public void Fire(Vector2 position, float damage, GameObject owner, int maxHit, float hitDelay, float duration)
    {
        transform.position = position;
        _damage   = damage;
        _owner    = owner;
        _maxHit   = maxHit;
        _hitDelay = hitDelay;
        _hitRecord.Clear();
        gameObject.SetActive(true);
        StartCoroutine(ExpireAfter(duration));
    }

    void OnTriggerEnter2D(Collider2D other) => TryHit(other);

    void OnTriggerStay2D(Collider2D other)
    {
        if (_maxHit > 1) TryHit(other);
    }

    void TryHit(Collider2D other)
    {
        if (_owner == null || other.gameObject == _owner) return;
        if (other.CompareTag(_owner.tag)) return;

        if (_hitRecord.TryGetValue(other, out var rec))
        {
            if (rec.count >= _maxHit) return;
            if (Time.time - rec.lastTime < _hitDelay) return;
            _hitRecord[other] = (rec.count + 1, Time.time);
        }
        else
        {
            _hitRecord[other] = (1, Time.time);
        }

        if (other.TryGetComponent<IDamageable>(out var d))
            d.TakeDamage(_damage, _owner);
    }

    IEnumerator ExpireAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        Expire();
    }

    public void Expire()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
        _hitRecord.Clear();
        OnExpired?.Invoke(this);
    }
}
