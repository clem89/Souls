using UnityEngine;

public class LockOnSystem : MonoBehaviour
{
    [SerializeField] InputReader _input;
    [SerializeField] PlayerCamera _playerCamera;
    [SerializeField] float _range = 15f;
    [SerializeField] float _maxAngle = 60f;
    [SerializeField] LayerMask _enemyLayer;

    Transform _currentTarget;

    void Awake()
    {
        Debug.Assert(_input != null, "LockOnSystem: InputReader not assigned");
        Debug.Assert(_playerCamera != null, "LockOnSystem: PlayerCamera not assigned");
        _input.LockOnPerformed += ToggleLockOn;
    }

    void OnDestroy()
    {
        _input.LockOnPerformed -= ToggleLockOn;
        ClearTarget();
    }

    void ToggleLockOn()
    {
        if (_currentTarget != null)
        {
            ClearTarget();
            Debug.Log("[LockOn] 해제");
            return;
        }

        var best = FindBestTarget();
        if (best != null)
        {
            SetTarget(best);
            Debug.Log($"[LockOn] 타겟 잠금: {best.name}");
        }
        else
        {
            Debug.Log("[LockOn] 범위 내 타겟 없음");
        }
    }

    void SetTarget(Transform target)
    {
        _currentTarget = target;
        _playerCamera.LockOnTarget = target;
        if (target.TryGetComponent<DummyEnemy>(out var enemy))
            enemy.OnDeath += ClearTarget;
    }

    void ClearTarget()
    {
        if (_currentTarget != null && _currentTarget.TryGetComponent<DummyEnemy>(out var enemy))
            enemy.OnDeath -= ClearTarget;
        _currentTarget = null;
        _playerCamera.LockOnTarget = null;
    }

    Transform FindBestTarget()
    {
        var hits = Physics.OverlapSphere(transform.position, _range, _enemyLayer);
        Transform best = null;
        float bestScore = float.MaxValue;
        var camForward = _playerCamera.transform.forward;

        foreach (var h in hits)
        {
            var toTarget = h.transform.position - transform.position;
            float angle = Vector3.Angle(camForward, toTarget);
            if (angle > _maxAngle) continue;

            float score = toTarget.magnitude + angle * 0.1f;
            if (score < bestScore) { bestScore = score; best = h.transform; }
        }

        return best;
    }
}
