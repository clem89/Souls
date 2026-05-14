using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] InputReader _input;
    [SerializeField] Transform _target;
    [SerializeField] float _distance = 4f;
    [SerializeField] float _height = 1.5f;
    [SerializeField] float _sensitivity = 2f;
    [SerializeField] float _pitchMin = -30f;
    [SerializeField] float _pitchMax = 60f;

    float _yaw;
    float _pitch = 15f;

    public Transform LockOnTarget { get; set; }

    void Awake()
    {
        Debug.Assert(_input != null, "PlayerCamera: InputReader not assigned");
        Cursor.lockState = CursorLockMode.Locked;
        _input.LookEvent += OnLook;
    }

    void OnDestroy() => _input.LookEvent -= OnLook;

    void OnLook(Vector2 delta)
    {
        _yaw += delta.x * _sensitivity;
        _pitch -= delta.y * _sensitivity;
        _pitch = Mathf.Clamp(_pitch, _pitchMin, _pitchMax);
    }

    void LateUpdate()
    {
        if (_target == null) return;

        if (LockOnTarget != null)
        {
            var toTarget = Vector3.ProjectOnPlane(
                LockOnTarget.position - _target.position, Vector3.up);
            if (toTarget.sqrMagnitude > 0.01f)
                _yaw = Quaternion.LookRotation(toTarget).eulerAngles.y;
        }

        var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.position = _target.position + rotation * new Vector3(0f, _height, -_distance);
        transform.LookAt(_target.position + Vector3.up * (_height * 0.5f));
    }
}
