using UnityEngine;

public class LockOnSystem : MonoBehaviour
{
    [SerializeField] InputReader _input;
    [SerializeField] PlayerCamera _playerCamera;
    [SerializeField] float _range = 15f;
    [SerializeField] float _maxAngle = 60f;
    [SerializeField] LayerMask _enemyLayer;

    Transform _currentTarget;

    void Awake() => _input.LockOnPerformed += ToggleLockOn;

    void OnDestroy() => _input.LockOnPerformed -= ToggleLockOn;

    void Update()
    {
        if (_currentTarget == null) return;
        if (_currentTarget.TryGetComponent<DummyEnemy>(out var e) && e.CurrentHp <= 0f)
        {
            _currentTarget = null;
            _playerCamera.LockOnTarget = null;
        }
    }

    void ToggleLockOn()
    {
        if (_currentTarget != null)
        {
            _currentTarget = null;
            _playerCamera.LockOnTarget = null;
            Debug.Log("[LockOn] 해제");
            return;
        }

        var best = FindBestTarget();
        if (best != null)
        {
            _currentTarget = best;
            _playerCamera.LockOnTarget = best;
            Debug.Log($"[LockOn] 타겟 잠금: {best.name}");
        }
        else
        {
            Debug.Log("[LockOn] 범위 내 타겟 없음");
        }
    }

    Transform FindBestTarget()
    {
        var hits = Physics.OverlapSphere(transform.position, _range, _enemyLayer);
        Transform best = null;
        float bestScore = float.MaxValue;
        var camForward = Camera.main.transform.forward;

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
