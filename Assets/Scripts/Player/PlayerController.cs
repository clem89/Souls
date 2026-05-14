using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] InputReader _input;
    [SerializeField] float _moveSpeed = 5f;
    [SerializeField] float _rotationSpeed = 10f;
    [SerializeField] float _gravity = -20f;

    CharacterController _cc;
    Vector2 _moveInput;
    float _verticalVelocity;
    Transform _cameraTransform;

    public Vector3 MoveDirection { get; private set; }

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _cameraTransform = Camera.main.transform;
        _input.MoveEvent += v => _moveInput = v;
    }

    void Update()
    {
        var camForward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
        var camRight = _cameraTransform.right;
        MoveDirection = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;

        if (_cc.isGrounded)
            _verticalVelocity = -2f;
        else
            _verticalVelocity += _gravity * Time.deltaTime;

        _cc.Move((MoveDirection * _moveSpeed + Vector3.up * _verticalVelocity) * Time.deltaTime);

        if (MoveDirection.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(MoveDirection),
                _rotationSpeed * Time.deltaTime);
    }
}
