using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParryReceiver))]
[RequireComponent(typeof(DummyEnemy))]
public class DummyEnemyAttack : MonoBehaviour
{
    [SerializeField] float _attackInterval = 3f;
    [SerializeField] float _windupTime = 0.6f;
    [SerializeField] float _parryWindowDuration = 0.4f;
    [SerializeField] float _attackDamage = 25f;
    [SerializeField] float _attackRange = 2f;

    ParryReceiver _parryReceiver;
    DummyEnemy _enemy;
    Transform _player;

    void Awake()
    {
        _parryReceiver = GetComponent<ParryReceiver>();
        _enemy = GetComponent<DummyEnemy>();
        _enemy.OnDeath += StopAttacking;
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (_player == null) Debug.LogWarning("[DummyEnemyAttack] Player 태그를 찾을 수 없습니다.");
    }

    void OnDestroy() => _enemy.OnDeath -= StopAttacking;

    void StopAttacking() => StopAllCoroutines();

    void Start() => StartCoroutine(AttackLoop());

    IEnumerator AttackLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_attackInterval);
            yield return AttackOnce();
        }
    }

    IEnumerator AttackOnce()
    {
        Debug.Log("[Enemy] ⚠ 공격 예고!");
        yield return new WaitForSeconds(_windupTime);

        _parryReceiver.OpenWindow(_parryWindowDuration);

        // 패링 윈도우 절반 시점에 타격 판정
        yield return new WaitForSeconds(_parryWindowDuration * 0.5f);

        if (_player != null && Vector3.Distance(transform.position, _player.position) <= _attackRange)
        {
            if (_player.TryGetComponent<IDamageable>(out var dmg) && !dmg.IsInvincible)
            {
                dmg.TakeDamage(_attackDamage, gameObject);
                Debug.Log("[Enemy] 타격!");
            }
        }

        yield return new WaitForSeconds(_parryWindowDuration * 0.5f);
    }
}
