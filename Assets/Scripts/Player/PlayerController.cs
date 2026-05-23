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

    public Vector3 MoveDirection { get; private set; }

    void Awake()
    {
        Debug.Assert(_input != null, "PlayerController: InputReader not assigned");
        _cc = GetComponent<CharacterController>();
        _input.MoveEvent += OnMove;
    }

    void OnDestroy() => _input.MoveEvent -= OnMove;

    void OnMove(Vector2 v) => _moveInput = v;

    void Update()
    {
        MoveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;

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
